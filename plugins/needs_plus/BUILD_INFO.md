# Needs Plus – build notes

Fork of the `needs` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3,
https://github.com/MaxLegend/TesmioLoader. Target: WRSR 1.2.0.9 (SOVIET64.exe PE stamp
0x6B05A574, SHA-256 `3EB4649C01C6A4E01F86ACE3BD8135D6359EC3774CF93E17AB69C0F1B65E6D28`),
TesmioLoader API 4.
Build: the standard line (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles
`needs_plus.cpp`; there are no other sources. Exports: `TsmPluginApiVersion`, `TsmPluginInit`,
`TsmPluginStart`. Hook sites, demand shape and storage slot logic deliberately stay identical to
the original so saves and content packages keep working.
User documentation: README_DE.md / README_EN.md. History newest first, technical notes below.

## 0.1.5 (2026-11-25)

- Ported to WRSR 1.2.0.9 (PE stamp 0x6B05A574). Person plan 0x877D80 -> 0x878270, storage build
  0x0FBCD0 -> 0x0FBB60, shop tick 0x18AA90 -> 0x18A920, type loader 0x1361F0 -> 0x136080, slot
  `push_back` 0x0C9150 -> 0x0C8FE0.
- The game object grew by 8 bytes in front of these fields: building types 0xA30AE8 -> 0xA30AF0,
  resource vector 0xA2B268 -> 0xA2B270, goods buckets +0x127D0 / +0x12830 -> +0x127D8 / +0x12838.
  Type stride (0xE98), record stride (896) and the 20 transport classes are unchanged.

## 0.1.4 (2026-11-25)

- Ported to WRSR 1.2.0.8: the citizen's daily plan 0x836CE0 -> 0x877D80, the storage builder
  0x0E40F0 -> 0x0FBCD0, the shop tick 0x171E30 -> 0x18AA90, the building type loader 0x11D800 ->
  0x1361F0, the type vector 0x9E6A30 -> 0xA30AE8, `push_back` 0x0B14E0 -> 0x0C9150, the resource
  vector 0x9E11C0 -> 0xA2B268.
- Layout: a building type is 0xE98 bytes (0xBE8 before; its storage vector moved from +0x3A0 to
  +0x3A8, inside the scanned range), a resource record 896 bytes with twenty transport classes,
  the five goods buckets of the game object start at +0x127D0 (+0x12720 before). The two class
  range checks use `RES_CLASS_COUNT` instead of the literals 17 and 31.
- Verified with `portmap.pl` against the 1.1.1.9 bytes (`portlists\needs_plus.cmp`; 433 of 512 to
  612 of 640 bytes equal per function), the bucket offsets by the two instructions that use each,
  and against MaxLegend's port, which carries the same values. Not run in the game yet.

## 0.1.3 (2026-11-18)

- The first list file is also made from the original's `needs.ini` when the original lies only
  in a Workshop item. `ResolveIniPath` looked for that file in `<loader>\plugins` only and fell
  back to the shipped template; Republic Mod Manager (`ForkSeed`) has taken the INI beside the
  original's Workshop DLL all along. New order: `<loader>\plugins\needs.ini`, Soviet Mod Loader's
  baseline, `needs.ini` beside the original's DLL in the Workshop folder, the template beside the
  own DLL.
- `FindOriginalIniInWorkshop` and `SmallestDllBelow` are the same code as in Resources Plus 0.1.5:
  nothing is searched when `<loader>\plugins\needs.dll` exists; the Workshop folder is
  `tesmioloader.ini [tesmioloader] workshop_path` or `<...>\steamapps\workshop\content\784150`;
  the smallest path of several `needs.dll` wins; the file is taken by its name, its exports are
  not checked. Runs once, only when the list file is missing.
- Checked offline with the same harness as Resources Plus (seven cases, all as expected). Not run
  in the game.
- The default lists of the original and of this plugin are the same two needs, so the change
  matters only when the two differ.

## 0.1.2 (2026-11-16)

- Ported to WRSR 1.1.1.13. Two of the four hooked functions moved: the citizen's daily plan
  0x836B40 -> 0x836CE0 and the shop tick 0x171E10 -> 0x171E30. The storage builder (0x0E40F0)
  and the building type loader (0x11D800) stayed, as did `std::vector<Slot>::push_back`
  (0x0B14E0), the type vector (0x9E6A30) and the resource vector (0x9E11C0).
- How it was checked: 512-640 bytes of each function in 1.1.1.9 (from the analysed Ghidra
  project) against the 1.1.1.13 executable with `my_plugins\tests\pe.pl cmplist`; every
  difference is a run of at most four bytes (call targets, RIP-relative operands), no structure
  offset changed. The `add rbx,0xBE8` of the type-vector clear is still there (type stride).
  The DLL MaxLegend ships for 1.1.1.13 carries the same seven addresses.
- `std::vector<Slot>::push_back` is now verified against its 14-byte prologue before it is
  ever called; a different build switches the storage half off instead of calling into the
  wrong function. `FixOtherGoodsBucket` does nothing without it.
- The brake against the original now asks for the loaded module (`needs.dll` exporting
  `TsmPluginInit`) as well as for the file in `plugins\`: since loader b0.3.7 the original can
  be loaded from a Workshop folder, and it inits before `needs_plus` in name order.
- Not changed: `ResolveIniPath` still looks for the original's `needs.ini` only in the loader's
  own `plugins\` folder.

## 0.1.1 (2026-09-20)

- Audit round, no new feature; the hook sites and the demand/storage logic are unchanged.
- Detail log `logs\tesmioloader.needs_plus.log`, opened in `TsmPluginInit` after `ReadSettings` through
  `TsmOpenLog`; `Probe()` writes there (the `probe = 1` dumps and every WARN about INI values), the
  summary lines stay in `tesmioloader.log`.
- `ListNumber` validates the three numbers of a `[list]` line: factor 0.001..100, chance 0..1,
  unhappiness 0..1; an out-of-range or unparsable value falls back to the default with a WARN line.
  `ReadSettings` does the same for `max_demands` (1..7), `when_full` (skip|replace) and
  `log_seconds` (0..86400).
- `LoadRegistry` reads the INI through `ReadWholeFile`, skips a UTF-8 BOM (which used to hide
  `[list]`), warns once when more than `MAX_NEEDS` (16) lines are declared, and frees the buffer.
- `ShopAlreadyDone` runs under `g_lock`; `LogProgress` throttles with `InterlockedCompareExchange`;
  `g_probeStorages` is a LONG counted with `InterlockedIncrement`.
- Both `FixOtherGoodsBucket` handlers log a fault and disable the fix (`g_bucketFix = 0`) instead of
  faulting again on the next citizen; every fault label carries the plugin name.

## 0.1.0 (2026-09-18)

- First release as a fork. Same hook sites and behaviour as the original 1.0, plus:
- **Its own file, created once from what the player has.** Reads `plugins\needs_plus.ini`.
  When it is missing, `ResolveIniPath` creates it from `plugins\needs.ini` (the original
  plugin's file, copied verbatim with a header that names the source and date); when that file
  is Soviet Mod Loader's generated output (first line "generated by Soviet Mod Loader"), from
  `<sml state>\base\needs.ini` instead, because the generated file already carries every
  package and RMM provides the packages again; otherwise from `needs_plus.template.ini` (or
  `needs_plus.ini` in a package folder) beside the DLL, the shipped template. Written through a
  temporary file and one rename. The `[needs]` section is read with the profile API against
  that path instead of the host's `configInt`, so the fallback chain is one place.
- **Duplicate names are refused.** A resource listed twice in `[list]` is skipped with a log
  line; the original built two needs from it.
- **Steps aside instead of doubling up.** When `plugins\needs.dll` exists and is enabled in
  `tesmioloader.ini`, or when `soviet_mod_loader.dll` (or `000_soviet_mod_loader.dll`) is present
  and switched on or already loaded, with `embedded_plugins` on, `TsmPluginInit` logs one line
  and returns 1 before anything is hooked. The file-plus-switch test matters because the loader
  inits `plugins` in name order and `needs_plus` comes before `soviet_mod_loader`.
- Log lines carry the prefix `needs_plus`; the messages that told the player to declare a
  resource "in plugins\resources.ini" now name the resources plugin's list in general, because
  under Resources Plus that file is `plugins\resources_plus.ini`.
- No detail log: like the original, everything goes to `tesmioloader.log` (`probe = 1` adds the
  citizen and storage dumps there).

## Technical notes

- Every address is for SOVIET64.exe v1.2.0.9 and is verified against expected bytes before a hook
  installs; a game update makes the hooks refuse rather than corrupt the process. See the header
  of `needs_plus.cpp` and `docs/11-needs.md` for the engine facts.
- The resource a need names is resolved lazily through the `resources` service, so it does not
  matter whether Resources Plus or the original resources plugin provides it.
