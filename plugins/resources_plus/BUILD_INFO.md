# Resources Plus – build notes

Fork of the `resources` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3,
https://github.com/MaxLegend/TesmioLoader. Target: WRSR 1.1.1.13 (SOVIET64.exe PE stamp
0x6A8D4087, SHA-256 `4DF5FBECC75300E3B843AE4E6426761C84AE2E989321C011C6F560BE79122125`),
TesmioLoader API 4.
Build: the standard line (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles
`resources_plus.cpp`; there are no other sources. Exports: `TsmPluginApiVersion`, `TsmPluginInit`.
Service name `resources`, the caption id base and the record layout deliberately stay identical
to the original so saves, content packages and consumers such as Deposits Plus keep working.
User documentation: README_DE.md / README_EN.md. History newest first, technical notes below.

## 0.1.5 (2026-11-18)

- The first list file is also made from the original's `resources.ini` when the original lies
  only in a Workshop item. `ResolveIniPath` looked for that file in `<loader>\plugins` only and
  fell back to the shipped template; Republic Mod Manager (`ForkSeed`) has taken the INI beside
  the original's Workshop DLL all along, so the two rules gave different lists depending on who
  created the file. New order: `<loader>\plugins\resources.ini`, Soviet Mod Loader's baseline,
  `resources.ini` beside the original's DLL in the Workshop folder, the template beside the own
  DLL.
- `FindOriginalIniInWorkshop` follows the loader's discovery: nothing is searched when
  `<loader>\plugins\resources.dll` exists (the loader takes that one, and its INI is the first
  source); the Workshop folder is `tesmioloader.ini [tesmioloader] workshop_path` or
  `<...>\steamapps\workshop\content\784150` found by walking up from the loader folder; of several
  `resources.dll` the smallest path wins (`SmallestDllBelow`, depth 8, reparse points skipped).
  Difference to the loader: the file is taken by its name, its exports are not checked.
- Runs once, only when the list file is missing. Walking the developer's Workshop folder (667
  items, about 3000 folders) takes about 0.4 s.
- Checked offline with a harness that includes the plugin source and builds fake Steam trees
  (scratchpad): local file over Workshop copy; smallest path of two Workshop copies, one deep,
  with a stray INI without DLL elsewhere; local DLL without INI (no Workshop lookup); Workshop
  DLL without INI; `workshop_path` override; loader outside `steamapps`; original only in the
  Workshop - seven cases, all as expected. Not run in the game.
- The default lists of the original and of this plugin are the same ten resources, so the change
  matters only when the two differ.

## 0.1.4 (2026-11-18)

- Translated names. `[list]` gives a resource one caption, shown in every game language. New
  sections `[names:<Language>]` with `<name> = <caption>` give a caption for one language;
  `<Language>` is the middle of the game's language file name (`German` for `sovietGerman.btf`).
  What is shown: the caption for the game's language, else the `[list]` caption, else what was
  shown before (the template's caption for a clone without a caption, the name for a record
  built from scratch).
- How the language is known: the game's config says `auto` (the Steam language), so it is not read
  there. The game calls `C3D_LANGUAGE::Initialize(char* file, char* fallbackFile)` once at start
  with `sovietGerman.btf`, `sovietEnglish.btf` (call site in the function that compares the
  language name, checked in the 1.1.1.13 executable: `rdx` = language file, `r8` = English).
  New IAT hook on that import, `h_LanguageInit` -> `NoteLanguageFile`; installed only when the
  file has at least one name line and the GetString slot was found. Without the slot the
  `[list]` captions are shown as before (WARN in the log). `sovietComments.btf` is ignored, and
  so is a call on a second language object.
- `h_GetString` answers a minted id with the entry's name for the current language
  (`ResEntry::shown`, set by `ApplyLanguage`), else as before. A clone that has translations but
  no `[list]` caption gets a text id of its own (`inherit`); the caption id of its template is
  kept when the record is built (`tmplTextId`) and answered through the original GetString in
  a language without a translation.
- The list file in the loader folder is the player's and is not replaced by an update, so the
  `[names:]` sections of the INI beside the DLL (`resources_plus.template.ini`, else
  `resources_plus.ini`) are read as well, after the player's (`LoadShippedNames`). The player's
  line for a resource and a language wins; a shipped name for a resource that is not in `[list]`
  is dropped without a log line.
- Text ids, record layout, the order of `[list]` and everything a save depends on are unchanged;
  names are captions only. A file without `[names:]` sections behaves exactly as 0.1.3 (no
  additional hook).
- The shipped INI carries `[names:German]` for its ten resources.
- Checked offline with a harness that includes the plugin source (scratchpad, not part of the
  project): own name over shipped name, shipped name filling a gap, fallback to the `[list]`
  caption and to the template's caption, switch of the language on the same object, the comments
  file, a second object, malformed names - 20 expectations, all met. Not run in the game yet.

## 0.1.3 (2026-11-16)

- Ported to WRSR 1.1.1.13. Three code addresses moved, nothing else did:
  `ResourceGet` 0x2AA830 -> 0x2AA7B0, the price pass 0x2A9340 -> 0x2A92C0, the customhouse
  tick 0x1854E0 -> 0x185500. `std::vector<Slot>::push_back` stays at 0x0B14E0, the resource
  vector at 0x9E11C0 and `C3D_MIDDLEPOINT` at 0x9EACD0 (`.data` did not move).
- How it was checked: the bytes of each function in 1.1.1.9 (read from the analysed Ghidra
  project) against the 1.1.1.13 executable on disk, with `my_plugins\tests\pe.pl cmp`.
  `ResourceGet`: 400 bytes, 10 differ in 7 runs, all inside call targets. Price pass: 400 bytes
  identical. Customhouse tick: 516 bytes, 16 differ in 12 runs, all call targets or RIP-relative
  operands. No structure offset changed in any of them (`self+0xC2B0`, the 0x340 record stride,
  `+0xFC0`, `+0x1234`). The DLL MaxLegend ships for 1.1.1.13 carries the same six addresses.
- An address key left in `plugins\resources_plus.ini` by an older game build no longer disables
  the plugin: `PickRva` drops an INI value whose bytes do not match when the built-in address
  does match, and says so in the log. When neither matches the hook refuses as before.
- The brake against the original now asks for the loaded module (`resources.dll` exporting
  `TsmPluginInit`) as well as for the file in `plugins\`. Since loader b0.3.7 a plugin can be
  loaded from anywhere below the Steam Workshop folder, and the loader inits in name order, so
  an original that is switched on is in the process before `resources_plus` is asked.
- Not changed: `ResolveIniPath` still looks for the original's `resources.ini` only in the
  loader's own `plugins\` folder. An original that lives in a Workshop item keeps its INI
  beside its DLL; taking that list over is left to Republic Mod Manager.
- Comments further down in the source still name 1.1.1.9 addresses for functions the plugin
  does not call (the resource table builder, the storage parser, the solver). They are notes,
  not inputs, and were not looked up again.

## 0.1.2 (2026-09-20)

- Audit round, no new feature; everything below was inherited from the original 1.7 unless noted.
- `ExtendCustomsStorage` accepts a transport class only below `RES_CLASS_COUNT` (18); the old
  guard allowed up to 31 and read class blocks past the 832-byte record.
- `h_CustomsTick` runs under `g_lock` (recursive, so the engine's push_back calling back into
  ResourceGet on the same thread cannot deadlock), checks `end >= begin` before computing the
  storage count, and `std::vector<Slot>::push_back` at `P_CUSTOMS_SLOT_PUSH_RVA` is verified
  against its 14-byte prologue before it is ever called - a different build disables the customs
  sync instead of calling into the wrong function.
- `h_ResourceGet`: `EnsureArmed` and the reserved-slot safety net run under `__try` with
  `FaultFilter`; a fault drops the hook to observe-only (`g_resHook = 1`). `AttachResourceMeshes`
  uses `FaultFilter` instead of a bare `EXCEPTION_EXECUTE_HANDLER`. `h_PricePass` stops applying
  prices after a fault (`g_priceFaulted`) instead of faulting at every recompute.
- `PatchIat` for `C3D_LANGUAGE::GetString` logs a WARN when the slot is missing.
- INI: `resource_vector_rva` must lie inside the image and be readable, `resource_capacity` must be
  -1, 0 or 1..4096, `packed` must be 0, 1 or auto (auto now leaves the transport rule ON - it used
  to switch it off), a numeric `family` must be -1 or one of the ten known values; each refusal is
  a WARN line. `LoadResourceRegistry` skips a UTF-8 BOM and reports a short read.
- Files (fork code): `ReadWholeFile` fails on a short read instead of returning a truncated buffer,
  `WriteWholeFile` keeps the Windows error in `g_ioError` before removing the temporary file, and
  `ResolveIniPath` guards the header length against `_TRUNCATE` returning -1.

## 0.1.1 (2026-09-18)

- The Soviet Mod Loader guard fires on the FILE and its switch, not only on the module handle:
  the loader inits `plugins` in name order, `resources_plus` before `soviet_mod_loader`, so at
  init time SML was not in the process yet, this plugin hooked the registration in mode 2, and
  SML's embedded resources found its site taken ("resources registration hook was not installed
  in mode 2", game start blocked). Now `plugins\soviet_mod_loader.dll` present plus its key in
  `tesmioloader.ini` not 0 is enough to step aside, whichever of the two inits first.

## 0.1.0 (2026-09-18)

- First release as a fork. Same hook sites and behaviour as the original 1.7, plus:
- **The moved resource array no longer crashes the game on exit.** The original allocates the
  bigger block with a plain `malloc` from ucrtbase. The engine's `std::vector` frees it through
  MSVC's "manually vector aligned" path for blocks of 4096 bytes or more (`rva 0x1A50`): the
  pointer must be 32-byte aligned, the qword before it must hold the raw allocation pointer, and
  that pointer must lie 8..39 bytes before the block. A plain block has none of that, so
  `_invalid_parameter` fired (`c0000409`, parameter 5, ucrtbase+0x11858) every time the game
  exited with more than six mod resources declared. `AlignedProcessAlloc` builds the expected
  layout; the heap stays the process CRT's, so the engine's `free` still owns the block.
- **Its own file, created once from what the player has.** Reads `plugins\resources_plus.ini`.
  When it is missing, `ResolveIniPath` creates it from `plugins\resources.ini` (the original
  plugin's file, copied verbatim with a header that names the source and date, so the order of
  `[list]` - part of every save - is preserved); when that file is Soviet Mod Loader's generated
  output (first line "generated by Soviet Mod Loader"), from `<sml state>\base\resources.ini`
  instead, because the generated file already carries every package and RMM provides the packages
  again; otherwise from `resources_plus.template.ini` (or `resources_plus.ini` in a Workshop package
  folder) beside the DLL, the shipped template. Written through
  a temporary file and one rename. The `[resources]` and `[customs]` sections are read with the
  profile API against that path instead of the host's `configInt`.
- **Duplicate names are refused.** A name listed twice in `[list]` is skipped with a WARN line; the
  original registered it twice (two records, two caption ids, the second winning every lookup).
- **Steps aside instead of doubling up.** When `plugins\resources.dll` exists and is enabled in
  `tesmioloader.ini`, or when `soviet_mod_loader.dll` (or `000_soviet_mod_loader.dll`) is loaded
  with `embedded_plugins` on, `TsmPluginInit` logs one line and returns 1 before anything is
  hooked. SML's `ValidateEmbeddedRuntime` ends the game on a second registrar, so this is not
  optional.
- Detail log `logs\tesmioloader.resources_plus.log` (the original wrote `tesmioloader.resources.log`
  next to the loader log).

## Technical notes

- Every address is for SOVIET64.exe v1.1.1.13 and is verified against expected bytes before a hook
  installs; a game update makes the hooks refuse rather than corrupt the process. See the header
  of `resources_plus.cpp` and `docs/04-adding-resources.md` for the engine facts.
- The relocation of the array happens once per process on the very first lookup, ahead of the
  engine's own init loop; the old block is deliberately left alone. Nothing in this fork changes
  that timing.
