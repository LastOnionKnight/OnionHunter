using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace OnionHunter;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>
    /// Items you're hunting. Names are resolved to item IDs at runtime against the
    /// Lumina Item sheet, so a typo here just won't resolve -- it never breaks the build.
    /// Fix the string in-game if a piece doesn't resolve (e.g. exact spelling of the Onion set).
    /// </summary>
    public List<string> TargetItemNames { get; set; } = new()
    {
        "Peregrine Helm",
        "Onion Doublet",
        "Onion Sorrel",
    };

    /// <summary>
    /// The AtkTextNode id inside the RetainerTaskResult window that holds the returned
    /// item's name. 0 = unknown. Use the "Dump venture-window nodes" button once in-game
    /// to discover it, then paste it here. This is config-driven on purpose so you never
    /// have to recompile to chase a UI layout change.
    /// </summary>
    public uint ItemNameNodeId { get; set; } = 0;

    /// <summary>When on, every venture result dumps all text nodes to the Dalamud log.</summary>
    public bool DebugDumpNodes { get; set; } = false;

    /// <summary>The full, persisted log of every venture return we've observed.</summary>
    public List<VentureRecord> Records { get; set; } = new();

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

[Serializable]
public class VentureRecord
{
    public long TimestampUnix { get; set; }
    public uint ItemId { get; set; }            // 0 if the window text didn't resolve to a known item
    public string ItemName { get; set; } = "";  // resolved name, or the raw window text as a fallback
}