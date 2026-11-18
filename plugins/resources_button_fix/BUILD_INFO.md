# Resources Button Fix – build notes

Target: WRSR 1.1.1.13 (PE stamp 0x6A8D4087), TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W3 /EHsc /LD`,
kernel32.lib only. Workshop package: `My Plugins\resources_button_fix` (local_copy). History newest first.

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
