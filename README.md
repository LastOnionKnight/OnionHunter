# Onion Hunter

**Current version: 0.1.2.4**

Onion Hunter is a small Final Fantasy XIV Dalamud plugin that records observed retainer-venture results and reports your own measured drop rate for tracked items.

It does not claim hidden server-side loot-table percentages. The statistics are based only on ventures the plugin actually observes on your account.

## Current platform

- Dalamud API 15
- .NET 10 / `net10.0-windows`
- standalone version stream, separate from GearGoblin / Tonberry Tactics

## What it does

- watches the retainer venture result window
- reads the returned item name
- resolves that name against the Lumina `Item` sheet
- logs observed venture returns
- tracks hits / total observations per configured target
- reports an observed rate with a Wilson 95% confidence interval

The confidence interval is important for low sample counts: a few lucky or unlucky ventures should not be presented as a trustworthy true drop rate.

## Default tracked items

The default configuration includes targets such as:

- Peregrine Helm
- Onion Doublet
- Onion Sorrel

Targets are configurable. Item names are resolved from game data at runtime; an invalid name simply fails to match instead of breaking the build.

## Command

```text
/onionhunt
```

Use the command to open the plugin window.

## First-run calibration

The plugin currently relies on a configurable text-node ID for the returned item name in the retainer result window.

1. Build or install Onion Hunter.
2. Run `/onionhunt`.
3. Enable the venture-window node dump option.
4. Complete a retainer venture.
5. Check `/xllog` for the node containing the returned item name.
6. Enter that node ID in the plugin configuration.
7. Disable node dumping.
8. Continue running ventures; observed counts and confidence intervals will accumulate.

## Repository distribution

The custom plugin repository points to:

```text
dist/latest.zip
```

That ZIP is intentionally committed because `repo.json` uses the raw GitHub path as the install/update source. Unpacked test copies of that ZIP are not source artifacts and should not be committed.

## Build

```powershell
dotnet restore
dotnet build -c Release
```

## Status

Pre-alpha / experimental. The core observation and statistics path exists, but venture-window parsing remains dependent on the calibrated UI node and should be treated as patch-sensitive.

## Scope

Onion Hunter is intentionally separate from GearGoblin / Tonberry Tactics. It is an observation tool for retainer ventures, not a gear optimizer and not a mechanism for reading or manipulating hidden loot tables.
