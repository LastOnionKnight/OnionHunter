using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace OnionHunter.Services;

/// <summary>
/// Listens for the "RetainerTaskResult" window and records the item shown.
/// We recursively scan the UI tree for any text node that resolves to a valid item name.
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

            (uint Id, string Name)? resolved = null;
            if (_plugin.Config.ItemNameNodeId > 0)
            {
                var node = addon->GetNodeById(_plugin.Config.ItemNameNodeId);
                if (node != null && node->Type == NodeType.Text)
                {
                    var tn = (AtkTextNode*)node;
                    var text = tn->NodeText.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var cleanText = text.Replace("", "").Trim();
                        resolved = Items.Resolve(cleanText);
                    }
                }
            }

            if (resolved == null)
            {
                resolved = FindItemNameRecursively(&addon->UldManager);
            }

            if (resolved == null)
            {
                if (_plugin.Config.DebugDumpNodes)
                    Plugin.Log.Warning("[OnionHunter] Venture returned but no item name found in UI.");
                return;
            }

            // Exclude "Venture" itself as it's the currency and usually present in UI text.
            if (resolved.Value.Name.Equals("Venture", StringComparison.OrdinalIgnoreCase))
                return;

            var record = new VentureRecord
            {
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ItemId = resolved.Value.Id,
                ItemName = resolved.Value.Name,
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

    private static (uint Id, string Name)? FindItemNameRecursively(AtkUldManager* mgr)
    {
        if (mgr == null) return null;

        for (var i = 0; i < mgr->NodeListCount; i++)
        {
            var n = mgr->NodeList[i];
            if (n == null) continue;

            if (n->Type == NodeType.Text)
            {
                var tn = (AtkTextNode*)n;
                var text = tn->NodeText.ToString();
                
                // Usually items have a weird prefix character like the HQ symbol or the rare item symbol.
                // We'll clean it up, or just let Lumina resolve it.
                // We strip out unprintable chars or rely on Items.Resolve being fuzzy.
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Basic cleanup in case of UI control characters like HQ icon
                    var cleanText = text.Replace("", "").Trim();
                    var resolved = Items.Resolve(cleanText);
                    if (resolved != null && resolved.Value.Id != 0 && !resolved.Value.Name.Equals("Venture", StringComparison.OrdinalIgnoreCase))
                        return resolved;
                }
            }
            
            var cn = n->GetAsAtkComponentNode();
            if (cn != null && cn->Component != null)
            {
                var found = FindItemNameRecursively(&cn->Component->UldManager);
                if (found != null) return found;
            }
        }
        return null;
    }
}