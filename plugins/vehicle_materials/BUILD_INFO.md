# Vehicle Materials – build notes

TesmioLoader plugin (API 4) for WRSR 1.2.0.9 (PE stamp 0x6B05A574), GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /LD`, kernel32.lib) compiles `vehicle_materials.cpp`; it
includes `src/tesmio_plugin.h` and `my_plugins/tesmio_config.h` (base INI beside the DLL or in
`plugins\`, personal overlay `user_config\vehicle_materials.ini`). User documentation: README_DE.md / README_EN.md. History newest first.

## 0.4.14 (2026-11-27)

- Package schema only: the rocket parts row of the vehicle classes matrix names `builtin:rocket`
  (Republic Mod Manager 0.8.12 draws it; an older one falls back to its gear). Code unchanged apart
  from the version string.

## 0.4.13 (2026-11-27)

- Vehicle kinds of 1.2.0.9, read from the game: 0x68B070 turns `$TYPE VEHICLETYPE_*` into the
  number at vehicle type +0x2B8 (the parser stores it at rbp+0x948, the weight at rbp+0x8D5C =
  type +0x86CC, so the type starts at rbp+0x690): 1 ROAD, 2 ROAD_SERVICE, 3 RAIL_VAGON,
  4 RAIL_LOCOMOTIVE, 5 RAIL_SERVICE, 6 SHIP, 7 CABIN, 8 AIRPLANE, 9 CONTAINER, 10 HELICOPTER. The
  rocket DLC put CABIN at 7; `AutomaticCategory` still had 7 = airplane, so airplanes (8) used
  the rail values since the port to 1.2.0.8. Now 1, 2 road; 3, 4, 5 rail; 6 ship; 8, 10
  airplane; 9 (the rocket parts of dlc5, all `part_proton_*` / `part_r7_*`) and 7 rocket.
- Fifth class `CAT_ROCKET`, section `[rocket]` (optional; a missing value is 0), `[mapping]`
  takes 4. Schema: `rocket` in `target_sections`, choice 4 in the mapping fields.
- Horse-drawn vehicles: the parser keeps the horses of a vehicle in a vector at type +0x7CC8
  (rbp+0x8358), and the game's requirement builder asks it; a vehicle with horses gets no custom
  material. Their works: building subtype at type +0x36C (parser rbp+0x1E1C, base rbp+0x1AB0),
  42 = $SUBTYPE_HORSE_WAGON (h_carpenter), 43 = $SUBTYPE_HORSE_STUD (h_stud_farm); such a type
  gets no consumption entry and no storage, and a storage an earlier version gave such a works
  leaves when it is empty (`FactoryGivenAny`).

## 0.4.12 (2026-11-27)

- Rebuilt with the shared header of the code review (`g_skipProductionLines`,
  `DefinitionSingles`); this plugin leaves the switch off - production lines are its business.

## 0.4.11 (2026-11-27)

- Rebuilt with the shared header that now serves export storages and every goods class
  (`Direction`, `GoodsClass`); nothing changes for the import storages this plugin gives.

## 0.4.10 (2026-11-27)

- The storages the plugin gave follow `factory_storage_capacity`: `FactoryAfterLoad` calls
  `storage_retrofit::Resize` with `FactoryResizeWanted` (on record and active) for every
  production line, between the removal and `FactoryGiveMissing`. Capacity written in place;
  a smaller size waits until the stock fits.
- Rebuilt with the shared header whose removal rule now looks at the class as well
  (`DefinitionKeeps`); nothing changes for the storages of this plugin.
- Not seen in the game at the time of writing.

## 0.4.9 (2026-11-27)

- The plugin's own copy of the removal (`FactoryStorageGone`, `TryBuildWithout`,
  `FactoryRemoveGone`) is gone; it calls `storage_retrofit::RemoveGone` with
  `FactoryGone` (on record and not active). Same rules; the "kept" lines are now limited
  by the header (40 per session). `g_serveVehicleStorages` is set in every mode - the
  shared routine leaves a building with a storage of vehicles alone without it.
- Not seen in the game in this form at the time of writing (the shared routine was, through
  Vanilla Buildings on a depot).

## 0.4.8 (2026-11-27)

- `FactoryRemoveGone` no longer stops at the first storage that stays. Every storage of the
  building is judged (`FactoryStorageGone`). When all that goes stands at the end, the end
  pointer moves back as before. Otherwise the storages that stay are moved together into a
  block the game made (`TryBuildWithout`: placeholders pushed by the game's function, then
  the bytes of the storages that stay) and the old block is left alone - the technique of
  `g_keepExisting`. The storages behind the gap stand one place earlier afterwards; whether
  the game minds that within the session was not known when this was written.
- `storage_retrofit::Prepare()` is now called in every mode (moving together needs the
  game's push).

## 0.4.7 (2026-11-26)

- Storages leave again: `FactoryRemoveGone`, after every loaded production line and before
  the missing storages are given. From the END of the building's list, one after the other:
  a single-resource import storage whose resource is on record as given by this plugin, is
  not an active material, is not in the type's own list, is empty (slot +8 and +0xC) and
  has no entries in its owner lists (+0x50, +0x68, +0xC8). The first storage that does not
  qualify ends it, so no storage ever changes its place. The vector's end pointer is moved
  back - nothing is moved, destroyed or freed (the game keeps addresses into storages).
- The record: `<loader>\plugins\vehicle_materials.storages.txt`, one resource per line,
  written when a storage is appended to a type for a material the first time.
- With `enabled = 0` or without any material the plugin stays loaded when the record is not
  empty (`g_cleanupOnly`): no resources service, no vehicle hook, only the subscription to
  `tesmio.building_load`. Without a record it stays off as before.
- Gone: `plugins\vehicle_materials.d` (`LoadPackageMaterials`) and the rule that the plugin
  runs for a package file although the shipped INI says `enabled = 0`. Republic Mod Manager
  0.8.4 enters a package's material in the personal list and sets `enabled = 1` there.
- Not seen in the game at the time of writing: a storage taken out.

## 0.4.6 (2026-11-26)

- Factories that stand: the storages a factory has are no longer cloned and freed when the
  missing ones are put in. `storage_retrofit.h` got `g_keepExisting` (`BuildRelocated`): the
  game makes a block with room for all, the existing storages are copied into it bit by
  bit, the new ones follow, and the old block is left allocated and untouched. Reason: with
  the staged swap a car factory lost its power for the session every time, and the game
  crashed twice on a pointer whose upper half had been zeroed (SOVIET64.exe+0x6CD691,
  +0x20759D in the save of a building) - something keeps addresses into what the swap
  frees. Cost: `originalCount * 0xE0` bytes per retrofitted factory stay allocated.
- Not seen in the game at the time of writing.

## 0.4.5 (2026-11-26)

- The production lines get what a material needs from the plugin, in memory (section
  "factories" of the source). Found in the game: its storage finder (mask 0x100000202C0 at
  RVA 0x448A57, building type number 40) skips every storage slot of a production line whose
  resource is not in the type's consumption or production list (type +0x898 / +0x8C8). A
  storage line alone was never enough; the same code is in 1.1.1.9.
- `ScanFactoryTypes` walks the game's `std::vector<TYPE>` (RVA 0xA30AF0, 0xE98 bytes each) on
  the first vehicle type the game builds (building types are read before) and again before
  every building of a saved game when the list changed. Per production line with a CARPLANT
  storage and per material: a consumption pair `{resource, 1.0}` through the merge helper,
  and a storage appended to the END of the type's list - a copy (game copy constructor) of a
  storage the parser made of a `$STORAGE_IMPORT_SPECIAL` line elsewhere, with class,
  capacity and slot set. A type that already has a storage for the resource keeps it.
- Factories that stand: `storage_retrofit.h` on the loader service `tesmio.building_load`,
  with `g_serveVehicleStorages` and the new `g_appendOnly` (missing storages go behind
  everything the building has). A building whose storages in front of the appended ones do
  not match its type (class and purpose) is left alone with a warning.
- New optional keys `[general] factory_storages` (default 1) and `factory_storage_capacity`
  (default 50). Addresses of this part: 1.2.0.9; port them with `storage_retrofit.h`.
- Not seen in the game at the time of writing: the scan itself (a start from the session
  stops before the game reads its data).

## 0.4.4 (2026-11-26)

- Materials of content packages: `LoadPackageMaterials` reads every `*.ini` in
  `<loader>\plugins\vehicle_materials.d` (sorted by name) after the configuration is applied. One section
  `[materials]`, one line per resource: `<resource> = <road>, <rail>, <ship>, <airplane>`, four
  numbers from 0 to the coefficient maximum, at least one above 0. A resource the player's list
  already carries keeps his coefficients; a line that cannot be read is reported and skipped; the
  list still ends at 32. Republic Mod Manager 0.8.0 writes these files, one per content package,
  when the player chooses ON on the package page.
- `enabled`: with at least one package material the plugin runs although the shipped INI says
  `enabled = 0`. It stays off when `enabled = 0` is the player's own setting - in
  `user_config\vehicle_materials.ini`, or in an INI of his in the loader's `plugins` folder
  (`g_enabledIsPersonal`). Republic Mod Manager judges the same way (`VehicleParts.Blocker`).
- No game address changed. Not run in the game at the time of writing.

## 0.4.3 (2026-11-25)

- Ported to WRSR 1.2.0.9 (PE stamp 0x6B05A574, image 0xAE9000). Requirement builder 0x419C10 ->
  0x419EC0, merge helper 0x2BD720 -> 0x2BD870, `ResourceGet` 0x2C6230 -> 0x2C6380. The resource
  manager stays at 0xA1EF20, the vehicle type fields (+0x2B8, +0x8618, +0x86CC) did not move. All
  three prologues are unchanged.

## 0.4.2 (2026-11-25)

- Ported to WRSR 1.2.0.8 (PE stamp 0x6B0097B2, image 0xAE9000). Requirement builder 0x3F8C50 ->
  0x419C10, merge helper 0x2A1840 -> 0x2BD720, `ResourceGet` 0x2AA7B0 -> 0x2C6230, resource
  manager 0x9D4F10 -> 0xA1EF20. All three prologues are unchanged.
- Vehicle object: the type moved from +0x294 to +0x2B8, the requirement vector from +0x85C8 to
  +0x8618, the weight from +0x867C to +0x86CC. They are named `VEHICLE_TYPE`,
  `VEHICLE_REQUIREMENTS` and `VEHICLE_SCALE` now and were read from the builder itself.
- The builder still tells 1 (road), 6 (ship) and 7 (airplane) apart and has one new branch for
  the types 8 and 10; those fall under the rail coefficients here unless `[vehicle_types]` says
  otherwise.
- Not run in the game yet.

## 0.4.1 (2026-11-16)

- Ported to WRSR 1.1.1.13: the vehicle requirement builder 0x3F8D10 -> 0x3F8C50, the merge
  helper 0x2A18C0 -> 0x2A1840, `ResourceGet` 0x2AA830 -> 0x2AA7B0; the resource manager stays at
  0x9D4F10. The expected PE stamp is now 0x6A8D4087 (image size unchanged, 0xA9D000); 1.1.1.9
  is refused with the build-mismatch message.
- How it was checked: `my_plugins\tests\pe.pl cmplist` against the 1.1.1.9 bytes from the
  analysed Ghidra project - builder 640 bytes, 38 differ in 22 runs; merge helper 413 bytes
  (the whole function), 2 differ in 2 runs; no run longer than four bytes, so only call targets
  and RIP-relative operands moved. The vehicle offsets the builder itself reads (`+0x294` type,
  `+0x867C` scale) are in the unchanged part.
- New: the merge helper and `ResourceGet` are called, not hooked, and used to be checked for
  readability only. Both are now compared with their prologue before any hook is installed
  (`merge-prologue`, `resourceget-prologue`). `ResourceGet` is also accepted when it starts
  with a jump, because the resources plugin has hooked it by the time this plugin starts.
- Not verified in 1.1.1.13: the meaning of the vehicle type numbers (1 road, 6 ship,
  7 airplane) - taken over from 1.1.1.9.

## 0.4.0 (2026-09-08)

- 2026-09-11, version unchanged: the detail log `tesmioloader.vehicle_materials.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before).
First published version.

- Additional production materials per vehicle class (road, rail, ship, airplane) registered
  through the Resources plugin; coefficients multiplied with the vehicle's internal production
  value; automatic vehicle-type mapping (type 1 road, 6 ship, 7 airplane, others rail) with
  `[mapping]` overrides; strict configuration validation that rejects the whole plugin on any
  error and leaves the game untouched.
- Configuration read as base plus personal overlay through `tesmio_config.h`; the base is
  `plugins\vehicle_materials.ini` when present, otherwise the INI beside the DLL; the overlay is
  merged key by key and validated with the same strict reader. The Workshop package declares
  `user_overlay = 1` and `local_copy = 1`.
- Package texts (RMM schema, de/en) in player style; the storage-line notice names
  `$STORAGE_IMPORT_SPECIAL`; schema fallbacks are English. READMEs on the common template.
