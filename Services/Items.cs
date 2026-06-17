using System;
using Lumina.Excel.Sheets;

namespace OnionHunter.Services;

/// <summary>
/// Thin wrapper over the Lumina Item sheet. Note: Lumina's accessor surface drifts a
/// little between versions. If <c>.ExtractText()</c> doesn't exist on your build, swap to
/// <c>.ToString()</c>; both are one-line fixes.
/// </summary>
internal static class Items
{
    public static (uint Id, string Name)? Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Strip quantity (e.g., "Fire Shard x 20" -> "Fire Shard")
        var match = System.Text.RegularExpressions.Regex.Match(name, @"^(.*)\s+x\s*\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
            name = match.Groups[1].Value.Trim();

        var sheet = Plugin.Data.GetExcelSheet<Item>();
        if (sheet == null) return null;

        foreach (var row in sheet)
        {
            var n = row.Name.ExtractText();
            if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                return (row.RowId, n);
        }
        return null;
    }

    public static string NameOf(uint id)
    {
        if (id == 0) return "(unresolved)";
        var sheet = Plugin.Data.GetExcelSheet<Item>();
        var row = sheet?.GetRowOrDefault(id);
        return row?.Name.ExtractText() ?? $"#{id}";
    }
}