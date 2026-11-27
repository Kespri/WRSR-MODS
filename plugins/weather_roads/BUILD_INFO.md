# Weather Roads – build notes

TesmioLoader plugin (API 4) for WRSR 1.2.0.9 (PE stamp 0x6B05A574, engine 0x6B05A4A4), GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles `weather_roads.cpp`;
it includes `src/tesmio_plugin.h`, `my_plugins/grit_spreader_api.h` (consumer side of the
`tss.grit_spreader` service of Technical Service Storage) and the two persistence headers
`weather_roads_persistence.h` / `weather_roads_persistence_format.h` (sidecar
`tesmioloader.weather_roads.protection.bin` beside the savegame). Hooks are installed only on the
verified SOVIET64.exe / C3DDLL64.dll build. User documentation: README_DE.md / README_EN.md.
History newest first.

## 0.3.8 (2026-11-27)

- Rebuilt with `/DTSM_RELEASE_VERSION=0.4.6` for TesmioLoader b0.4.6 (launcher 0.4.6 warns about every plugin built for 0.4.5); code unchanged apart from the version string.

## 0.3.7 (2026-11-25)

Ported to WRSR 1.2.0.9 (SOVIET64.exe stamp 0x6B05A574, image 0xAE9000; C3DDLL64.dll stamp
0x6B05A4A4, image 0x1F3000). No behaviour change intended.

- Engine: the code of C3DDLL64.dll is byte-identical to the previous build, so the texture
  functions, the vtable (0x188C58) and the EditMask return address keep their values; only the
  expected stamp changed.
- Executable: weather call site 0x32BFAB -> 0x32C1EB (tick 0x352540 -> 0x3527F0), road snow clear
  body 0x6EFEF9 -> 0x6F0269, road snow adjust 0x44EF90 -> 0x44F240, mask update 0x4324F0 ->
  0x4327A0, the four snow writes 0x6EFF80.. -> 0x6F02F0.., save-close bridge 0x452EE2 -> 0x453192.
- The game object grew by 8 bytes: the road manager is at +0xF840 (was +0xF838), which is inside the
  compared prologue of the snow adjust function, and the flag tested at the save close is at
  +0x14DEE (was +0x14DE6). Both expected-byte arrays follow.

## 0.3.6 (2026-11-25)

Ported to WRSR 1.2.0.8 (SOVIET64.exe stamp 0x6B0097B2, image 0xAE9000; C3DDLL64.dll stamp
0x6B004BD7, image 0x1F3000); 1.1.1.13 is refused by the PE stamps. No behaviour change intended.
Not run in the game yet.

- Code moved by no single distance. Weather call site 0x30D958 -> 0x32BFAB (resume 0x32BFBA),
  weather tick 0x333B50 -> 0x352540. Road-snow clearing entry 0x6BC3B0 -> 0x6EFEF0: body
  0x6EFEF9, the four zero stores 0x6EFF80 / 0x6F0050 / 0x6F0120 / 0x6F01C0. Road-snow adjust
  0x42AEF0 -> 0x44EF90, its two +30 weather return addresses 0x5F8026 / 0x5F8116 (the game still
  has four callers: -255 at 0x355C55, -30 at 0x5F7DF8 and the two +30 ones). Global mask update
  0x40E720 -> 0x4324F0, EditMask flush return 0x40ED81 -> 0x432BFF (call at 0x432BF9, import
  `C3D_TERRAIN::EditMask`). Road geometry 0x56ADC0 -> 0x591030, save-close call 0x42EDC2 ->
  0x452EE2 (import `fclose`). Global game-world pointer 0x9941F0 -> 0x9DDC88.
- The world object grew, so its fields moved, each by its own distance: day 0x590 -> 0x5B8, year
  0x594 -> 0x5BC, time of day 0x59C -> 0x5C4, weather switch 0x5C4 -> 0x5EC, mask request bytes
  0x5E4 / 0x5E5 -> 0x60C / 0x60D, winter roll 0xE28 -> 0xE50 (its period timer 0xE54), weather
  index 0xE70 -> 0xEA0, night factor 0xE74 -> 0xEA4, road manager 0xF7A0 -> 0xF838; on the
  game-world object the terrain 0xED8 -> 0xF78. Road fields (+8/+0x10 points, +0x20/+0x28 end
  nodes, +0xE8/+0xF0 snow bytes, +0x120 type) and manager +0x2B0/+0x2B8 did not move.
- Expected bytes re-read where they carry a moved value: the weather call displacement
  (`E8 8B 65 02 00`), road-snow adjust (`mov r8,[rcx+0xF838]`), global mask update
  (`sub rsp,0xB0`, the frame grew), the save-close signature (`FF 15 78 01 46 00`,
  `cmp byte [rdi+0x14DE6],0`).
- The save function was rebuilt by the compiler: the save folder is `self+0xA20` (it was +0x9F8)
  and lives in R12, no longer in R15. It is loaded at 0x450CEF and again at 0x45196A, and no
  instruction writes R12 between there and the string call at 0x452F02 that still takes it after
  the close; the one jump that skips the second load (0x450EFD -> 0x451971) arrives with the first
  one intact. The folder signature is now `lea r12,[rdi+0xA20]` at 0x45196A and the bridge passes
  `mov rdx,r12`. The same register carries the folder into `"%s/windows.bin"` right before the
  final `fopen`, exactly where the old build used R15.
- C3DDLL64.dll: the texture functions are where they were (texel writer 0x19100 with the same
  15-byte prologue, access open 0x18D40, open2 0x18E80, close 0x18FC0). The texture vtable moved
  0x187BF0 -> 0x188C58 (slots 16, 17, 18 and 23 hold those four functions). `EditMask` is the
  export at 0xFC480 and ends before 0xFC870; its only `call [r10+0xB8]` is at 0xFC7B3, so the
  texel writer returns to 0xFC7BA (it was 0xFBC8A). Terrain fields (+0x158 mask texture, +0x87C /
  +0x880 size, +0x890 / +0x898 origin) were read back from `GetMaskPixelOverPoint`, texture fields
  (+0x14, +0x18, +0x130, +0x158, +0x160, +0x168, +0x170) from the access functions: unchanged.
- Method: `my_plugins\tests\portmap.pl` over the whole `.text` with the 1.1.1.9 bytes of every
  site (list `tests\portlists\wr.map`) plus a second list with the weather tick, the light factor,
  the winter roll and the caller of the mask update, so that every field offset is evidenced by
  the instruction that uses it; `my_plugins\tests\bytearrays.pl` then compared every
  expected-byte array with the files on disk at its site (fourteen comparisons, all match). The winter roll and the save function
  matched only weakly and were read by hand in the disassembly.
- Known difference in the game, not in the plugin: the winter roll now skips climates 1, 2 and 4
  (it skipped 1 and 2); the plugin only reads the rolled value.
- Open for the game test: weather sampling (the snapshot refuses a weather index above 3 or a
  roll above 7), gradual snow and the inline mask correction, the plow trail with and without
  the grit service, the sidecar save and load.

## 0.3.5 (2026-11-18)

Texts only. No behaviour change: no hook, address, INI key or default was touched.

- Two startup INFO lines carried a version tag from before the first published version. Both now
  say the same without it: the road-snow clearing observer line ends with "these sites are kept
  for diagnostics only", and the visible plow after-effect line reads "that trajectory is bound
  to one vehicle across the render/update thread boundary and only its still-open brush interval
  is finalised with Sand, Gravel or dry".
- The same tags are gone from six source comments (file head, `VisibleBrushCapture`, the 16 ms
  coalesced pass, the event-driven visible correction, the EditMask channel note,
  `ApplyTreatmentToPendingVisibleBrushes`).
- Version in the source, the INI head comment, both READMEs and the package manifest.

## 0.3.4 (2026-11-16)

Ported to WRSR 1.1.1.13; 1.1.1.9 is refused by the PE stamps. No behaviour change.

- Weather call site 0x30D9D8 -> 0x30D958 and weather tick 0x333BD0 -> 0x333B50 (-0x80).
- Road-snow clearing 0x6BC210 -> 0x6BC3B0 (+0x1A0): body 0x6BC3B9, the four zero stores
  0x6BC440 / 0x6BC510 / 0x6BC5E0 / 0x6BC680.
- Road-snow adjust 0x42AF60 -> 0x42AEF0, both weather return addresses 0x5D0797 / 0x5D0885,
  road geometry 0x56AE30 -> 0x56ADC0 and the save-close call 0x42EE32 -> 0x42EDC2 (-0x70).
- Global mask update 0x40E7E0 -> 0x40E720 and the EditMask flush return 0x40ED81 (-0xC0).
- The save-close signature carries a displacement and was re-read (`FF 15 50 F2 43 00`).
- C3DDLL64.dll was rebuilt (stamp 0x6A8B496D, same image size). The texel writer 0x19100, its
  caller 0xFBC83 and the texture vtable 0x187BF0 (slots 16, 17, 18 and 23) are where they were;
  only the expected engine stamp changed.
- Method: `my_plugins\tests\portmap.pl` with the 1.1.1.9 bytes of every site; 0 changed
  instructions, only rel32 / RIP-relative operands differ.
## 0.3.3 (2026-09-09)

- 2026-09-11, version unchanged: the detail log `tesmioloader.weather_roads.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before). The file was called `weather_roads.log` before; the new name follows the other plugins so the Republic Mod Manager log window lists it.
- Defaults `[snow] accumulation_multiplier` 0.30 -> 0.35 and `maximum_accumulation_per_burst`
  50 -> 95 (user decision after in-game tuning: too little snow stayed after short snowfalls).
  Changed in the compiled-in `g_cfg` initialiser, both INIs, the schema/DE/EN descriptions and
  the README tables. No code path changed. Users with their own values in `user_config` are not
  affected; the package defaults_hash changes, so RMM shows the update marker once.
- Fix: 0.3.1 inserted `releaseFollowsWeather` into `WeatherRoadsConfig` without adding an entry to
  the aggregate initialiser of `g_cfg`, so every compiled-in default after it was shifted by one
  (that was the source of the C4244/C4838 warnings at the initialiser, wrongly taken for
  pre-existing). No runtime effect as long as the INI carries every key, because the readers
  fall back to those values only for missing keys. Entry added, the build is warning-free again.

## 0.3.2 (2026-09-09)

- `+0xE28` of the world object is the winter weather roll, not a 0/1/2 precipitation state.
  Disassembly of the winter routine (exe+0x5D06B8..0x5D08FA): when the period timer at `+0xE2C`
  expires the game rolls `rand % 3` on climate 3 and `rand % 8` on every other climate, so the
  field holds 0..7. Only 1 is snowfall: the +30 road-snow ticks (`0x42AF60`, return RVA
  `0x5D08F5` in the limiter log) and the flags at `+0x5E4` run only for 1; 0 and 2..7 are all
  "no snow". Confirmed in the user's log (accumulation calls only during state 1, none during 2)
  and by a read-only memory probe of the paused game (roll 6 while the plugin reported
  "unavailable").
- `ReadWeatherSnapshot` accepts 0..7 (was 0..2). With the old range the snapshot was refused on
  5 of 8 rolls: 23 "weather tick unavailable" events in a 12-minute session, info window "no world
  data", and `release_follows_weather` could not act.
- `release_follows_weather` drops the queue on any roll other than 1 (was: only 0). Event line
  carries `precipitation_state=<n> (<name>)`.
- `WeatherRollName`: dry / snow / no snow / unknown for the weather EVENT lines and the info window;
  the window's old "rain" label for 2 was wrong.
- Package: INI comment, schema and DE/EN descriptions say "snowfall" instead of "precipitation"
  and "a few seconds" instead of "half a minute" (the burst maximum caps the queue at 80 units,
  4.4 s at step 2 / 110 ms).
- In-game test 2026-09-09 21:45 (user): 23 weather lines with named rolls 0..7 in two minutes, no
  "weather tick unavailable" line, one snowfall (roll 1, 11 ticks, 100 units queued) ended by
  "gradual snow release stopped with the weather: precipitation_state=4 (no snow), dropped_units=4"
  19 ms before the roll change was logged.

## 0.3.1 (2026-09-09)

- `[snow] release_follows_weather` (default 1, 0..1): `ServiceGradualRoadSnow` reads the weather
  snapshot before every release step; with `precipitation_state == 0` the remaining queued units
  are dropped (counted in `g_gradualSnowCancelledUnits`, one EVENT line), the next-step tick is
  cleared and the visual batch flushed. `ReadWeatherSnapshot` failing (no capture, stale > 30 s,
  implausible values) leaves the release untouched - fail open. Values seen in the user's log:
  precipitation_state 1 while it snows, 0 afterwards (plausible range 0..2).
- Reason: the queue (up to 255 units at 1 unit / 125 ms) kept whitening roads for up to ~30 s
  after the snowfall had visibly ended. The alternative of holding the weather object in the
  snowing state was rejected (feedback into the same queue, fighting the game's weather machine).
- Startup INFO line now ends with `release follows weather=on|off`.
- Package: schema field on the Snow tab (order 45), DE/EN texts, INI comment, READMEs.

## 0.3.0 (2026-09-08)

First published version.

- Scaled and optionally gradual road-snow build-up with a per-burst cap, independent natural
  melting scale, protection phase (game minutes) followed by a weaker salt phase (game hours) with
  a configurable factor, material strength and dry-plowing detection through the grit spreader
  service, visual snow correction on tracked plowed areas, F10 info window with built-in
  German/English texts, protection persistence beside the savegame (sidecar written after the
  final native file close), defensive loader checks and cleanup on exceptions.
- Configuration: `plugins\weather_roads.ini` when present, otherwise the INI next to the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge); own `ConfigString` reader
  identical to the loader's (GetPrivateProfileStringA plus trim). Chosen path logged as
  `configuration file:`.
- Package: five-tab RMM schema (33 fields); texts (schema, de/en) in player style: every boolean
  described with ON/OFF, line breaks in longer descriptions, the restart/Technical Service Storage
  note in `[launcher] notice` (needs Republic Mod Manager 0.4.3), the plow formula kept out of the
  UI, "protection phase after plowing" naming. READMEs on the common template.
