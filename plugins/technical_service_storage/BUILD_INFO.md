# Technical Service Storage – build notes

Target: WRSR 1.1.1.13 (PE stamp 0x6A8D4087), TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W4 /EHsc /LD`,
kernel32.lib only. Companion plugins: weather_roads (consumer of the `tss.grit_spreader`
service), vanilla_buildings (depot storage definitions), localization (text pack
`technical_service_storage`). History newest first.

## 0.3.5 (2026-11-18)

Texts only, no behaviour change. The startup line `Grit spreader policy: ...` in the log no
longer carries a version number from before the first publication; it now reads "priorities
and vehicle grit tanks use separate versioned sidecars". Source comments in
technical_service_storage.cpp, sand_spreader_diagnostic.h and storage_migration.h were reworded
the same way, so that they state what the code does instead of when it changed. Addresses,
signatures, configuration keys, save and sidecar formats are those of 0.3.4.

## 0.3.4 (2026-11-16)

Ported to WRSR 1.1.1.13; 1.1.1.9 is refused by the PE stamp. No behaviour change.

- Technical Services panel 0x74C500 -> 0x74C6A0, native storage renderer 0x706C00 -> 0x706DA0,
  post-storage helper 0x6F4B40 -> 0x6F4CE0 and every site inside them (both calls, the warning
  transition, the discard gate, the 27 priority-row sites) moved by +0x1A0; so did the vehicle
  window reads (0x7C0C99, 0x7C0CA2) and the engine-label call (0x7DC0A7).
- Office priority row 0x2734F0 -> 0x273470 and its label call 0x27356B (-0x80).
- Storage migration: loader 0x1F0B60 and controls 0x1EBB70 (-0x80), load call 0x433167 (-0x70);
  push 0xCC2F0, vector destructor 0xB18F0 and deep copy 0x10C500 did not move.
- Spreader: skill check 0x3E2260 (-0xC0), static-game LEA 0x43969A and world-save call 0x42CD3E
  (-0x70), world save 0x7C20 unchanged, every other site +0x1A0.
- Float pool (.rdata) +0x10: arg13 0x90A970 (24), arg14 0x90A938 (20), warning padding 0x90A8DC
  (15), title phase 0x90A96C (23), title lead 0x90AA1C (45). All `.data` addresses and the five
  import slots are unchanged; each was confirmed through an instruction that uses it.
- Signatures that carry a displacement were re-read from the new build: engine-label call,
  warning transition, office label call, migration load call, task-fuel compare.
- Method: `my_plugins\tests\portmap.pl` with the 1.1.1.9 bytes of every site (Ghidra project).
  It finds each blob in the new build, compares instruction by instruction and accepts only
  differences in rel32 / RIP-relative operands; result: 0 changed instructions.
## 0.3.3 (2026-09-08)

- 2026-09-11, version unchanged: the detail log `tesmioloader.technical_service_storage.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before).
Reserve threshold in whole percent: new key `[sand_diagnostic] return_threshold_percent`
(default 20, 0..100), read via `ConfigKeyPresent` (config_validation.h); the internal value stays
in basis points (x100), so the runtime path is unchanged. The 0.3.2 key
`return_threshold_basis_points` is still accepted when the percent key is absent and logs a
WARN pointing to the new key. INI, RMM schema (`[detail:return_threshold_percent]`, 0..100),
texts and READMEs follow. Version string is plain `0.3.3`.

## 0.3.2 (2026-09-08)

`SAND_DIAG_MAX_TRACKED_VEHICLES` raised from 256 to 1024 (sand_spreader_diagnostic.h); the
tracked-vehicle table costs about 250 bytes per slot. `[sand_diagnostic] max_vehicles` now defaults
to 512 and validates 1..1024 (config_validation.h, INI, RMM schema). When the table is full the
least recently seen vehicle is still recycled, unchanged. Shipped `[grit_materials]` list reduced
to `sand` and `gravel` (road_salt is a custom resource the player supplies). Package texts
(schema, de/en) rewritten in player style during the user's review; no other runtime change,
save and sidecar formats unchanged.

## 0.3.1 (2026-09-07)

One path decision added to `LoadConfigFile` (config_validation.h): when
`plugins\technical_service_storage.ini` does not exist, the INI beside the DLL is read instead
(`ConfigOwnDirectory`, GetModuleHandleEx on the DLL's own address). The chosen path is logged
as `ini-path` ("Configuration file: ..."). Parsing, validation, materials, runtime and sidecar
formats are byte for byte the 0.3.0 logic. Balance, native fuel, save formats, priorities and
migration contracts unchanged.

Workshop package `My Plugins\technical_service_storage`: soviet.mod.ini with local_copy,
RMM keyed_list schema for `[grit_materials]` (four tabs, eight cards, all 40 switches),
DE/EN, READMEs per template, LICENSE GPL v3.

Verification: migration, runtime, configuration/material-section tests and the DLL metadata
smoke test of 0.3.0 still apply; static analysis (`cl /analyze /W4`, 2026-09-07) reports no
plugin-code warnings, the remaining findings (SEH filters, migration vector reads) were
reviewed as intentional or false positives.

## 0.3.0 (2026-09-03)

First published version. Plugin metadata, grit-spreader diagnostics and documentation share one
version label (`SPREADER_DIAGNOSTIC_VERSION` aliases `PLUGIN_VERSION`). The material section is
`[grit_materials]`; `ParseConfigText` also accepts the spelling `[Streumaterialien]` and normalises
it before duplicate detection, the first section wins even when the two spellings are mixed.

### Hardening boundaries

- Central startup-only configuration schema, complete integer validation, independent C
  numeric locale, bounded complete file read, per-line warnings. Missing optional keys retain
  defaults. First scalar/section declaration wins; materials retain the first valid
  declaration and their file order.
- Explicitly unusable material configuration suspends grit, migration and priority/tank
  persistence writes; existing native storage/sidecars are retained. Native UI remains
  available. No implicit sand replacement for an invalid config.
- Localization checks host table size, consume, resolver and reserved IDs only for
  plugin-owned keys. Native text IDs and built-in fallback rendering remain.
- Runtime warning causes are independent of the debug budget (one per minute per cause).
  Detail writes serialise whole lines and handle short writes. On disk failure, report once
  through the host outside the lock, without recursive file logging.
- Windows hook preparation rejects before publishing a branch on failure. Once patched,
  protection/cache errors mark health degraded and retain the DLL. The original save target
  is published before patching the native save branch.
- Initialisation/startup summaries use independent time/count baselines and report installed,
  disabled and unavailable features, including migration.
- No wholesale decomposition of `sand_spreader_diagnostic.h` and no gameplay rewrite.

### Contracts

- Runtime skill 35, two verified clear call sites, synchronous grit-spreader API.
- Fuel-proportional grit consumption, signed native fuel and stationary-only speed gating;
  no upper speed gate and no native fuel mutation.
- The reserve request keeps the real and the displayed remainder, then dry treatment at zero.
- The native home-refuelling destination latch owns return requests; the native AI owns route
  and target. A confirmed home refill debits the selected storage only once.
- Depot priorities/tanks and road protection sidecars have a versioned format.
- Add-only storage migration after the native building-load call, with unchanged native
  signatures, deep-copy transaction, controls initialisation and worker lock.
- weather_roads and vanilla_buildings code is not touched by this plugin.

### Diagnostics

F8 is a read-only diagnostic gated by `debug = 1`; lifecycle tracking is not disabled with debug
logging. Routine INFO events under Tank diagnostic, Grit diagnostic and Building lifecycle
diagnostic are opt-in. WARN/ERROR/FATAL and persistence/migration/configuration events are not
filtered by that switch. Shared SDK unused-function notices at /W4 are not gameplay faults.

### Verification

Tests use synthetic objects and isolated temporary save fixtures; no native game state is
executed or edited by the tests. A long-duration in-game test remains the final check.
