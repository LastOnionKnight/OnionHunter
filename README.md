# Onion Hunter

A tiny Dalamud plugin that logs your retainer ventures and shows your real, observed drop rates for the gear you are hunting. No invented percentages. Just your data, with a Wilson 95 percent confidence interval so small samples do not lie.

## Tracked by default
- Peregrine Helm
- Onion Doublet
- Onion Sorrel

Targets are configurable in the window. Names are resolved against the Lumina Item sheet at runtime; a typo just will not match, it never breaks the build.

## How it works
- Hooks the RetainerTaskResult window via IAddonLifecycle.
- Reads the returned item by name from a text node, resolves to an item id, logs it.
- Computes hits / total per tracked item with a Wilson 95 percent CI.

## First-run setup
1. Build: dotnet build -c Release
2. Load the plugin in Dalamud.
3. Run /onionhunt.
4. Enable "Dump venture-window nodes", collect one venture, open /xllog, find the node whose text is the returned item name.
5. Paste that node id into "Item-name node id", disable the dump toggle.
6. Run ventures. Counters and CIs populate as you go.

## Status
Pre-alpha. Standalone, separate version stream from GearGoblin / Tonberry Tactics.