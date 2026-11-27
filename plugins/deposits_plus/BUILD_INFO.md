# Deposits Plus – build notes

Fork of the `deposits` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3. Target:
WRSR 1.2.0.9 (SOVIET64.exe SHA-256 `3EB4649C01C6A4E01F86ACE3BD8135D6359EC3774CF93E17AB69C0F1B65E6D28`),
TesmioLoader API 4. Build: the standard line of the root `build.bat` (`cl /O2 /MT /W3 /EHsc /LD`,
kernel32.lib) compiles `deposits_plus.cpp`; the headers `deposit_generation.h`, `deposit_country.h`,
`deposit_visual_shader.h` and the `.inl` files are included, `third_party` holds Microsoft's DXIL
hash helper (see below). Service name `deposits` and the savegame file `tesmio_deposits.bin`
deliberately stay identical to the original so consumers such as Depletion keep working.
User documentation: README_DE.md / README_EN.md. History newest first, technical notes below.

## 0.4.21 (2026-11-27)

- The package settings page (`config\languages`) in 18 more languages - bg, cs, es, fr, hu, it, ja, ko, pl, pt, ro, ru, sk, sr, tr, uk, zh-CN, zh-TW - translated for the user and checked (keys, placeholders, `\n`, paths); code unchanged apart from the version string.

## 0.4.20 (2026-11-27)

- Rebuilt with `/DTSM_RELEASE_VERSION=0.4.6` for TesmioLoader b0.4.6 (launcher 0.4.6 warns about every plugin built for 0.4.5); code unchanged apart from the version string.

## 0.4.19 (2026-11-25)

- Ported to WRSR 1.2.0.9 (PE stamp 0x6B05A574; engine stamp 0x6B05A4A4). 82 address constants were
  carried by the instruction-by-instruction comparison of both builds (`my_plugins\tests\rebase.pl`);
  code moved by -0x170 .. +0x4F0 from 0x091180 on, the float pool in `.rdata` by +0xB8.
- Offsets inside the game object from +0xBCB8 on are 8 higher: the editor's active tool
  +0xD4C0 -> +0xD4C8, the resource vector 0xA2B270, the tooltip buffer 0xA2C558 -> 0xA2C560.
  Terrain (+0xF78), the resource maps (+0xFA0 / +0xFA8) and the brush cursor (+0x1120) stayed.
- Expected bytes: the parser LEA and the two resource-map brackets carry new displacements
  (`kParserOrig`, `kMapOpenOrig`, `kMapCloseOrig`); every other compared block is byte-identical at
  its new address (`arraycheck.pl`).
- Sand surface: the engine DLL of this build has the stamp 0x6B05A4A4; its code is byte-identical to
  the previous one (31 bytes of `.rdata` differ), so the texture hooks keep their addresses.

## 0.4.18 (2026-11-25)

- Ported to WRSR 1.2.0.8. 89 constants shared with the upstream plugin were taken from
  MaxLegend's port and each compared with an own measurement (`portmap.pl` with
  `portlists\dp.map`, the "references seen" list, and the float constants read back by value).
- Four upstream values were wrong or missing and are NOT taken over: the radius table sits eight
  bytes too high there (it reads 134, 213, 51.2 ...; ours reads 130, 210, 50, 30, 250, 170, 150
  at 0x95211C, 0x952270, 0x951F5C, 0x951ED4, 0x9522F0, 0x9521BC, 0x952158); `G_HITBOX_HALF` is
  0x951BB8 (5.0), `G_ED_BUTTON` 0x9513BC (0.85); the editor's active tool moved from +0xD428 to
  +0xD4C0 (old `mov rbx,[rdi+0xD428]` / `mov byte [rdi+0x10F0],r13b`, new 0xD4C0 / 0x1120).
- Layout: the deposit type of a building +0x368 -> +0x370, the building type in the type
  descriptor +0x360 -> +0x368, the resource maps +0xFA0/+0xFA8, the terrain +0xF78, the parser
  locals 0x4C50 / 0x1E18 / 0x1E20, the sample buffer `rbp+0xC0`, the brush cursor flag 0x1120,
  the tool's icon path +0xB5. The Rocks panel pushes R12 and R15 now (22-byte prologue).
- The four working-vehicle gates (this fork only) were read again: 0x18FC18 (guard 0x18FC04),
  0x1CA56A (0x1CA563), 0x402C09 (0x402BA4) and 0x69F274 (0x69F264). The eligibility test ends in
  a near JNE now, so that site is fifteen bytes, and its descriptor register is RSI.
- `my_plugins\tests\bytearrays.pl` compares all 23 expected byte strings with the executable at
  the new addresses: all match. The minimap object (+4 six flags, +0x48..+0x58), the engine's
  texture vtable slots 2, 16, 18 and 36 and text id 0x2F3 were checked and did not change. The
  editor button drawer keeps its nine arguments (call site compared in both builds).
- Two guards against the engine DLL (C3DDLL64.dll, PE stamp 0x6B004BD7, image 0x1F3000) were
  re-read after a first start showed both refusing: the country border vector is still at
  terrain+0x8A0, but the exported `C3D_TERRAIN` constructor zeroes it from +0x1F on with an XORPS
  between the first and the other two stores (one block at +0x17 before); the sand surface checks
  the new stamp and the device accessor in `CreateShaders` at +0x2BA with its new displacement
  (`4C 8B 0D 3F 17 1C 00`).
- Start on 1.2.0.8 (init and start phase, read from the loader log): every patch site accepted,
  `country-guard=1`, four native vehicle gates installed, four brush pairs hooked, the sand
  surface hook prepared. No world was loaded yet; the signatures of the editor and minimap
  functions the plugin calls were not checked one by one.

## 0.4.17 (2026-11-19)

- `AdoptOriginalDeposits` runs at every game start for as long as there is no
  `<loader>\plugins\deposits_plus.ini`, and it creates that file only when the original has
  deposits of its own - so for most players it ran its search forever. The search walked the whole
  Workshop content folder (every file of every subscribed item, depth 8) for `deposits.dll`.
  New `AdoptFindDllInWorkshop`: the DLL is looked for only where a TesmioLoader package keeps a
  plugin - `<item>\<dll>`, `<item>\plugins\<dll>`, `<item>\plugins\<folder>\<dll>` - which is one
  listing of the Workshop root plus two attribute checks per item. The smallest path still wins.
  A copy buried deeper is not found by the plugin any more; Republic Mod Manager, which asks the
  loader's own discovery, still takes such deposits over.
- Checked without the game: the takeover harness, 20 checks (four new: folder below `plugins`,
  directly in the item, a copy buried deeper is ignored, the smaller path of two items counts).

## 0.4.16 (2026-11-18)

Two faults of a brush whose deposit has no pictures and names no resource, seen in the game with
a deposit taken over from the original (user's screenshot: a fifth brush pair with the gas
picture, called "Bauxite").

- Picture. `LoadToolIcon` asked the engine for `editor/tool_<verb>_<name>.png` without looking
  whether the file exists. `CreateManagedTexture` hands out a texture object whatever happens and
  `Load2DFromFile` reports nothing, so the "did not load - keeps bauxite's icon" branch never ran
  and the button showed what the texture last held: the picture of the brush loaded just before.
  New `ToolIconFileExists`: the engine's own file check (`C3DHelp_CheckIfFileExist`, which sees
  the loader's VFS; the slot is now also taken in `InstallEditorPatch`, not only when there are
  extra maps), and as a second opinion the file directly under `<vfs>\media_soviet` and under the
  game's `media_soviet`. A missing picture is not loaded; the button keeps the donor's icon and
  the WARN line is written, as the code always meant to.
- Caption. The hover text comes from the record of the resource the deposit's `icon` names; with
  no such resource the donor's caption stayed ("Bauxite"), so two brush pairs carried the same
  name. Such a brush now gets the deposit's own name (section name, first letter capitalised)
  under a text id of this plugin: `DEP_TEXT_ID_BASE` 1100000 + the deposit's index, answered by a
  new IAT hook on `C3D_LANGUAGE::GetString` (`h_DepGetString`); every other id is passed on. The
  hook is installed with the editor patch when at least one brush is declared (one more hook in
  the loader's summary). A deposit whose `icon` names an existing resource is unchanged.
- Checked offline (scratchpad harness including the plugin source): file check with and without
  the engine's answer, own caption id and text, pass-through of other ids - 12 checks, all met.
  The take-over harness of 0.4.15 still passes. Not run in the game yet.

## 0.4.15 (2026-11-18)

- Deposits the player added to the original plugin's `deposits.ini` are taken over once. Their
  type numbers and tokens are part of his saves, and this plugin's list did not know them: it read
  `<loader>\plugins\deposits_plus.ini` or the INI beside the DLL and never looked at the
  original's file.
- `AdoptOriginalDeposits`, called at the top of `LoadDepositRegistry`: when the loader folder has
  no `deposits_plus.ini`, the original's `deposits.ini` is looked for in `<loader>\plugins` (the
  only place the original itself reads) and otherwise beside the original's DLL in the Workshop
  folder (the loader's discovery rule, as in Resources Plus 0.1.5). Every section of it that is a
  deposit (not `[deposits]`, the original's settings) and that the shipped list does not have by
  name is appended to a copy of the shipped INI, block for block with its comment lines; the file
  gets a header naming both sources. A section whose type number or token the shipped list - or a
  section taken just before - already uses is left out with a WARN line.
- Nothing is created when there is nothing to take over: the shipped INI is then read directly,
  as before, and keeps following the package. With a file made here, a later change of the shipped
  INI does not reach the player until Republic Mod Manager has saved once: RMM 0.6.7 takes the
  added sections over as entries of the player and regenerates the file from the package INI.
- Package schema: `token` is `unique = 1` now, as the plugin has always required.
- Checked offline with a harness that includes the plugin source and builds fake Steam trees
  (scratchpad): two extra deposits with comment lines, the settings section and the four defaults
  not copied, the plugin's own reader finding six deposits in the created file, a second start
  leaving the file alone, type and token collisions, an existing list file, the original only in
  the Workshop folder, no original - 16 checks, all met. Not run in the game.

## 0.4.14 (2026-11-18)

- Brush pictures travel with the plugin. The engine asks for a brush button's picture as
  `editor/tool_<verb>_<name>.png`, and the loader serves such a path only from its own VFS folder;
  a DLL loaded from a Workshop item could not put a file there, so its buttons showed the donor's
  icon unless the player had the pictures already. New folder `deposits_plus\editor` beside the DLL
  (package: `plugins\deposits_plus\editor`, by hand: `<loader>\plugins\deposits_plus\editor`) with
  the eight pictures of the shipped deposits (sand, clay, gas, copper; paint and erase).
  `ProvideToolIcons`, called from `InstallEditorPatch` when at least one brush is declared, copies
  for every brush the picture that is missing in `<vfs root>\media_soviet\editor`
  (`host->vfsRoot`, the same root `BlankMapPath` writes to). An existing file is never replaced
  (`CopyFileA` with fail-if-exists); the copy loses a read-only attribute. Log:
  `editor   <n> brush picture(s) copied into ...`, or a WARN with the Windows error. Only names of
  letters, digits and `_` are copied. Nothing changes for a brush without a picture on either
  side: the button keeps the donor's icon.
- Three log lines named a game build that is long gone ("differ from build v1.1.1.7"); they say
  1.1.1.13 now, the build the expected bytes belong to.
- Comments: version tags removed, stale folder names corrected. No other code change.
- The DLL of 0.4.13 was run from the Workshop folder on 2026-11-18 (list read from the package
  INI). The picture copy of 0.4.14 has not been run in the game yet.

## 0.4.13 (2026-11-18)

- The deposit list is read from `<loader>\plugins\deposits_plus.ini` when that file exists and
  otherwise from `deposits_plus.ini` beside the DLL. Before, only the loader folder was tried: a
  DLL the loader takes straight from a Workshop item (TesmioLoader b0.3.7 finds it there itself)
  had no file to read until Republic Mod Manager had saved once, and came up with "no mod
  deposit types". The log names the file the list came from.
- The global keys are still read through the host table (`plugins\deposits_plus.ini`). Upstream
  b0.3.7 hands a Workshop plugin the INI beside its DLL for that name; the loader build
  b0.3.7-rmm.2 prefers the file in `<loader>\plugins` when it exists, so list and switches come
  from the same file again. On an unpatched loader the switches of a Workshop DLL come from the
  package INI even when a finished INI lies in the loader folder.
- Not tested in the game yet with the DLL in a Workshop folder.

## 0.4.12 (2026-11-18)

- New key `[deposits_plus] legacy_adopt_min_cells` (default 500, 0..1048576, 0 = adopt anything).
  When a save without metadata is first loaded, sand is taken over from the old terrain channel
  (area-averaged to the 1024 x 1024 resource map). If fewer cells than this are non-zero there,
  the channel now counts as empty and the first distribution applies. Before, any non-zero cell
  recorded sand as "existing": a save made with the original deposits plugin that held 102 stray
  mask texels got no sand and, once saved, never would (seen in the game on 2026-11-16).
- The log names the number of adopted cells, or says that the channel was treated as empty.
- A save that was already stored with sand recorded as adopted is not revisited.

## 0.4.11 (2026-11-16)

Ported to WRSR 1.1.1.13. Every site was found again with `my_plugins\tests\portmap.pl` from its
1.1.1.9 bytes; 0 changed instructions, only rel32 / RIP-relative operands differ.

- Unchanged: parser site 0x10EAB8 / 0x10EAE8 / 0x118805, sampler 0x8360, world save 0x7C20, tool
  lookup 0x3AAA0, terrain overlay 0xAEE0, every `.data` address and both mask import slots.
- -0x80: ResourceGet 0x2AA7B0, the whole mine scan (dispatch 0x1DD763, mask and map brackets
  0x1DD448 .. 0x1DDE35, radius 0x1DCABD), vehicle tick 0x1B161A, the editor (panel 0x233100,
  rocks 0x22EE20, dispatch 0x30D120, cursor 0x2F0E90, texels 0x238AF0, paint 0x2350C0 /
  0x2352F0).
- -0x70: minimap 0x4BDE10 / 0x4BFED0, clip helper 0x446180, world-save call 0x42CD3E.
- -0xC0: editor button 0x3826A0, tooltip 0x383BB0, vehicle eligibility 0x3E3A39. +0x20: mine
  work sum 0x176E18.
  +0xA0: quarry arrival 0x66E6C8. +0x1A0: strcmp thunk 0x84F6C0.
- Float pool: +0x10 for most constants, +0x8 for 0.5 / 0.85 / the badge offset / the icon scale,
  +0xC for the panel size; each taken from the instruction that loads it.
- Signatures with a displacement re-read: map open (`.. 9C 6D 7B 00`), map close (`.. CB 63 7B 00`).
- Fixed: `kRadiusSources` and the default radius still held the addresses of the build before
  1.1.1.9, so on 1.1.1.9 a mod deposit with `radius = oil` read 140 instead of 130 (the float 0x18
  further on). The table now names the operands of the radius function: 130, 210, 50, 30, 250,
  170, 150.
- The brake recognises the original `deposits.dll` wherever it was loaded from (module handle plus
  its `TsmPluginInit` export), not only as a file in `plugins\`.
- Sand surface: the expected C3DDLL64.dll stamp is 0x6A8B496D; `CreateShaders`+0x2BA and the
  C3D_TERRAIN constructor carry the same bytes as before. The pixel-shader hashes of
  `default_terrain.inix` were not compared with the new game files.
## 0.4.10 (2026-09-20)

- Audit round, no new feature; addresses, record layouts and the save format are unchanged.
- `PrepareExtraMaps()` runs before `PatchDepositType`; `InstallExtraMaps()` only when the type patch
  is in place (`depositTypesReady`), otherwise `g_extraCount = 0` - a refused type patch no longer
  leaves the texture hook armed for map slots the game never learned about.
- `h_ED_TerrainDraw`: `__try/__finally` restores the overlay state, an outer `__except(FaultFilter)`
  drops the editor patch; `h_ED_PaintTexels` restores the slot in `__finally`; `PaintDeposit`
  restores `g_brushDep` and the bauxite byte in `__finally`.
- Reads and writes of `g_extra` run under `g_lock`.
- The blank DDS writer checks the written byte count, logs GetLastError and deletes the partial file;
  `GenerationAtomicWrite` logs GetLastError before DeleteFileA.
- `ValidateDeposits`: `building_type` must be 7 or 92, any other value rejects the deposit;
  `minimap` must be 0 or 1 (WARN and treated as 1 otherwise).
- `deposit_visual_runtime.inl`: DOS and NT headers are checked with ReadablePtr, `e_magic` and
  `Signature` before any field is read.
- RMM schema (package): `maximum_items = 32` (MAX_DEPOSITS) and `[list:tiles] maximum_items = 16`
  (MAX_SAND_TILES) instead of 118 and unlimited; the natural generation card moved to the General tab.

## 0.4.9 (2026-09-18)

- The Soviet Mod Loader guard no longer relies on the module handle alone. TesmioLoader inits the
  DLLs in `plugins\` in name order, and `deposits_plus` sorts before `soviet_mod_loader`: a local
  copy of this plugin would init while SML is not in the process yet, hook, and make SML's
  embedded deposits refuse its own sites. The guard now also fires when
  `plugins\soviet_mod_loader.dll` exists and its key in `tesmioloader.ini` is not 0. Found on
  Resources Plus 0.1.0, which lives in `plugins\` and blocked SML's start exactly this way; fixed
  here before it can happen with "Files local only".

## 0.4.8 (2026-09-14)

- Steps aside under Soviet Mod Loader. The existing guard looks for `plugins\deposits.dll`, but SML
  carries resources, deposits, needs and buildings compiled into itself, so that file does not exist
  and the guard never fired. A second check in `TsmPluginInit` now asks whether
  `soviet_mod_loader.dll` (or `000_soviet_mod_loader.dll`) is loaded in this process and whether its
  `[loader] embedded_plugins` is on; if so the plugin logs one line and returns 1 before anything is
  hooked. The module handle also answers the loader switch: with `soviet_mod_loader = 0` the DLL is
  not in the process and this plugin runs normally. `embedded_plugins` is read as well so a future
  per-capability switch in SML lets the plugin work again without another change here.
- Why it matters even though the patches defend themselves: the deposit type splice, the world save
  call, the minimap hooks and the editor hooks all compare their expected original bytes and refuse
  once SML has patched the same sites. The texture import in `InstallExtraMaps` does not - an IAT
  entry is a pointer with nothing to verify - so it would install and chain in front of SML's own
  texture hook, for maps this plugin believes it owns and can never save (its own log says "extra
  maps load but are never written back"). The guard has to sit before that, and it does.

## 0.4.7 (2026-09-12)

- The editor brush name is no longer capped at seven characters (four with `map = terrain`). The
  name in the tool descriptor is only an internal registry key: the button's picture is bound from
  a path this plugin passes itself, the hover text comes from `TOOL_CAPTION`, and the paint hook
  matches the active tool by pointer, never by name. So the descriptor now gets a short generated
  key `<verb>_t<type>` - ten characters at most, which fits `paint_rock` as well as
  `paint_bauxite` - while the icon path keeps the configured name
  (`editor/tool_<verb>_<editor>.png`). The configured name may be up to 31 characters of letters,
  digits, `_` and `-`; the two PNGs a deposit ships are named exactly as before.
- Found because a deposit called `rocksalt` silently lost its brush: the old code refused the
  14-character `paint_rocksalt`, logged `editor FAILED tool name: ...` and dropped the pair, which
  left the deposit working and the brush missing. The minimap button was never affected - it takes
  its picture from the resource record named by `icon`, where no length is involved.

## 0.4.6 (2026-09-10)

- Desert map test by the user: sand at 100% everywhere left no room for copper, clay and gas, because the
  filled sand record counted as an occupied deposit. A `desert_fill` deposit's record is now skipped in
  the occupancy mask on desert maps; the ores respect only the vanilla maps and each other.
- Organic fill: `GenerationHeights` samples the terrain height at every cell centre; the richness is a
  two-octave value-noise band `desert_fill_min..desert_fill_max` (percent, default 60..100) and, with
  `desert_fill_relief = 1` (default), falls linearly with height from the 2nd percentile of the land
  heights to 0 at the 98th, with a little noise on the slope. Seeded from the world seed and the
  deposit name. `desert_fill_relief = 0` keeps the band over the whole land. Log line names the band
  and the two heights.
- Package schema: three new fields under "Desert maps: everywhere"; READMEs updated.

## 0.4.5 (2026-09-10)

- Minimap: hovering a mod deposit button previews its overlay layer like the vanilla five do; the
  hovered layer wins over the selected one while the mouse rests on the button (`h_MM_DrawOverlay`
  picks state 1 before state 2; the row hook already tracked state 1). A vanilla layer that is on
  while a mod button is hovered stays underneath because the vanilla overlay has already been drawn.

## 0.4.4 (2026-09-10)

- Root cause of the Siberia/Asia failure, confirmed by the 0.4.3 line on terrain_siberia2: the live terrain
  mask's B channel (the gravel/rock texture layer) reserved 305236 cells, the resource maps 168000; the
  union of 417754 grew to 724009 with the 40 m gap because the layer is speckled over the mountains.
  Copper took the last three fitting spots, sand/clay/gas got nothing.
- New `[deposits_plus] generation_block_gravel` (0/1, default 0): only when set does the mask layer count
  as an occupied deposit. Vanilla places iron and coal in the mountains itself, so overlapping the gravel
  zone is harmless; the resource maps (coal, iron, oil, bauxite, uranium) stay respected either way.
  Logged in the `generation enabled=` line (`block_gravel=`) and in the occupancy line.
- Package schema: switch "Gravel zone blocks" on the Natural generation card; READMEs updated.

## 0.4.3 (2026-09-10)

- Diagnostic only: `generation occupancy:` log line before the gap dilation with the reserved cells per
  source (resourcemap R/G/B, resourcemap2 R/G, mask B, tombstones), the live mask texture's size and
  non-zero count per channel, and the union. Reason: on the Siberia and Asia DLC maps the placement
  reserved 724009 / 624610 cells although the resource maps on disk cover about 157000 / 115000 cells
  (offline replay incl. a 3-cell gap: 198504 / 147392); sand, clay and gas then found no room while
  copper still fit. The runtime mask texture is the suspect; the line will tell.

## 0.4.2 (2026-09-10)

- Tile files may live in set folders under `deposits_plus\assets`: `color` / `normal` are paths relative to
  the assets folder (`Siberia/sand_meadow_siberia_color.dds`); `VsAssetPathOk` refuses `..` elements, drive
  letters and absolute paths, everything else goes straight into the existing `VsReadFile` join. The
  `SandTile` fields for both names grew to 128 characters.
- Shipped assets are sorted into sets: `Vanilla` (meadow summer/autumn), `Siberia` (summer, snow-dusted
  autumn), `Asia - Jungle` (summer) and `Ultimate Vanilla +` (meadow pair matching that texture pack,
  not referenced by default). All colour files DXT1, all normal maps DXT5 with height in alpha,
  1024x1024, 11 mips. The shipped `[sand_tile:]` table points at the set files; Siberia and jungle no
  longer reuse the meadow pair.
- Package schema: the two file fields are grouped file lists with a pre-save check (Republic Mod
  Manager 0.4.48: `picker = files`, `reference = files`, `reference_format = dds_dxt1|dds_dxt5`).

## 0.4.1 (2026-09-10)

- Sand surface tile table: `[sand_tile:<id>]` sections (`base` = terrain base texture on material slot 5
  with its folder, `color` / `normal` = DDS files in `deposits_plus\assets`) replace the two hard-coded
  meadow names. `VsTile` matches the engine's texture path from the end as a whole path element, so
  `dlc2/tiles_siberia/grass2.dds` and `tiles_normal/grass2.dds` are different entries; a base without an
  entry, or an entry whose files fail to load, stays native. `VsDds` accepts any square power-of-two side
  from 256 to 4096 with a complete mip chain (file limit 48 MB). An INI without `[sand_tile:]` sections
  falls back to the classic meadow summer/autumn pair. The shipped INI maps meadow, Siberia (summer and
  the snow-dusted autumn `grass2snow.dds`) and jungle; Siberia/jungle reuse the meadow files until the
  user supplies their own. Winter and desert bases have no entry.
- `desert_fill = 1` per deposit: when the world's `script.ini` (shipped with saves too) says
  `$TYPE_DESERT`, the first distribution of that deposit fills every non-water cell of its 1024x1024
  map at full richness (`GenerationRun`, before the random placement branch; record status 1). Only a
  deposit that never held data is filled; saved data is never touched. Infinite unless Depletion mines
  it down. Shipped on for `[sand]`.
- Parser: `[sand_tile:]` sections are not deposits; unknown tile keys are logged and ignored.

## 0.4.0 (2026-09-07)

First published version.

- Generation presets: `generation_frequency` (1..6 regions) and `generation_size` (1..3 radius
  classes) are the only way to size the generation; an absent preset keeps the defaults (3 regions,
  class 2, richness 0.45..1.00).
- Assets are looked up beside the DLL first (`<DLL folder>\deposits_plus\assets`), then under
  `plugins\deposits_plus\assets`; the folder found is logged (`sand surface assets from ...`).
- `TsmPluginInit` returns 1 ("deposits_plus idle") while the upstream `plugins\deposits.dll`
  exists and is enabled in tesmioloader.ini, so both never run together.
- Workshop package `My Plugins\deposits_plus` with `local_copy = 1`, `[assets] dir = hooks\deposits_plus`
  and an RMM keyed_sections schema (tabs General / Sand structure / Deposits); texts (schema, de/en)
  reworked by the user, headings "Natural generation" and "Working vehicles" in the detail panel.
- No change to hooks, savegame format or `tesmio_deposits.bin` against the original plugin.

## Technical notes: sandy meadow surface

Optional ground visuals over the independent sand deposits: the meadow texture with irregular
sand traces is blended in at sand deposits; the resource density drives the transition.
Settings `sand_surface`, `sand_surface_strength` (0.0–1.0), `sand_surface_token` in
`[deposits_plus]`; the token must be a deposit with `independent_map = 1`. Summer and autumn use
separate colour/normal textures, also during the seasonal blend; the native order is
summer → autumn → snow; `grass2_snow.dds` and the native snow calculation are not replaced.

Technique: the extra texture is injected at load time into 13 identified native terrain pixel
shaders inside the actual virtual `CreateShaders` call; their existing sequence for terrain
blending, light, fog and snow is kept, the game loader's normal shader class linkage is passed
through unchanged. Sampling uses the native UVs, samplers and derivatives; only RGB is blended,
the original alpha stays. No shader file is changed. The selection mask is the current GPU
resource map; its identity is re-checked on a world change; no GPU readback or full resource copy
per frame. Unknown engine/shader builds, missing assets, unsuitable resource formats and binding
slots taken by other mods disable only this visual with a concrete warning; other Deposits Plus
features stay independent. Assets: DDS 1024 × 1024 with eleven mip levels, colour BC1/DXT1, normal
BC3/DXT5; `build.bat` copies them into `plugins\deposits_plus\assets`.

Offline verification: all 13 original shaders with D3D11/WARP, rendered mask/light/snow
fixtures, DDS loading, season flags, world-map change, temporary shader hooks and restoration
of the graphics bindings; both seasonal colour/normal pairs with 69,120 GPU pixel comparisons;
material accesses of the installed engine. In game confirmed: summer look, shrinking with
Depletion, save/load. Expected log lines:
`sand surface shader preparation: 13/13 verified native programs augmented`,
`sand surface active: token=$TYPE_MINE_SAND ...`,
`sand surface terrain selection: season=2 transition=0 surface=autumn/native ...`.
`prepared` alone means the hooks are in place, not that anything was rendered yet.

Third-party code: `third_party/d3d12TokenizedProgramFormat.hpp` and `third_party/DxilHash.cpp`
are unchanged from Microsoft's DirectXShaderCompiler repository (University of Illinois Open
Source License, `third_party/LICENSE.TXT`, shipped as `THIRD-PARTY-LICENSE.txt`). The hash
helper computes the DXBC format checksum only; it is not a security signature check.

## Technical notes: natural generation inside the country border

- Every loaded world: all resources of the current INI are matched by token against the
  savegame's history. Unknown empty resources and empty resources that were only skipped so far
  are generated once. Existing or already initialised deposits stay, also after full depletion.
  No built-in resource lists, savegame names or world ids.
- Natural, elongated, winding fields with varying width, occasional branches, irregular edges,
  real gaps and fading richness; a field may consist of several separate patches but counts
  once. Isolated grid points are removed.
- No placement in water: terrain heights inside every resource cell are checked against water
  level, wave amplitude and a safety margin, plus the shore distance (`generation_shore_m`,
  `generation_water_clearance_m`).
- Only inside the country border: a verified snapshot of the loaded native `BORDER_POLYGON`;
  without a polygon the native rectangular build limits apply. Border cells are excluded
  conservatively. The border structure is verified against the installed engine; an unknown or
  invalid structure suppresses new placements instead of placing outside the country.
- No overlap with other newly generated fields, existing plugin deposits or Vanilla
  oil/iron/coal/uranium/bauxite (`generation_gap_m`); the gravel/rock terrain zone only with
  `generation_block_gravel = 1`. Existing overlaps are not changed.
- Round robin over all new resources: at most one field and 32 attempts per turn, then the next
  resource; the order rotates with the saved seed. At most 256 attempts per requested field.
- Water, border and occupied pieces are cut away; a field counts only when at least 60 % of its
  original area remains after clipping and removal of tiny remnants (size 3 is not a remnant).
  Otherwise another place is tried.
- The log names target/result, clipped fields, attempts and the separate rejection reasons:
  country border, water/shore, deposits/gap, map edge, too little remaining area.
- First generation runs once at the first terrain draw after loading; no continuous generator.
- Levels: `generation_frequency` 1–6 (desired fields 1–6) and `generation_size` 1–3 (base
  measure 150–350 / 350–550 / 550–750 m). The base measure is chosen per field and scales its
  whole course; the elongated course spans about 3.4 to 4.6 times the base measure along its
  main axis. Invalid levels disable only that resource's generation with a warning.
- Sand: the shipped INI keeps `map = terrain` / `component = 1` as the origin note and adds
  `independent_map = 1`; the plugin assigns sand its own separate channel after the existing
  auto channels. On the first adoption of an old savegame without metadata the terrain richness
  is adopted (area-averaged from terrain to resource resolution) unless a sand deposit already
  exists; if that is empty too, the general first distribution applies. The visible terrain
  mask is neither recoloured nor reduced by sand mining; the sand brush lives in the editor's
  resource tab. Switching back to a terrain mask is not a supported migration.

Metadata `tesmio_deposits.bin`: versioned, checksummed identities, channel mapping, seed and
history of removed resources; written after the native map save from its DDS files and
replaced atomically through a temporary file; both are needed on load. History states:
adopted, generated, not yet initialised, generation attempted but no/too little room. Once data
is adopted or saved in a channel it is marked initialised, and later mining does not reset that.
Limit with old data: without metadata the plugin cannot prove that an empty channel was never
mined; `generate_existing_empty = 0` then protects old or incompletely initialised empty
entries. That option is never a regeneration command. Worlds from `media_soviet/save/...` and
`saved_last` count as old savegames without metadata, terrain templates as new worlds.
Unreadable/invalid metadata, missing original map files, contradicting types, unknown DDS
formats or missing engine functions produce warnings and suppress new generation and metadata
overwrites; a broken file is never interpreted as a new empty world.

Offline verification: all 18 level combinations, fair round-robin distribution, INI-order
independence, clipping/minimum area, concave country borders and border cells, layout
signature of the installed engine, water/occupancy masks, no overlap, bounded search, metadata
checksums, re-adding, re-sorting, depletion, remove/re-adopt, sand adoption and unchanged
native vehicle gates; plus a test with the real border, height and Vanilla occupancy data of
the coastal map template with several seeds; offline tools `rmhist` (DDS channel histogram) and
`occ` (occupancy replay). Log lines start with `generation`.

## Technical notes: working vehicles for own deposits

Per deposit section: `working_vehicle_skill = gravelmining` (or `none`, case-insensitive;
unknown values warn and disable only the vehicle extension of that section). The building
stays `$TYPE_MINE_SAND` (building_type 7) and still needs `$WORKING_VEHICLES_NEEDED`,
`$VEHICLE_PARKING` places and the normal operating conditions; vehicles use their existing
`$SKILL_GRAVELMINING`, there is no `$SKILL_SANDMINING`. The deposit map and the `$PRODUCTION`
resource are not changed. Fuel, state, capacity and production rules of the game stay.

Technique: four native type checks are extended by the explicitly configured mine types:
vehicle admission for purchase/assignment, two work-performance calculation paths and the
arrival/work state of pit vehicles. No building or vehicle types are rewritten in memory; the
original branches for gravel and bauxite stay first; an additional check limits new assignments
to building type 7. Installed at plugin start only; all four sites including surrounding
identification bytes are verified against 1.1.1.9 first. Deviating bytes or a preparation
failure leave all four checks untouched; without an active assignment no vehicle patch is
installed; `code_patch = 0` disables this extension too; other plugins rewriting the same sites
cause a warning and rejection. No new save format, no periodic vehicle scans, no change to the
executable on disk. Log prefix `vehicles`; successful start: `4 native gates installed` plus
one assignment line per released mine.

Offline verification against the installed executable: DLL built with the central build.bat
switches; 24,960 real x64 test calls of the original/extension branches (registers and defined
arithmetic flags, Vanilla behaviour, unconfigured types, building-type separation, limits and
up to 32 deposits); INI parser, defaults, invalid values, existing extra keys; four sites and
preconditions checked directly against executable bytes; deviating bytes and memory/protection/
cache failures before activation tested, never partially installed vehicle branches; a
successful installation changes only the four intended areas of a private in-memory copy.
