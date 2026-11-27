# Technical Service Storage – build notes

Target: WRSR 1.2.0.9 (PE stamp 0x6B05A574, image size 0xAE9000), TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W4 /EHsc /LD`,
kernel32.lib only. Companion plugins: weather_roads (consumer of the `tss.grit_spreader`
service), vanilla_buildings (depot storage definitions), localization (text pack
`technical_service_storage`). History newest first.

## 0.3.12 (2026-11-27)

- The package settings page (`config\languages`) in 18 more languages - bg, cs, es, fr, hu, it, ja, ko, pl, pt, ro, ru, sk, sr, tr, uk, zh-CN, zh-TW - translated for the user and checked (keys, placeholders, `\n`, paths); code unchanged apart from the version string.

## 0.3.11 (2026-11-27)

- Rebuilt with `/DTSM_RELEASE_VERSION=0.4.6` for TesmioLoader b0.4.6 (launcher 0.4.6 warns about every plugin built for 0.4.5); code unchanged apart from the version string.

## 0.3.10 (2026-11-27)

- Documentation only (README DE/EN, INI and schema comments, Workshop text where the package has one) brought up to date in the documentation audit; code unchanged apart from the version string.

## 0.3.9 (2026-11-26)

- Depot migration (`storage_migration.h`): the storages a depot has are no longer cloned and
  freed when a missing one is added. The game makes a block with room for all (pushes of
  plain definition storages), the existing storages are copied into it bit by bit, the new
  ones follow, and the old block stays allocated and untouched. Reason: the cloning form,
  used by Vehicle Materials on a vehicle factory, cost the factory its power and crashed the
  game twice (a pointer with a zeroed upper half) - the game keeps addresses into the
  storages of a building. Vehicle Materials 0.4.6 showed the moving form to work in the game
  on a factory. For depots it has not been seen in the game at the time of writing.

## 0.3.8 (2026-11-26)

- Storage migration: `InstallStorageMigrationHook` first asks the loader for the service
  `tesmio.building_load` (`TSM_SERVICE_BUILDING_LOAD`, loader b0.4.5-rmm.3 or newer). With it the
  plugin subscribes (`MigrationEventBefore` takes `g_storageLoadLock`, `MigrationEventAfter` runs
  `MigrationAfterLoad` when the load completed and releases the lock; subscribed as the outermost
  one, `TSM_BUILDING_LOAD_OUTER`, so the lock spans every other plugin's work on the building)
  and does not patch the call
  at 0x457617 any more: other plugins need the same moment and only one patch fits there. Without
  the service (MaxLegend's loader) or when the subscription is refused, the direct patch is
  placed as before, with the same byte checks of the call and of the game's loader.
- The four native helpers (push, deep copy, vector destructor, controls) are verified on both
  paths. The log line `Storage migration [load-hook]` says `via=loader-building-load-event` on
  the new path. The reconciliation itself is unchanged.
- No game address changed. Not run in the game with a saved game at the time of writing.

## 0.3.7 (2026-11-25)

Ported to WRSR 1.2.0.9 (PE stamp 0x6B05A574, image 0xAE9000); 1.2.0.8 is refused by the stamp. No
behaviour change intended.

- 61 addresses carried by the instruction-by-instruction comparison of both builds. Technical
  Services panel 0x783F00 -> 0x784350, post-storage helper 0x72A8F0 -> 0x72AC60, warning append
  0x78551D -> 0x78596D, office priority row 0x28E910 -> 0x28E810.
- Three functions the plugin touches were compiled anew and were located by their bytes
  (`my_plugins\tests\mklist.pl` + `portmap.pl`):
  - Native storage panel 0x73CA40 -> 0x73CDB0. Its frame is 0x10 larger. The shared
    discard-visibility flag is at `rbp-0x40` now (was `rbp-0x44`; all 13 uses in the function moved
    alike); gate 0x74582B -> 0x745C7C, fall-through 0x745C86, skip 0x745FE6. The expected bytes and
    the compare the stub replays follow.
  - World loader: building-load call 0x457397 -> 0x457617 (callee 0x20B890 -> 0x20B790), the LEA of
    the static game object 0x45DB0A -> 0x45DD8A.
  - Vehicle window: empty weight read 0x7FEADC -> 0x7FEF2C, engine power read 0x7FEF35, label draw
    call 0x81B0BB -> 0x81B4EB. The 48 bytes around both reads are unchanged.
- The game object grew by 8 bytes: building vector +0x11BB0 / +0x11BB8 -> +0x11BB8 / +0x11BC0,
  resource vector 0xA2B268 -> 0xA2B270. Vehicle and vehicle type fields did not move.
- Fuel-eligibility hook 0x6E7790 -> 0x6E7B00: the RIP-relative compare in the stolen bytes has a new
  displacement (`F1 79 33 00`); the mode global stays at 0xA1F4F8.

## 0.3.6 (2026-11-25)

Ported to WRSR 1.2.0.8 (the DLC build); 1.1.1.13 is refused by the PE stamp. No behaviour change
intended. The code moved by +0x17BE0 .. +0x3F1B4 with no single distance, `.rdata` and `.data`
moved too, and several game structures grew, so every address and every offset was taken from an
instruction of the new build that does what the old one did.

- UI path: Technical Services panel 0x74C6A0 -> 0x783F00, native storage renderer 0x706DA0 ->
  0x73CA40, post-storage helper 0x6F4CE0 -> 0x72A8F0, the two calls 0x7872A0 / 0x7872B9, the
  warning transition 0x78551D, the 27 priority-row sites 0x786962 .. 0x78715A (row calls still
  target 0x28E910, percentage calls still go through PrintLeftUnicode, the cursor store is still
  `movss [rbp-0x80],xmm2`), office priority row 0x273470 -> 0x28E910 with its label call 0x28EA0B,
  vehicle window reads 0x7FEADC / 0x7FEAE5, engine-label call 0x81B0BB (its format string is
  referenced once in the whole image, so the copy is unambiguous; r15 is still the vehicle).
- Frame of the Technical Services panel: the native warning buffer moved from rbp+0x250 to
  rbp+0x2A0 (1024 characters, the stack cookie follows at rbp+0xAA0); r12b is still the
  "warnings present" flag tested right behind the transition.
- Discard-visibility gate 0x70F9C6 -> 0x74582B: the shared flag is at rbp-0x44 now (was rbp-0x40)
  and the JE target is 0x745B99, an instruction of its own that reloads xmm10 before the common
  join; the stub compares the new slot and jumps to exactly that target. Fall-through 0x745835.
- Float pool, each read back by value and found through the instruction that loads it: arg13
  0x951E94 (24), arg14 0x951E5C (20), warning padding 0x951DFC (15), title phase 0x951E90 (23),
  title lead 0x951F40 (45), DPI 0x9DC08C.
- `.data`: panel context 0xA08070 with rect size / position / padding / colour 0xA082F8 /
  0xA08300 / 0xA08308 / 0xA0831C, input context 0xA9EC50, click flag 0xA9EF51, localization
  context 0x9E15A0, font manager 0x9E0FC0, current UI font pointer 0x9DF338, resource vector
  0xA2B268, static game 0xA1EF20, world pointer 0x9DDC88, task-fuel mode 0xA1F4F8.
- Import slots, confirmed by import name: Collision 0x8B18F0, GetMouseSolid 0x8B1908,
  PrintRightUnicode 0x8B1918, PrintLeftUnicode 0x8B1920, GetString 0x8B1930.
- Layout: building type in the type descriptor +0x360 -> +0x368; the type's own storage vector
  +0x3A0 -> +0x3A8; resource record 0x340 -> 0x380 bytes with 61 base records; vehicle type
  description pointer +0x1708 -> +0x1748, current speed +0xD24 -> +0xD44; in the vehicle type:
  skills +0x85E0/+0x85E8 -> +0x8630/+0x8638, engine power +0x8678 -> +0x86C8, empty weight
  +0x867C -> +0x86CC, fuel capacity +0x8684 -> +0x86D4; building vector of the game object
  +0x11B08/+0x11B10 -> +0x11BB0/+0x11BB8. Unchanged and confirmed by the same instruction in old
  and new code: window +0x01 / +0x240; building +0x318, +0x320, +0x604, +0x970, +0x9E8,
  +0xC70/+0xC78, +0xEA8; the whole Storage object (stride 0xE0; the native deep copy, controls
  builder, push and vector destructor compare equal apart from call targets); vehicle +0x4F0,
  +0x4F8, +0x528, +0x5E8, +0x5F0, +0x680, +0x688, +0x698, +0x6A0, +0x6A8, +0x6B8, +0x700, +0x7A0,
  +0xC80.
- Spreader: skill check 0x4013D0, clear entry 0x6EFEF0, clear calls 0x6D032E / 0x6D06BA,
  natural-home call 0x6D0700, route-home planner 0x6F0280, route-link cleaner 0x6D9E50, refuel
  order 0x6E8AC0, task-fuel eligibility 0x6E7790, fuel-capacity load 0x704B71, low-fuel compare
  0x704B81, fuel-building write 0x704BAD, speed compare 0x6D6067 (it uses xmm14 now), static-game
  LEA 0x45DB0A. World save 0x7C20 -> 0x8140, its one call at 0x450DDE; the folder is passed from
  r12 there (`mov rdx,r12`; it was r15), and the check before the call patch expects that.
- Storage migration: loader 0x20B890 with its call at 0x457397 (`mov rdx,r14` in front of it now),
  push 0xE3ED0, deep copy 0x124750, vector destructor 0xC94D0, controls 0x2068F0.
- Signatures that carry a displacement or a moved field were re-read from the new build: vehicle
  window reads, engine-label call, warning transition, discard gate, office label call, skill
  check, the three spreader calls, route-link cleaner, refuel order, task-fuel compare,
  fuel-capacity load, speed compare, migration load call. Both engine exports that are compared
  (`CalcRectLeftUnicodeNoArg`, `C3D_NODE::GetPosition`) still start with the expected bytes.
- Method: `my_plugins\tests\portmap.pl` with the 1.1.1.9 bytes of every site (window 0x80000),
  `my_plugins\tests\bytearrays.pl` for all 37 expected-byte arrays at their new addresses, a
  script check of the 22 call targets, and old-against-new disassembly for every changed frame
  slot, register and offset.
- Not run in the game. Needs a look there: the panel with its material rows and discard buttons,
  the appended material warnings, wrapped priority labels, the tank line in the vehicle window,
  a snowplow run with refill and return, saving and loading the sidecars, and the add-only
  storage migration on a loaded save. The meaning of the storage bytes +0x84 / +0x85 / +0x88
  (import, export, purpose) was not read again in the parser; only their place in the object was.

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
