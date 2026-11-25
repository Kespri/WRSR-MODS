# 🧰 Resources Button Fix 0.4.5

**TesmioLoader plugin for compact tool grids in the terrain editor**

Rearranges the tool buttons in two windows of the terrain editor of *Workers & Resources: Soviet Republic* 1.2.0.9: in the **Resources** window the paint buttons stand in blocks above their erase buttons, in the **Rocks/Gravel** window either side by side or in two rows. Extra tools from deposits.dll or Deposits Plus, such as sand, are included automatically.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Value ranges](#-value-ranges)
- [Live reload](#-live-reload)
- [Automatic scaling](#-automatic-scaling)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader b0.4.5 or newer (API 4)
- Optional: deposits.dll or Deposits Plus – their additional resource and terrain tools are arranged in the right window as well.

### In three steps
1. **Subscribe:** the TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Start the game** and open the Resources or Rocks/Gravel window in the terrain editor.
3. **Adjust** if you like: columns, blocks, size and spacing live in `resources_button_fix.ini`, most conveniently through Republic Mod Manager. Most changes apply the next time the window is opened.

---

## ✨ Features

### 🎯 Core function
- ✅ Resources window: paint buttons in blocks above their matching erase buttons, up to 32 columns and 4 blocks
- ✅ Rocks/Gravel window: paint and erase side by side (P E P E) or as two rows (P P above E E)
- ✅ Automatic or fixed button size, freely adjustable spacing and offsets
- ✅ The window grows when needed; the red delete-all button and the Brush controls move down with it
- ✅ Tree tools and every other editor window stay untouched; no game file and no VFS file is written

### 🆕 New in 0.4.0
- ✅ Every Republic Mod Manager text in player style: notice box on General, "Troubleshooting" card with the detailed log, ON/OFF instead of 1/0.
- ✅ **INI beside the DLL:** when `tesmioloader\plugins\resources_button_fix.ini` does not exist, the DLL reads the INI from its own folder, i.e. from the Workshop package. The plugin runs straight from the Steam subscription without copying anything.
- ✅ The chosen configuration file is logged at start; live reload follows that file.

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice.

---

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item
2. The TesmioLoader (b0.4.5 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

---

### Method 2️⃣: By hand into the loader folder

```
1. Copy resources_button_fix.dll and resources_button_fix.ini from plugins\
   → <game>\tesmioloader\plugins\
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

The package ships a launcher schema in the `config` folder. Republic Mod Manager (version 0.6.3 and later) shows Resources Button Fix in three tabs, German and English:

- **General:** notes, a button for this guide and Troubleshooting (detail log)
- **Resources window:** arrangement (columns, blocks, size, spacing) plus position, window and delete-all button
- **Rocks/Gravel window:** arrangement (P E P E or two rows, size, spacing) plus position and window

The "Plugin active" switch in the header also sets `enabled = 1` when switched on. Republic Mod Manager keeps personal values separately in `tesmioloader\user_config\resources_button_fix.ini` and writes the finished INI to `tesmioloader\plugins\resources_button_fix.ini` on saving – that is where the plugin reads it. The INI in the package stays untouched. If you prefer editing the INI by hand, everything else is below.

---

## ⚙️ Configuration

### Main file: `resources_button_fix.ini`

The DLL reads in this order:
- **First:** `tesmioloader\plugins\resources_button_fix.ini` if it exists (installed by hand or written by Republic Mod Manager)
- **Otherwise:** the INI beside the DLL, in the Workshop package `plugins\resources_button_fix.ini`

⚠️ **The file is checked strictly.** UTF-8 without BOM; exactly one `[general]`, `[resources_window]` and `[rocks_window]` section; no unknown or repeated sections and keys; every key with a value; switches only `0` or `1`; numbers within the documented range; comments only on their own lines with `;` or `#`, never behind a value. A missing single key uses the compiled default, which is the same value as in the shipped INI (column "Default" under Value ranges); a missing required section or an otherwise invalid file prevents any change to the game at start. The file may be at most 16 MiB, a value at most 63 characters.

### Section `[general]`

```ini
[general]
; 1 enables the whole plugin, 0 disables it (restart required)
enabled = 1
; 1 writes layout diagnostics to the log, troubleshooting only
debug = 0
```

### Section `[resources_window]`

```ini
[resources_window]
; 1 rearranges the Resources window, 0 keeps the original layout
enabled = 1
; resource pairs beside each other per block (1 to 32)
columns_per_block = 6
; blocks below each other (1 to 4); capacity = columns x blocks
maximum_blocks = 2
; auto or a fixed share of the original size 0.25 to 1.00
button_scale = auto
; distance within the row and between paint and erase row, 1.00 = normal
horizontal_spacing = 1.00
vertical_spacing = 1.00
; extra distance between two blocks
block_gap = 0.35
; offset of the whole grid, positive = right or down
x_offset = 0.00
y_offset = 0.00
; 1 lets the window grow, 0 shrinks the buttons to fit
expand_window = 1
; below_grid or original
cancel_position = below_grid
; visible gaps around the red delete-all button
grid_to_cancel_gap = 10.00
cancel_to_brush_gap = 40.00
; fine adjustment of the red button
cancel_x_offset = 0.00
cancel_y_offset = 0.00
```

The two visible gaps are arranged like this:

```
[resource buttons]
       |  grid_to_cancel_gap
   [red X button]
       |  cancel_to_brush_gap
     Brush
```

### Section `[rocks_window]`

```ini
[rocks_window]
; 1 rearranges the Rocks/Gravel window, 0 keeps the original layout
enabled = 1
; 1 = P E P E in one row, 2 = paint row above erase row
blocks = 2
; auto or a fixed share of the original size 0.25 to 1.00
button_scale = auto
horizontal_spacing = 1.00
vertical_spacing = 1.00
; offset of the whole grid
x_offset = 0.00
y_offset = 0.00
; 1 lets the window grow and moves the Brush controls down
expand_window = 1
; visible gap between grid and Brush with a growing window
controls_gap = 20.00
```

The `blocks` setting produces these arrangements (`P` = paint, `E` = erase):

```
blocks = 1          blocks = 2
P E  P E            P P
                    E E
```

---

## 📏 Value ranges

The DLL checks these limits on every read. A value outside rejects the whole file: at start the game stays unchanged, on live reload the previous values stay active.

| Key | Range | Default |
|---|---|---|
| `enabled`, `debug`, `expand_window` | exactly 0 or 1 | 1 / 0 / 1 |
| `columns_per_block` | 1 to 32 | 6 |
| `maximum_blocks` | 1 to 4 | 2 |
| `blocks` | 1 or 2 | 2 |
| `button_scale` | `auto` or 0.25 to 1.00 | auto |
| `horizontal_spacing`, `vertical_spacing` | 0.50 to 2.00 | 1.00 |
| `block_gap` | 0.00 to 2.00 | 0.35 |
| `x_offset`, `y_offset`, `cancel_x_offset`, `cancel_y_offset` | -100.00 to 100.00 | 0.00 |
| `cancel_position` | `below_grid` or `original` | below_grid |
| `grid_to_cancel_gap`, `cancel_to_brush_gap`, `controls_gap` | 0.00 to 100.00 | 10.00 / 40.00 / 20.00 |
| File | at most 16 MiB, values at most 63 characters | |

---

## 🔄 Live reload

Almost every setting applies without a restart:

```
1. Edit the INI by hand and save it (Republic Mod Manager only saves while the game is not running)
2. Open the Resources or Rocks/Gravel window again
3. The changed file is read at the first recognised tool button
```

The file is checked as a complete new configuration set. If only one line is invalid, **no** new values are applied; all last valid settings stay active and the reason is logged (`reload-rejected`). Only `[general] enabled` needs a complete restart; a change while the game runs is logged but not applied.

---

## 📐 Automatic scaling

`button_scale = auto` computes a suitable size from the detected number of tools, the columns and the spacing. The safe lower limit is `0.25`; if the plugin has to use it, a warning is logged. The tool count comes from the previous complete window pass; newly added tools are taken into account stably from the following pass. Surplus tools beyond the capacity `columns_per_block x maximum_blocks` keep their original position and produce a single warning.

---

## 💾 Compatibility

### Game and loader
- Fixed code positions verified for 1.2.0.9; another game version is refused before any hook is installed
- Before every change the PE signature, 64-bit machine type, time stamp, image size, the bytes of the shared button hook and the reach of every jump are checked
- Calls from trees and other windows are passed through unchanged; a runtime fault safely disables the layout change for the session
- Without near memory for the optional transitions the arrangement stays active, only the following controls then keep their position

### Savegames
The plugin changes neither game files nor savegames; it only arranges buttons in the memory of the running game.

### Version compatibility
- **0.4.5:** for WRSR 1.2.0.9 (hotfix for the rocket DLC); otherwise unchanged
- **0.4.4:** for WRSR 1.2.0.8 with the rocket DLC; otherwise unchanged
- **0.4.3:** if a value is missing from the INI, the same default as in the shipped INI now applies. Before, three compiled defaults differed: columns per block and button size in the Resources window, gap to the Brush in the Rocks/Gravel window. Log texts tidied as well; with a complete INI nothing changes in the game
- **0.4.2:** the extra buttons of Deposits Plus are now recognised as well. Before, the plugin only looked for the original deposits plugin, and the extra pairs ran out of the Resources window
- **0.4.1:** for WRSR 1.1.1.13
- **0.4.0:** first published version

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| Plugin does nothing | `enabled = 0`, game version not 1.2.0.9 or invalid INI | check the log for `unsupported-build` or `configuration` |
| Change not visible | window not reopened or live reload rejected | close and open the window; check the log for `reload-rejected` |
| Buttons too small | automatic lower limit 0.25 reached | fewer columns, more blocks or `expand_window = 1` |
| Tools missing from the grid | capacity exceeded | raise `columns_per_block` or `maximum_blocks` |
| Red button in the wrong place | `cancel_position` or gaps | `below_grid` with `expand_window = 1`, adjust the gaps |

### Logging

All messages go to `tesmioloader.log` and the detail log `logs\tesmioloader.resources_button_fix.log`. Every message names level, source and rule; initialisation and start end with a summary.
- In **Republic Mod Manager** the log icon in the sidebar opens the log view with filter and sender.

Look for:
- `Configuration file` → which INI the DLL chose
- `configuration`, `unknown-key`, `config-range`, `duplicate-key` → rejected INI
- `reload-rejected` → live change discarded, previous values active
- `hook-install`, `near-memory`, `layout-fault` → hook or layout logic

---

## 📦 File structure

**Workshop package** (Steam subscription)
```
<Workshop item>\
├── plugins\
│   ├── resources_button_fix.dll    (plugin)
│   └── resources_button_fix.ini    (original INI)
├── config\                         (launcher schema for Republic Mod Manager)
│   ├── resources_button_fix.launcher.ini
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
├── plugins\
│   ├── resources_button_fix.dll    (method 2 only)
│   └── resources_button_fix.ini    (method 2, or the finished INI from Republic Mod Manager)
└── user_config\
    └── resources_button_fix.ini    (personal values from Republic Mod Manager)
```

Loaded from the Workshop, DLL and INI stay in the package. The DLL then reads the INI straight from there, as long as there is no `tesmioloader\plugins\resources_button_fix.ini`.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. The plugin contains no third-party code; the loader SDK header comes from the TesmioLoader by MaxLegend (GPL v3). The complete source lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/resources_button_fix.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Are the tree tools rearranged as well?**
A: No. Only the Resources and Rocks/Gravel windows; every other editor window stays unchanged.

**Q: Do I have to restart the game after every change?**
A: No. Only the plugin switch needs a restart; everything else applies the next time the window is opened.

**Q: What happens on an error in the INI?**
A: At start the game stays unchanged, on live reload the previous values stay active. The reason is in the log.

**Q: Does the plugin work with Deposits Plus?**
A: Yes. Extra tools from deposits.dll or Deposits Plus are arranged in the right window as well.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager shows every setting with a description and checks the value ranges.

---

**Last update:** Resources Button Fix 0.4.5  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
