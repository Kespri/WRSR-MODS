# Resources Button Fix – build notes

Target: WRSR 1.2.0.9 (PE stamp 0x6B05A574), TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W3 /EHsc /LD`,
kernel32.lib only. Workshop package: `My Plugins\resources_button_fix` (local_copy). History newest first.

## 0.4.6 (2026-11-26)

- Default of `maximum_blocks` raised from 2 to 4 (built-in default and shipped INI). Capacity was
  12 pairs; the base game brings 5, Deposits Plus 4 and four content packages one each, so the
  thirteenth pair ("Resources capacity exceeded at pair 13") kept its original position far right of
  the window. `blocks` is still the number the tools need, capped by the setting, so nothing changes
  for a game with twelve pairs or fewer. No code change beyond the default.

## 0.4.5 (2026-11-25)

- Ported to WRSR 1.2.0.9 (PE stamp 0x6B05A574, image 0x00AE9000). Button drawer 0x3A12F0 ->
  0x3A15A0, Resources panel 0x24E390..0x24EA97 -> 0x24E240..0x24E947, Rocks panel
  0x24A050..0x24B782 -> 0x249F00..0x24B632. Repointed instructions: cancel X 0x24E6F3, cancel Y
  0x24E710, the brush-gap reload 0x24E892, Rocks brush Y 0x24A1DD.
- The float pool moved by +0xB8: 50 at 0x952014, 85 at 0x952104, 250 at 0x9523A8, 80 at 0x9520EC,
  0.85 at 0x951474; the remaining constants follow the same distance. DPI stays at 0x9DC08C.

## 0.4.4 (2026-11-25)

- Ported to WRSR 1.2.0.8 (PE stamp 0x6B0097B2, image 0x00AE9000). Button drawer 0x3826A0 ->
  0x3A12F0; its prologue saves RBX at `[rax+0x18]` now, the nine arguments are the same (call
  site compared in both builds). Resources panel 0x233100..0x233807 -> 0x24E390..0x24EA97, Rocks
  panel 0x22EE20..0x2304F7 -> 0x24A050..0x24B782 (its epilogue changed, the end is the byte behind
  its RET). Repointed instructions: cancel X 0x24E843, cancel Y 0x24E860, the brush-gap reload
  0x24E9E2, Rocks brush Y 0x24A32D.
- Layout constants by value: 50 at 0x951F5C, 85 at 0x95204C, 250 at 0x9522F0, 80 at 0x952034,
  0.85 at 0x9513BC, 90 at 0x952064, 105 at 0x9520BC, 42 at 0x951F30, 170 at 0x9521BC, 60 at
  0x951FAC, 75 at 0x952014; DPI at 0x9DC08C.
- `bytearrays.pl` matches the prologue and the four instruction heads. Not run in the game yet.

## 0.4.3 (2026-11-18)

- The compiled defaults (`kDefaultResources`, `kDefaultRocks`) equal the shipped
  `resources_button_fix.ini` for every key. They apply to a key that is missing from the INI; a
  missing file or a missing section still stops the plugin at start. Three of the 26 keys differed,
  so a shortened INI gave another layout than the shipped one:

  | Key | compiled before | shipped INI = compiled now |
  |---|---|---|
  | `[resources_window] columns_per_block` | 9 | 6 |
  | `[resources_window] button_scale` | 0.50 (fixed) | auto |
  | `[rocks_window] controls_gap` | 15.00 | 20.00 |

  `button_scale = auto` is `ScaleSetting { automatic = 1, value }`; the value is not read while
  `automatic` is set. Change the INI and the two initialisers together.
- Log and comment texts tidied. The configuration summary at start begins with
  `configuration loaded:` (the live reload keeps `configuration reloaded:`).
- No other change; with a complete INI the layout is the same as before.

## 0.4.2 (2026-11-16)

- The appended buttons are recognised from `deposits_plus.dll` as well. Until now only the module
  `deposits.dll` was looked up, so with the fork loaded the plugin logged "only vanilla buttons are
  present", left its pairs where the fork drew them and they ran out of the Resources window
  (seen in the game on 1.1.1.13: three pairs beside the panel). The original is tried first; the
  fork steps aside while the original is loaded, so at most one of the two is in the process.
- The log line names the module that was found.

## 0.4.1 (2026-11-16)

- Ported to WRSR 1.1.1.13; 1.1.1.9 is refused by the PE stamp. All twenty addresses moved
  except the DPI global (`.data`, 0x992088):

  | What | 1.1.1.9 | 1.1.1.13 |
  |---|---|---|
  | editor button drawer | 0x382760 | 0x3826A0 |
  | Rocks panel (function range) | 0x22EEA0-0x230577 | 0x22EE20-0x2304F7 |
  | Resources panel (function range) | 0x233180-0x233887 | 0x233100-0x233807 |
  | cancel X / Y operands | 0x233633 / 0x233650 | 0x2335B3 / 0x2335D0 |
  | Resources brush-gap reload | 0x2337D2 | 0x233752 |
  | Rocks brush Y operand | 0x22F171 | 0x22F0F1 |
  | float pool: 50, 85, 250, 80, 90, 105, 42, 170, 60, 75 | 0x90AA28 ... | each +0x10 |
  | float pool: 0.85 | 0x909ED4 | 0x909EDC (+0x8) |

- How it was checked (`my_plugins\tests\pe.pl`): the drawer was found by its first 50 bytes
  (one match) and its first 256 bytes compared with 1.1.1.9 (11 differ in 6 runs, all operands);
  both panels moved by -0x80 as a whole - the bytes around each patch site and across each
  function end are those of 1.1.1.9 (the two ends byte-identical), and both starts are where
  MaxLegend's deposits 1.1 hooks them; the four patched operands resolve to 42, 170, 75 and the
  DPI global (`pe.pl rip`); every pool constant was read at its new address (`pe.pl float`).
- New: `SupportedGameBuild` also requires each of the eleven pool addresses to hold its value
  (`layout-constant`). The values used to be trusted on the strength of the PE stamp alone.

## 0.4.0 (2026-09-09)

- 2026-09-11, version unchanged: the detail log `tesmioloader.resources_button_fix.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before).
First published version.

- Compact paint/erase button grids in the terrain editor's Resources and Rocks/Gravel windows, so
  many plugin resources fit; grid geometry, growing window and the position of the round red
  clear-all button are configurable per window; changed values are picked up live when the window
  is reopened, only the plugin switch needs a full restart.
- Configuration: `plugins\resources_button_fix.ini` when present, otherwise the INI beside the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge); the chosen file is logged at
  start and the live reload follows it.
- Package: Republic Mod Manager texts (57 keys de/en plus the English schema fallbacks) in player
  style; `[launcher] notice` (restart only for the plugin switch, live reload when the window is
  reopened); card "Troubleshooting" with the detailed log; ON/OFF wording. READMEs on the common
  template.
