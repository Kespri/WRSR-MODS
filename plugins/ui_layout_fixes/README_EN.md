# 🪟 UI Layout Fixes 0.3.1

**TesmioLoader plugin for targeted corrections to single info windows**

Bundles window-specific corrections for *Workers & Resources: Soviet Republic* 1.1.1.13. Every window type is its own module with its own configuration, target checks and switch. Currently included: the **CUSTOMHOUSE** module (wider row spacing of the resource list in the customs house) and the **TEXT_WRAP** module (game captions you list by text id are wrapped and drawn line by line instead of running off their window; the route hint of the vehicle window is preset). There is deliberately no global row spacing.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Value ranges](#-value-ranges)
- [How it works](#-how-it-works)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.1.1.13
- TesmioLoader b0.3.7 or newer (API 4)
- No dependency on other plugins

### In three steps
1. **Subscribe:** the TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Set the row spacing** if you like: `resource_row_pitch` from 25.0 to 60.0 in `ui_layout_fixes.ini`, most conveniently through Republic Mod Manager (default 30.0).
3. **Restart the game completely** and open a customs house or a vehicle with a route problem. The detail log shows `[CUSTOMHOUSE] active` and `[TEXT_WRAP] active`.

---

## ✨ Features

### 🎯 Core function
- ✅ CUSTOMHOUSE module: wider row spacing of the resource list in buildings with `$TYPE_CUSTOMHOUSE`, i.e. the game's customs houses and compatible mod buildings that use the same info window
- ✅ Only the two verified calls of the resource list are changed: the measurement pass (list height) and the drawing pass (icons and texts); both use the same spacing
- ✅ The position of the window sections below and the scroll range are still computed by the game's native layout; every other window keeps the native spacing of 25.0 logical pixels
- ✅ Only verified code and data in the memory of the running game are changed; game files, building files and savegames stay untouched

### 🆕 New in 0.3.0
- ✅ Every Republic Mod Manager text in player style: notice boxes instead of group texts, General card removed, own help text in the add dialog, game captions picked through "Choose text...".
- ✅ **TEXT_WRAP module:** long captions such as "View area where a possible issue exists on route!" in the vehicle window are drawn as one line and run off the window. The module intercepts the game's text lookup for every text id in the `[text_wrap_ids]` list, returns a re-wrapped copy and draws it line by line through the engine's hooked print functions. Rules: at most `max_chars` characters per line (default 58, overridable per text), words are never split, the lines get similar lengths, the game's paragraphs flow together (`keep_breaks = 1` keeps them), line spacing `line_spacing` as a multiple of the font size (default 1.15). If a text needs more than `max_lines` lines (default 4), the lines get wider step by step. No game code is changed; the module does not depend on the game build and works in every game language that separates words with spaces.
- ✅ **Finding candidates:** `log_long_texts = 70` writes every text id the game shows whose longest line exceeds 70 characters once to the detail log, with its first 80 characters. In Republic Mod Manager you then add the id on the Text ids tab.
- ✅ Republic Mod Manager: list editor with the tabs General, Customs house, Text wrap and Text ids.
- ✅ **Shared configuration rule** (`tesmio_config.h`): the base is `tesmioloader\plugins\ui_layout_fixes.ini`, otherwise the INI beside the DLL (in the Workshop package). If `user_config\ui_layout_fixes.ini` exists in the loader folder, its keys win one by one. Both paths are logged at start.

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice.

---

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item
2. The TesmioLoader (b0.3.7 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. The DLL finds the INI next to itself in the package
4. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

---

### Method 2️⃣: By hand into the loader folder

```
1. Copy ui_layout_fixes.dll and ui_layout_fixes.ini from plugins\
   → <game>\tesmioloader\plugins\

2. Restart the game completely
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

The package ships a launcher schema in the `config` folder. Republic Mod Manager (version 0.6.3 and later) shows UI Layout Fixes in four tabs, German and English:

- **General:** notes and the button for this guide
- **Customs house:** module switch and row spacing of the resource list
- **Text wrap:** module switch, characters per line, line limit, line spacing, paragraph handling and the log helper for long texts
- **Text ids:** the list of wrapped texts with a width of their own per entry; the route hint 1970 is preset and can be hidden. The "Choose text..." button in the add dialog lists every caption of your game language with search and minimum length

The "Plugin active" switch in the header also sets `enabled = 1` when switched on. Republic Mod Manager writes the effective INI to `tesmioloader\plugins\ui_layout_fixes.ini`; the INI in the package stays untouched. Your own values and text ids are stored separately in `user_config\ui_layout_fixes.editor.ini` and merged in when you save. If you prefer editing the INI by hand, everything else is below.

---

## ⚙️ Configuration

### Main file: `ui_layout_fixes.ini`

The DLL reads in this order:
- **Base:** `tesmioloader\plugins\ui_layout_fixes.ini` if it exists, otherwise the INI beside the DLL (in the Workshop package `plugins\ui_layout_fixes.ini`); Republic Mod Manager writes the effective INI in the loader folder
- **Overlay:** `tesmioloader\user_config\ui_layout_fixes.ini`, key by key on top, if you create it by hand (a `[text_wrap_ids]` section there replaces the whole list)

⚠️ **Comments only on their own lines with `;`.** The file must be UTF-8 without BOM; do not repeat sections or keys. Unknown or misspelled names are ignored and the plugin silently uses the compiled default, so keep the documented names unchanged and check the log after changes. Settings are read only at start: change the INI, restart the game completely.

```ini
[general]
; 1 enables the individually configured modules, 0 starts no module and writes no patch
enabled = 1

[customhouse]
; 1 enables only the CUSTOMHOUSE module; no effect while [general] enabled = 0
enabled = 1
; row spacing of the resource list in logical pixels, 25.0 to 60.0, decimal point
; 25.0 = native game value, 30.0 = plugin default
resource_row_pitch = 30.0

[text_wrap]
; 1 enables only the TEXT_WRAP module; no effect while [general] enabled = 0
enabled = 1
; default width in characters for every listed text without a width of its own, 20 to 200
max_chars = 58
; line limit, 0 to 12; 0 = no limit; above it the lines get wider step by step
max_lines = 4
; line spacing as a multiple of the font size, 0.50 to 3.00, decimal point
line_spacing = 1.15
; 0 = the game's paragraphs flow together, 1 = paragraphs stay separate lines
keep_breaks = 0
; 0 = off; otherwise every shown text with a longer line lands once in the detail log
log_long_texts = 0

[text_wrap_ids]
; text id = characters per line; 0 = default from [text_wrap], otherwise 20 to 200; up to 64 entries
1970 = 0
```

`enabled` is read as an integer: `0` disables, any other value enables. Use only `0` or `1` anyway.

---

## 📏 Value ranges

| Key | Range | Default |
|---|---|---|
| `[general] enabled` | 0 or 1 | 1 |
| `[customhouse] enabled` | 0 or 1 | 1 |
| `resource_row_pitch` | 25.0 to 60.0 inclusive, decimal point | 30.0 |
| `[text_wrap] enabled` | 0 or 1 | 1 |
| `max_chars` | 20 to 200, whole number | 58 |
| `max_lines` | 0 to 12, whole number (0 = no limit) | 4 |
| `line_spacing` | 0.50 to 3.00, decimal point | 1.15 |
| `keep_breaks` | 0 or 1 | 0 |
| `log_long_texts` | 0 to 400, whole number (0 = off) | 0 |
| `[text_wrap_ids] <id>` | id 1 to 100000; value 0 or 20 to 200 | `1970 = 0` |

For `resource_row_pitch`: `25.0` equals the native game value and adds no spacing; non-numeric, infinite or out-of-range values are discarded, the plugin logs a warning (`invalid-config`) and uses `30.0`, the module stays active.

The same applies to the keys in `[text_wrap]`: invalid or out-of-range values trigger a warning (`invalid-config`) and fall back to the default. In `[text_wrap_ids]` a line with an invalid text id is skipped with a warning, an invalid width falls back to the default width with a warning; if the section is missing or empty, the built-in entry 1970 applies.

---

## 🔬 How it works

The game natively uses a spacing of `25.0f` for the resource list. The plugin redirects only the two verified CUSTOMHOUSE calls through a near memory bridge; during such a call the native spacing is temporarily replaced by the configured value and restored right after. This leaves untouched: resource lists of other windows, the native list-height computation, the positioning of the following window sections, the scroll range, goods quantities, prices, building properties, savegames and files.

Before writing a patch the module checks the size of the loaded image, the PE time stamp of `SOVIET64.exe`, the signature of the CUSTOMHOUSE panel function, the measurement and drawing calls with their targets, the native row-pitch instruction and the reachability of the memory bridge. If any step fails, the plugin writes no patch and aborts its start; TEXT_WRAP then does not run either, as long as `[customhouse] enabled = 1`.

The TEXT_WRAP module works at two points, both through the import table of `SOVIET64.exe`. First it hooks the engine DLL's text lookup `C3D_LANGUAGE::GetString`: every listed text id receives a re-wrapped copy from the plugin's own buffer, every other id passes through unchanged, and the copy is rebuilt only when the game returns a different text for the id (language switch). Second it redirects the engine's print functions (`PrintLeft/Center/RightUnicode` on `C3D_FONTMANAGER` and `C3D_FONT`, `PrintLeftUnicodeNoArg`) through small generated stubs: if the text pointer lies inside the plugin's buffers, the text is drawn line by line with y advanced by font size times `line_spacing` per line; every other call jumps on to the original with all its arguments intact. The print functions are variadic, which is why the stub only checks the pointer and never touches the stack. No game code is changed; the module does not depend on the game build, only the text ids may move with a game update. Other plugins hooking the same imports (for example the resources plugin on the text lookup) chain in load order; each one answers only its own texts.

Limit: if the game copies a text into a buffer of its own before printing, the pointer check no longer recognises it and it stays one line. Characters are not pixels; give the entry a width of its own for narrow windows.

| Property | Expected value |
|---|---|
| Game version | `SOVIET64.exe 1.1.1.13` |
| PE time stamp | `0x6A8D4087` |
| Size of the executable image | `0x00A9D000` |

---

## 💾 Compatibility

### Game and loader
- Exactly supported game version 1.1.1.13 for CUSTOMHOUSE; other versions are refused because memory addresses and signatures depend on the version. TEXT_WRAP changes no game code and depends only on the text ids
- Compatible with the resources plugin, which hooks the same text lookup; the hooks chain in load order
- Only languages that separate words with spaces are wrapped (Chinese and Japanese stay unchanged)
- No dependency on the Localization plugin or any other TesmioLoader plugin
- A complete game restart removes every active memory change

### Savegames
The plugin changes neither game files nor savegames.

### Version compatibility
- **0.3.1:** for WRSR 1.1.1.13; otherwise unchanged
- **0.3.0:** first published version with the modules CUSTOMHOUSE and TEXT_WRAP

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| No wider spacing | `[general]` or `[customhouse]` `enabled = 0`, or `resource_row_pitch = 25.0` | switch on, set a value above 25.0, restart the game |
| `invalid-config` in the log | a value in `[customhouse]`, `[text_wrap]` or a line in `[text_wrap_ids]` invalid | enter a value in the documented range (row spacing with a decimal point) |
| A listed text is still one line | `[text_wrap] enabled = 0`, id not in the list, or the game copies the text before printing | enable the module, check the id (`log_long_texts`); otherwise that text stays out of reach |
| Lines too far apart or overlapping | `line_spacing` does not match the font | change the value; the first wrapped print logs font size and step in the detail log |
| `print-import`, `print-hooks` | print imports could not be redirected | check `tesmioloader.log`; only this module stays inactive |
| `stub-allocation` | memory page for the stubs unavailable | restart the game, keep the log |
| `font-size` | font size export not found | line spacing uses 16 times `line_spacing` |
| `text-length` | game text longer than the buffer (1023 characters) | check the text id; the native text is shown unchanged |
| `all window modules are disabled` | plugin on but both modules `enabled = 0` | enable at least one module |
| `no text ids listed` | `[text_wrap_ids]` holds only invalid lines | add a valid id or switch the module off |
| `log-open` | detail log could not be created | check write permissions; `tesmioloader.log` stays available |

### Logging

All messages go to `tesmioloader.log` and to the detail log `logs\tesmioloader.ui_layout_fixes.log`, each with level, module and rule, plus the Windows error code where one applies.
- In **Republic Mod Manager** the log icon in the sidebar opens the log view with filter and sender.

Expected on a successful start:
```
[CUSTOMHOUSE] active
[TEXT_WRAP] active
```

---

## 📦 File structure

**Workshop package** (Steam subscription)
```
<Workshop item>\
├── plugins\
│   ├── ui_layout_fixes.dll         (plugin)
│   └── ui_layout_fixes.ini         (original INI)
├── config\                         (launcher schema for Republic Mod Manager)
│   ├── ui_layout_fixes.launcher.ini
│   └── languages\
│       ├── de.ini
│       └── en.ini
├── rmm.mod.ini                     (manifest for Republic Mod Manager)
├── workshopconfig.ini              (Steam Workshop entry)
├── previewimage.png
├── LICENSE                         (GNU GPL v3)
├── README_DE.md
└── README_EN.md
```

**Loader folder** (only with method 2 or with Republic Mod Manager)
```
<game>\tesmioloader\
├── logs\
│   └── tesmioloader.ui_layout_fixes.log
├── plugins\
│   ├── ui_layout_fixes.dll         (method 2 only)
│   └── ui_layout_fixes.ini         (effective INI)
└── user_config\
    └── ui_layout_fixes.editor.ini  (personal values from Republic Mod Manager)
```

Loaded from the Workshop, the DLL stays in the package; `tesmioloader\plugins` then holds only the effective INI, once you have saved in Republic Mod Manager.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. The plugin contains no third-party code; the loader SDK header comes from the TesmioLoader by MaxLegend (GPL v3). The complete source lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/ui_layout_fixes.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Does the spacing apply to other buildings as well?**
A: No. Only to buildings with `$TYPE_CUSTOMHOUSE`; every other window keeps the native spacing.

**Q: Do I have to restart the game after a change?**
A: Yes. Settings are read only at start.

**Q: What happens with an invalid row spacing?**
A: The plugin warns in the log and uses 30.0; the module stays active.

**Q: A listed text is still too wide?**
A: Lower `Characters per line`, best with a width of its own for that entry in the list; the module spreads the words evenly over the lines. If the text needs more lines than `Line limit`, the module widens the lines again; raise the limit or set it to 0.

**Q: How do I find the text id of a caption?**
A: Quickest through "Choose text..." in the add dialog of Republic Mod Manager: search for a word of the caption or set "Lines from (characters)" to 60, for example; a double click takes the id. Without the text picker: set `Find long texts (characters)` (`log_long_texts` in the INI) to 70, for example, start the game and open the window; the detail log then lists every shown text id with a line over 70 characters, together with the start of the text. Set it back to 0 afterwards.

**Q: Will more windows be added?**
A: The plugin is built for it: every window is its own module with its own section in the INI.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager shows every switch and value with descriptions and checks the value ranges.

---

**Last update:** UI Layout Fixes 0.3.1  
**For:** WRSR 1.1.1.13 | TesmioLoader API 4
