using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace OnionHunter.Services;

/// <summary>
/// Listens for the "RetainerTaskResult" window (the "your retainer has returned with..."
/// panel) and records the item shown. We read the item by its on-screen NAME from a text
/// node and resolve that to an item id, rather than guessing struct offsets -- UI text is far
/// more stable across patches than struct layouts.
/// </summary>
public sealed unsafe class VentureLogger : IDisposable
{
    private const string AddonName = "RetainerTaskResult";
    private readonly Plugin _plugin;

    public VentureLogger(Plugin plugin) => _plugin = plugin;

    private long _lastLoggedUnix;

    public void Enable()
        => Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, AddonName, OnResult);

    public void Dispose()
        => Plugin.AddonLifecycle.UnregisterListener(OnResult);

    private void OnResult(AddonEvent type, AddonArgs args)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - _lastLoggedUnix < 5) return; // Debounce 5 seconds per window

            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null) return;

            if (_plugin.Config.DebugDumpNodes)
                DumpTextNodes(addon);

            var nodeId = _plugin.Config.ItemNameNodeId;
            if (nodeId == 0)
            {
                Plugin.Log.Information(
                    "[OnionHunter] A venture returned, but no ItemNameNodeId is set yet. " +
                    "Turn on 'Dump venture-window nodes' in the window, run one more venture, " +
                    "find the node whose text is the item name, and paste its id in.");
                return;
            }

            var node = addon->GetTextNodeById(nodeId);
            if (node == null) return;

            var windowText = node->NodeText.ToString().Trim();
            if (string.IsNullOrWhiteSpace(windowText)) return;

            var resolved = Items.Resolve(windowText);
            var record = new VentureRecord
            {
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ItemId = resolved?.Id ?? 0,
                ItemName = resolved?.Name ?? windowText,
            };

            _plugin.Config.Records.Add(record);
            _plugin.Config.Save();

            _lastLoggedUnix = now;
            Plugin.Log.Information($"[OnionHunter] Logged venture return: {record.ItemName}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[OnionHunter] Failed to parse a venture result.");
        }
    }

    /// <summary>Logs every text node id + text so you can find the item-name node once.</summary>
    private static void DumpTextNodes(AtkUnitBase* addon)
    {
        var mgr = addon->UldManager;
        for (var i = 0; i < mgr.NodeListCount; i++)
        {
            var n = mgr.NodeList[i];
            if (n == null || n->Type != NodeType.Text) continue;
            var tn = (AtkTextNode*)n;
            Plugin.Log.Information($"[OnionHunter][node] id={n->NodeId} text='{tn->NodeText}'");
        }
    }
}