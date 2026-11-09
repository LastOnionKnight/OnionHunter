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

        // Clean non-printable SeString control codes, HQ icons, and non-breaking spaces
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\u0000-\u001F\u007F-\u009F\u00a0]", "").Trim();

        // Strip prefix quantity (e.g. "1 Peregrine Helm" -> "Peregrine Helm", "20 Fire Shard" -> "Fire Shard")
        name = System.Text.RegularExpressions.Regex.Replace(name, @"^\d+\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        // Strip suffix quantity (e.g. "Fire Shard x 20" or "Fire Shard × 20" -> "Fire Shard")
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+[xX×]\s*\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

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