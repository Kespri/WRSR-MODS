# Workers & Resources: Soviet Republic – TesmioLoader plugins

**English** | [Deutsch](README_DE.md)

## 🤖 Attention, comrade: an AI helped build this

These plugins were written with the help of an artificial intelligence. The five-year plans
behind them were still drawn up, tested and sworn at by a human every time the game crashed.
If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

## About

Native plugins for *Workers & Resources: Soviet Republic* 1.2.0.9, loaded by the
[TesmioLoader](https://github.com/MaxLegend/TesmioLoader) (API 4). The loader finds plugin DLLs
by itself, in its own `plugins` folder and in subscribed Steam Workshop items; the Republic Mod
Manager lists what the loader finds, edits the settings and checks the start. Every plugin is
packaged for the Steam Workshop with a settings schema for the Republic Mod Manager. This
repository holds the source code, as the GNU GPL v3 requires; there are no ready-made DLLs,
textures or models here - those come with the Workshop packages.

## Plugins

| Plugin | Version | What it does |
|---|---|---|
| [Deposits Plus](plugins/deposits_plus) | 0.4.19 | New deposit types, natural generation, sandy meadow, working vehicles |
| [Localization](plugins/localization) | 0.4.2 | Translation service: text packs become extended language files |
| [Research Expansion](plugins/research_expansion) | 0.4.4 | New research and changes to the research tree |
| [Resources Button Fix](plugins/resources_button_fix) | 0.4.6 | Compact tool grids in the terrain editor |
| [Technical Service Storage](plugins/technical_service_storage) | 0.3.7 | Grit storages, material priorities and snowplow tanks |
| [UI Layout Fixes](plugins/ui_layout_fixes) | 0.3.3 | Row spacing in the customs house, text wrapping in info windows |
| [Vanilla Buildings](plugins/vanilla_buildings) | 0.4.7 | Temporary changes to building files without touching the originals |
| [Vehicle Materials](plugins/vehicle_materials) | 0.4.3 | Extra materials for vehicle production |
| [Weather Roads](plugins/weather_roads) | 0.3.7 | Road snow, melting and protection after plowing |

These three travel with the Republic Mod Manager package:

| Plugin | Version | What it does |
|---|---|---|
| [Buildings Plus](plugins/buildings_plus) | 0.2.3 | New buildings from a declaration: donor building plus commands that append, replace, remove or insert lines, generated at game start |
| [Resources Plus](plugins/resources_plus) | 0.1.8 | Own resources from `plugins\resources_plus.ini`, fork of resources: fixes the crash on exit, takes over the list of the original once, names per game language, pauses while the original is loaded |
| [Needs Plus](plugins/needs_plus) | 0.1.5 | Own citizen needs from `plugins\needs_plus.ini`, fork of needs: takes over the list of the original once, duplicate names refused, pauses while the original is loaded |

Each plugin folder has its own guide in two languages (`README_DE.md`, `README_EN.md`)
and English build notes (`BUILD_INFO.md`). Shared files: `plugins/grit_spreader_api.h`
(service between Technical Service Storage and Weather Roads) and `plugins/tesmio_config.h`
(INI base and personal overlay). `src/` holds the four SDK headers `tesmio_api.h`,
`tesmio_plugin.h`, `tesmio_release.h` and `tesmio_discovery.h` from the TesmioLoader by MaxLegend (GPL v3), so the
plugins and the loader build from this repository with their relative include paths.
`tesmio_plugin.h` carries one local change: `TsmOpenLog` writes the plugin detail logs into
`tesmioloader\logs\`. `src/tesmioloader.cpp` is the loader itself in the corrected build
b0.4.5-rmm.2 that the Republic Mod Manager package ships and installs over the upstream DLL
(same API, same log lines; what changed is in `src/BUILD_INFO.md`).

## Tools

| Tool | Version | What it does |
|---|---|---|
| [Republic Mod Manager](tools/republic_mod_manager) | 0.7.7 | Windows program: lists the plugins the loader finds, edits their settings, switches them on and off and checks the start; its Workshop item brings the installer, the loader and the three plugins above |

## Installing

Subscribe to the Workshop package; the TesmioLoader finds the plugin by itself at the next game
start. Settings and switches are in the Republic Mod Manager. The plugin guides also describe
the way by hand: DLL and INI into `tesmioloader\plugins`.

## Building

Visual Studio 2022 or newer with the x64 C++ tools, then in a Developer Command Prompt
inside the plugin folder:

```
cl /nologo /O2 /MT /W3 /EHsc /std:c++17 /LD /Fo"build\\" /Fd"build\\" /Fe"build\<name>.dll" <name>.cpp /link kernel32.lib
```

The Republic Mod Manager builds with
`tools/republic_mod_manager/build.bat` (C# compiler of the .NET Framework 4). The exports
`TsmPluginApiVersion`, `TsmPluginInit` and `TsmPluginStart` are the loader's plugin contract
(API 4). Addresses and signatures are valid for game build 1.2.0.9 (PE stamp 0x6B05A574) only.

## Bug reports

Please open an issue with the game version, the loader version, the active plugins and the
logs `tesmioloader.log` from `tesmioloader` and `tesmioloader.<plugin>.log` from `tesmioloader\logs`.

## Credits and licence

GNU GPL v3, see [LICENSE](LICENSE). Deposits Plus, Buildings Plus, Resources Plus and Needs Plus
continue plugins from the TesmioLoader by MaxLegend (Tesmio), and `src/tesmioloader.cpp` is his
loader with corrections. `plugins/deposits_plus/third_party` contains Microsoft code under the
University of Illinois Open Source License (see `LICENSE.TXT` there).
