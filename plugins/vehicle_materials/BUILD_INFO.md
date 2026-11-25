# Vehicle Materials – build notes

TesmioLoader plugin (API 4) for WRSR 1.2.0.9 (PE stamp 0x6B05A574), GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /LD`, kernel32.lib) compiles `vehicle_materials.cpp`; it
includes `src/tesmio_plugin.h` and `my_plugins/tesmio_config.h` (base INI beside the DLL or in
`plugins\`, personal overlay `user_config\vehicle_materials.ini`). User documentation: README_DE.md / README_EN.md. History newest first.

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
