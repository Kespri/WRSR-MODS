# tesmioloader.dll – build notes

The loader itself: MaxLegend's TesmioLoader b0.3.7 (GPL v3, https://github.com/MaxLegend/TesmioLoader)
with the corrections below. Target: WRSR 1.1.1.13 (SOVIET64.exe PE stamp 0x6A8D4087), plugin
API 4, accepts plugins built for API 3..4. Build: the loader line of the root `build.bat`
(`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib; user32 is pulled in by a pragma for the
one message box). Offline self-test: compile the same file with `/DTESMIO_SELFTEST` as an EXE
and run it with a fresh scratch folder and, optionally, any built plugin DLL - 49 checks over
the ini reader, the VFS path rule, plugin discovery, the INI a plugin is handed, the
user_config overlay, the service noticeboard, the save manifest and the repeat counter.

Compatibility promise: the host table, the API version, the export names, the two-phase
Init/Start rule, the `[plugins]` switches, the launcher's ini keys and every log line other
tools read (`plugin <name> <version> from <dll>`, `<name>  idle - `, `plugin N loaded`,
`--- shutdown`, the service lines) are those of upstream b0.3.7. A plugin built for upstream
runs here; a plugin built here runs on upstream. The launcher is upstream's, unchanged.
Republic Mod Manager ships this DLL and installs it over the upstream one.

## b0.3.7-rmm.2 (2026-11-18)

- `api_IniPath`: a plugin asking for `plugins\<its name>.ini` gets `<loader>\plugins\<name>.ini`
  when that file exists, and the INI beside its DLL otherwise. Upstream always hands out the INI
  beside the DLL. For a DLL in `<loader>\plugins` both are the same file; the difference is a DLL
  loaded from a Workshop item. Why: Republic Mod Manager writes the finished INI of such a
  plugin into the loader folder (list editors always, and packages whose plugin opens its INI by
  path and never sees `user_config`), and the plugins that resolve their INI themselves follow
  the same order already - loader folder first, then beside the DLL. A plugin such as Deposits
  Plus, which parses its list by path and reads its switches through the host table, took the
  two from different files otherwise.
- `user_config\<name>.ini` is still laid over whichever file answers.
- Self-test: 49 checks (two new: the file in the loader folder wins; the INI beside the DLL
  answers again once it is gone). Not in the game yet.

## b0.3.7-rmm.1 (2026-11-16)

Upstream b0.3.7 (game 1.1.1.13, plugin discovery in the Steam Workshop folder, wide paths,
no plugin limit) merged with everything b0.3.6-rmm.1 carried: a three-way merge of upstream
3baa141 (b0.3.6), upstream 775b9d9 (master, "v0.4.2") and our tree, 14 conflicts resolved by
hand. `src\tesmio_discovery.h` is upstream's, unchanged. Still ours on top of b0.3.7:

- the save manifest from the live services, with the corrected key reader (upstream still
  cuts `key=value` with `*eq = 0` and then steps by `strlen(p) + 1`, into the value, and
  still keys the manifest on the plugin names resources/deposits/buildings);
- services of a plugin that declines are withdrawn before `FreeLibrary`;
- the wide VFS root (upstream decodes the ANSI root as UTF-8), `..` refused in VFS paths;
- trampolines left read+execute, the menu string read-only, readable-pointer check before
  the byte compare of an inline hook;
- the readable log: header with loader build, game path, image size and PE stamp (now
  naming 1.1.1.13), `log_game` as a level, repeated game messages counted, every redirected
  file named once, the `summary` line, the previous log kept as `logs\tesmioloader.previous.log`,
  the module list in a crash report; 64 services.

New in this build:

- **Personal values for every plugin.** `<loader>\user_config\<name>.ini` is laid over a
  plugin's INI key by key in `configInt` / `configString`: a key found there wins, the rest
  comes from the INI beside the plugin's DLL. Republic Mod Manager writes that file, so an
  INI in a Workshop folder is never touched and survives the item's updates. One log line per
  file (`config   <name>.ini: personal values from user_config\<name>.ini are laid over it`).
  A plugin that opens its INI by path and parses it itself is out of reach; the plugins in
  `my_plugins` already merge the same file themselves (`tesmio_config.h`).
- **The `[plugins]` switches go through the loader's own reader** (BOM-tolerant) with the
  discovered key, and the loader's ini is opened by its wide path.
- One `plugin   scan:` line naming how many plugin DLLs were found and the two places
  searched; a plugin whose path is too long is named instead of skipped silently.
- Dropped: the 128-file list and the name sort of rmm.1 (discovery sorts by key itself).

Not taken over on purpose: nothing. Not verified in the game yet at the time of writing.

## b0.3.6-rmm.1 (2026-09-20)

- **Save manifest from the live services.** `tesmioloader.save.ini` used to list resources
  only when a plugin named `resources` was loaded, deposits only for `deposits`, buildings
  only from `plugins\buildings.ini` - with Resources Plus, Deposits Plus, Buildings Plus or
  Soviet Mod Loader it was empty, and the load check said "resources.dll is off". Now the
  writer asks the `resources` v1 and `deposits` v2 services (the original plugins, the forks
  and SML all publish them) and lists generated buildings as the stamped, numbered folders
  under every `out` dir (`plugins\buildings.ini` and `plugins\buildings_plus.ini` are read for
  it); the checker asks the same services (`indexOf`, `get`) and falls back to the provider's
  own ini (`resources_plus.ini` before `resources.ini`, `deposits_plus.ini` before
  `deposits.ini`) only when no service is published. Relative save paths resolve the game
  folder to the working directory instead of skipping the buildings check. The manifest
  also records `[tesmioloader] loader`.
- **Withdrawn services.** A plugin that published a service in Init and then declined (returned
  non-zero) was unloaded with its interfaces still on the noticeboard, so `consume()` handed
  out pointers into a freed DLL. `DropServices` removes them before `FreeLibrary`.
- **Non-ASCII install paths.** `g_vfsRoot` came from `GetModuleFileNameA` and was re-decoded as
  UTF-8 in `VfsResolveW`, so every wide file open (textures) missed the VFS when the game
  folder held an umlaut or a Cyrillic letter. The loader now keeps the wide paths
  (`g_baseDirW`, `g_vfsRootW`) and derives the ANSI copies for the plugin table with CP_ACP.
- **Deterministic plugin order.** File names are collected, sorted case-insensitively and then
  loaded; `FindFirstFile` order is only alphabetical on NTFS.
- **BOM-tolerant ini.** `tesmioloader.ini` is read by the loader's own reader (`LoadIniText`,
  `IniReadValue`, `IniInt`): a UTF-8 BOM no longer hides the `[plugins]` section. The profile
  API stays behind `configInt`/`configString` for plugins, unchanged.
- **VFS root is a root.** A relative path with a `..` step is never redirected.
- **Read-only after writing.** Inline-hook trampolines are switched to PAGE_EXECUTE_READ and the
  menu version string to PAGE_READONLY once written; `allocNear` stays RWX because plugins
  write their own code into it. `installInlineHook` also checks that the target is readable
  before comparing the prologue.
- **Log.** Header with loader version, build time, API range, date, the game executable, its
  image size and PE stamp (with a note when it is not the 1.1.1.9 build the addresses were taken
  from), the base folder; a `summary` line before `--- hooks installed ---` with hooks placed
  and refused, plugins and services; the crash report lists the loaded modules on the first
  report; the previous run survives as `logs\tesmioloader.previous.log`.
- **A log worth reading.** One ordinary start wrote 952 lines, 104 KB, and most of it was the
  same thing over and over: the VFS naming a file at every open (the engine opens one `.nmf`
  eight times, 116 lines for 26 files) and the game complaining about every folder it walked
  (47 `Failed to open ...` in a row, all about other people's Workshop items). Three changes:
  - the VFS names a redirected file **once**, and `ReportRepeats` writes the totals at shutdown;
  - a repeating message of the game is written `game_repeat_limit` times (default 3) and then
    counted. `Shape()` decides what "the same message" means: a word holding a path separator
    becomes `<path>`, a word of digits becomes `#`, the wording is what is left. The tally is in
    the log at shutdown (`repeats  47 x Failed to open <path>`), so nothing disappears silently.
    A message that leaves **no** wording behind is never counted together: the game prints bare
    numbers (`1`, `2`, `3`) as separate warnings, and grouping those would hide nine distinct
    messages behind a shape that reads `#`. Measured in the first run with a real game;
  - `log_game` is a level, not a switch: 0 off, 1 errors, **2 errors and warnings (the new
    default)**, 3 everything. A `log_game = 1` in an existing ini now means errors only - that is
    the point, and the header line says which level is running.
  The game's own `log.html` is untouched: every call is still forwarded to the engine.
- Limits raised: 64 plugins, 64 services, 128 files considered in `plugins\`.
- `CheckSaveManifest` returns the number of missing items (for the self-test); the message box
  can be silenced for tests only (`g_noMessageBox`). The box now names Republic Mod Manager.
