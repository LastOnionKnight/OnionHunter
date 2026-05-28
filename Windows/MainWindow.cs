using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using OnionHunter.Services;

namespace OnionHunter.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin _plugin;

    public MainWindow(Plugin plugin)
        : base("Onion Hunter \u2014 Venture Odds###onionhunter")
    {
        _plugin = plugin;
        Size = new Vector2(480, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var cfg = _plugin.Config;
        var total = cfg.Records.Count;

        ImGui.TextUnformatted($"Total venture returns logged: {total}");
        if (total == 0)
            ImGui.TextWrapped(
                "No ventures logged yet. Send retainers on the venture that can yield your " +
                "target gear, collect the results, and they'll appear here. The denominator is " +
                "every return you collect while this is running \u2014 so for a clean rate, only run " +
                "the relevant venture while hunting.");

        ImGui.Separator();

        foreach (var name in cfg.TargetItemNames)
        {
            var resolved = Items.Resolve(name);
            if (resolved is not { } item)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.3f, 1f),
                    $"{name}  \u2014  (couldn't resolve this name; check spelling)");
                continue;
            }

            var hits = cfg.Records.Count(r => r.ItemId == item.Id);
            DrawItemRow(item.Name, hits, total);
        }

        ImGui.Separator();
        DrawSetupSection(cfg);
        ImGui.Separator();
        DrawRecentLog(cfg);
    }

    private static void DrawItemRow(string name, int hits, int total)
    {
        var rate = total > 0 ? (double)hits / total : 0.0;
        var (lo, hi) = Wilson(hits, total);

        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 0.85f, 1f, 1f),
            total > 0
                ? $"{hits}/{total}  =  {rate * 100:0.0}%   (95% CI: {lo * 100:0.0}\u2013{hi * 100:0.0}%)"
                : $"{hits}/{total}");

        // A rough "you'll likely see one within N ventures" read, only once there's signal.
        if (hits > 0 && rate > 0)
        {
            var expected = (int)Math.Ceiling(1.0 / rate);
            ImGui.SameLine();
            ImGui.TextDisabled($"  ~1 per {expected}");
        }
    }

    private void DrawSetupSection(Configuration cfg)
    {
        ImGui.TextDisabled("Setup");

        var nodeId = (int)cfg.ItemNameNodeId;
        if (ImGui.InputInt("Item-name node id", ref nodeId))
        {
            cfg.ItemNameNodeId = (uint)Math.Max(0, nodeId);
            cfg.Save();
        }

        var dump = cfg.DebugDumpNodes;
        if (ImGui.Checkbox("Dump venture-window nodes to log", ref dump))
        {
            cfg.DebugDumpNodes = dump;
            cfg.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                "Turn on, collect one venture, then open the Dalamud log (/xllog). Find the " +
                "node whose text is the returned item's name and put its id above. Then turn this off.");

        if (ImGui.Button("Reset all logged data"))
            ImGui.OpenPopup("confirm_reset");

        if (ImGui.BeginPopup("confirm_reset"))
        {
            ImGui.TextUnformatted("Wipe every logged venture return?");
            if (ImGui.Button("Yes, wipe it"))
            {
                cfg.Records.Clear();
                cfg.Save();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private static void DrawRecentLog(Configuration cfg)
    {
        ImGui.TextDisabled("Recent returns");
        if (ImGui.BeginChild("recent", new Vector2(0, 140), true))
        {
            IEnumerable<VentureRecord> recent = cfg.Records.AsEnumerable().Reverse().Take(40);
            foreach (var r in recent)
            {
                var when = DateTimeOffset.FromUnixTimeSeconds(r.TimestampUnix).LocalDateTime;
                ImGui.TextUnformatted($"{when:MM-dd HH:mm}   {r.ItemName}");
            }
        }
        ImGui.EndChild();
    }

    /// <summary>
    /// Wilson score interval -- an honest 95% range for a proportion that behaves well on
    /// small samples (unlike naive hits/total +/- normal approximation).
    /// </summary>
    private static (double lo, double hi) Wilson(int hits, int n, double z = 1.96)
    {
        if (n == 0) return (0, 0);
        var p = (double)hits / n;
        var z2 = z * z;
        var denom = 1 + z2 / n;
        var centre = p + z2 / (2 * n);
        var margin = z * Math.Sqrt(p * (1 - p) / n + z2 / (4.0 * n * n));
        return (Math.Max(0, (centre - margin) / denom), Math.Min(1, (centre + margin) / denom));
    }
}