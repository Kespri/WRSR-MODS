# 🏭 Deposits Plus 0.4.19

**Extension of the TesmioLoader plugin deposits**

Fully configurable resource deposits for *Workers & Resources: Soviet Republic* 1.2.0.9 with natural generation, a visual ground texture and vehicle integration.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Value ranges](#-value-ranges)
- [Frequency & size](#-frequency--size)
- [Natural generation](#-natural-generation)
- [Sandy meadow](#-sandy-meadow-visual-texture)
- [Working vehicles](#-working-vehicles)
- [Relation to the original plugin](#-relation-to-the-original-plugin)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader b0.4.5 or newer (API 4)
- Recommended: Resources Plus or Resources – only needed so that a deposit's minimap button can show the icon of a resource registered there (`icon`). The deposits themselves do not need it.

### In three steps
1. **Subscribe:** the TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Switch the original off:** if you also have the deposits plugin by MaxLegend, switch it off. Republic Mod Manager does that by itself on saving; in TesmioLauncher clear the checkbox of deposits. Unsubscribing is not necessary.
3. **Start the game** and load a saved game. The settings live in `deposits_plus.ini`, most comfortably through Republic Mod Manager.

---

## ✨ Features

### 🎯 Original functions (kept)
- ✅ Custom deposit types (not present in the base game)
- ✅ Fully INI-driven: types, minimap, editor brushes
- ✅ Code patch for new mineral types

### 🆕 Extensions (new in Deposits Plus)

#### 1️⃣ **Natural generation** (`generation`)
- Elongated, branched deposit fields instead of grid points
- Fading richness (rich → weak → empty)
- Automatic adaptation to the country border and to water
- No overlap with other deposits
- Random or fixed seed per world

#### 2️⃣ **Sandy meadow** (`sand_surface`)
- Visual ground texture on sand deposits
- Strength scales with richness (rich fields show more clearly)
- Tile table per ground texture: sets Vanilla, Siberia (summer and snow-dusted autumn), Asia - Jungle and Ultimate Vanilla +; your own DDS files in your own folders welcome
- Purely visual, no gameplay effect
- Desert maps: `desert_fill` turns the whole land into the sand deposit, organically with 60 to 100 % in the lowlands and thinning out with height; the ores still find room

#### 3️⃣ **Working vehicles** (`working_vehicle_skill`)
- A deposit can borrow the vehicle skill of gravel mining
- Example: sand with `gravelmining`
- Works with the vehicle skills the game already has

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice, and Deposits Plus and the original deposits plugin never run at the same time.

### ⚠️ Important: switch the original plugin off

If you also have the deposits plugin by MaxLegend (subscribed or in `tesmioloader\plugins`):
- **Republic Mod Manager:** switches deposits off by itself on saving as soon as Deposits Plus is switched on
- **TesmioLauncher:** clear the checkbox of deposits
- **Unsubscribing or deleting is not necessary** – while the original is loaded, Deposits Plus stays idle by itself (log line `deposits_plus  idle`)

---

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item
2. The TesmioLoader (b0.4.5 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. The DLL finds the textures next to itself in the package
4. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

---

### Method 2️⃣: By hand into the loader folder

```
1. Copy deposits_plus.dll and deposits_plus.ini from plugins\
   → <game>\tesmioloader\plugins\

2. Copy the folder plugins\deposits_plus (textures and brush pictures)
   → <game>\tesmioloader\plugins\deposits_plus\
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

The package ships an editor schema in the `config` folder. Republic Mod Manager (version 0.6.3 and later) shows Deposits Plus with four tabs, in German and English:

- **General:** notices, buttons for this guide, the plugin switches "Game file adjustment" (the code patch), "Minimap layers" and "Terrain editor brushes", and the card "Natural generation"
- **Sand structure:** the sandy meadow
- **Sand tiles:** the tile table, one entry per ground texture with colour and normal-map file
- **Deposits:** the list on the left, every setting of the selected deposit on the right, a plus button for new deposits with resources from Resources Plus or Resources and an automatically proposed type number

Personal changes live in `user_config\deposits_plus.editor.ini`; the effective file is `tesmioloader\plugins\deposits_plus.ini`. The INI in the package stays untouched; a Steam update is the new baseline at once. In RMM, Deposits Plus and deposits are a replacement pair: when you switch one on, RMM switches the other one off. If you prefer editing the INI by hand, everything you need is below.

---

## ⚙️ Configuration

### Main file: `deposits_plus.ini`

The original INI lives:
- **Workshop:** in the package folder under `plugins\deposits_plus.ini`, next to the DLL
- **Installed by hand:** `tesmioloader\plugins\deposits_plus.ini`

If there is a `tesmioloader\plugins\deposits_plus.ini`, the plugin reads that file, otherwise the INI next to its DLL. Republic Mod Manager always writes the effective version to `tesmioloader\plugins\deposits_plus.ini`.

⚠️ **Comments only on their own lines, starting with `;`.** The parser takes everything after `=` as the value. A comment behind a value breaks tokens and switches generation off ("generation disabled for safety" in the log). No hot reload: edit the INI, restart the game, load the saved game.

### Section `[deposits_plus]` (global switches)

```ini
[deposits_plus]
; Code and UI patches
; 1 splices the new deposit types into the game executable (required for everything else)
code_patch = 1
; 1 adds a minimap button and overlay layer per deposit (hover previews, click keeps)
minimap = 1
; 1 adds a paint/erase pair per deposit to the terrain editor
editor = 1

; Natural generation
; 1 checks every deposit type after each world load and places the ones still empty
generation = 1
; 1 also fills channels recorded as empty earlier; 0 protects old empty channels
generate_existing_empty = 1
; first load of a save made with the original deposits plugin: sand is taken over from the old
; terrain channel. With fewer cells there than given here the channel counts as empty and sand
; is distributed like on a new map. 0 takes over any amount
legacy_adopt_min_cells = 500
; 0 = random seed per new world; any other number = always the same layout
generation_seed = 0

; Distances
; minimum distance between a new field and any other deposit (metres)
generation_gap_m = 40
; minimum distance between a new field and water (metres)
generation_shore_m = 40
; height the terrain inside a cell must lie above the water level (metres)
generation_water_clearance_m = 2
; 1 = the terrain's gravel/rock zone counts as occupied too (mountain maps such as Siberia then leave almost no room)
generation_block_gravel = 0

; Sandy meadow (optional)
; 1 = on, 0 = off
sand_surface = 1
; strength 0 to 1; 1.0 = full look, smaller values fade it
sand_surface_strength = 1.0
; token of the deposit the surface follows
sand_surface_token = $TYPE_MINE_SAND
```

### Per deposit: its own section

The section name is the deposit's name; `deposits_plus` is the only forbidden name. Example sand, as shipped, with every possible key:

```ini
[sand]
; required: the token in the mine's building.ini
token = $TYPE_MINE_SAND
; required: type number 10 to 127, no duplicates (0 to 9 belong to the game)
type = 11
; auto, resourcemap, resourcemap2 ... resourcemap10 or terrain
map = terrain
; colour channel 0 to 3 of the map; required unless map = auto
component = 1
; 1 allocates a separate resource channel and copies existing sand there
independent_map = 1
; 1 = on desert maps ($TYPE_DESERT in script.ini) the whole land is this deposit when its map is first created; never blocks the other deposits
desert_fill = 1
; 1 = richness falls with height (full band in the lowlands, 0 on the highest land), 0 = the band everywhere
desert_fill_relief = 1
; richness band in percent in the lowlands, varied by noise
desert_fill_min = 60
desert_fill_max = 100
; 7 = mine (default), 92 = water well
building_type = 7
; search radius of the mine: oil, ore, bauxite, gravel, wood, water, watersurface, or a number in metres
radius = gravel
; resource whose icon the minimap button shows (empty = no icon)
icon = sand
; 1 = minimap button and layer (hover previews it, click keeps it), 0 = none
minimap = 1
; name of the editor brush, at most 31 characters (empty = no brush)
editor = sand
; only for the depletion plugin: tonnes per saturated texel, 0 = infinite, empty = global figure
; deplete = 500

; Natural generation
; 1 places this deposit once, 0 only prevents the first placement
generation = 1
; frequency 1 to 6 (see table), default 3
generation_frequency = 5
; size class 1 to 3 (see table), default 2
generation_size = 3
; richness 0.001 to 1, independent of the presets
; generation_richness_min = 0.45
; generation_richness_max = 1.00

; Working vehicles: gravelmining or none; needs building_type = 7
working_vehicle_skill = gravelmining
```

Further deposits in the shipped INI: copper (type 10), clay (type 12) and gas (type 13). A deposit is rejected and reported in the log when its token is missing, its type number lies outside 10 to 127, its map lies beyond resourcemap10, its `component` lies outside 0 to 3, its `building_type` is neither 7 nor 92, its type number, token or channel is a duplicate, or its radius is unusable. An unknown word in `map` is only reported in the log; resourcemap2 is used then.

---

## 📏 Value ranges

The DLL checks these limits. A value outside them switches generation off for the deposit (section) or for all (section `[deposits_plus]`) and writes "generation WARN … generation disabled for safety" to the log. Exceptions: with `type` and `component` the deposit is rejected, and a brush name that is too long is cut off after 31 characters.

| Key | Range | Default |
|---|---|---|
| `generation`, `generate_existing_empty`, `independent_map` | 0 or 1 | 1 / 1 / 0 |
| `legacy_adopt_min_cells` | 0 to 1048576, whole number | 500 |
| `generation_seed` | 0 to 4294967295, integer | 0 |
| `generation_gap_m`, `generation_shore_m` | 0 to 500 m | 40 / 40 |
| `generation_water_clearance_m` | 0 to 50 m | 2 |
| `generation_block_gravel` | 0 or 1 | 0 |
| `generation_frequency` | 1 to 6 | 3 |
| `generation_size` | 1 to 3 | 2 |
| `generation_richness_min`, `_max` | 0.001 to 1 | 0.45 / 1.00 |
| `sand_surface_strength` | 0 to 1, otherwise the surface is off | 1.0 |
| `type` | 10 to 127 | required |
| `component` | 0 to 3 | required, except with auto |
| `editor` | at most 31 characters | empty |

---

## 📊 Frequency & size

### Frequency levels (`generation_frequency`)

| Value| Frequency          | Fields |
|------|--------------------|--------|
|  1   | extremely rare     |   1    |
|  2   | rare               |   2    |
|  3   | normal (default)   |   3    |
|  4   | frequent           |   4    |
|  5   | very frequent      |   5    |
|  6   | extremely frequent |   6    |

### Field sizes (`generation_size`)

|Value| Size             | Base radius   |
|-----|------------------|---------------|
| 1   | small            |   150–350 m   |
| 2   | medium (default) |   350–550 m   |
| 3   | large            |   550–750 m   |

**Note:** the size scales the whole field, not single points. Fields are elongated and branched, not circles; separate pieces count as one field. Along its main axis a field spans about 3.4 to 4.6 times the base radius, so large fields need room.

---

## 🌍 Natural generation

### How does it work?

1. **On load:** the plugin compares every deposit in the INI, by token, with the history of the saved game
2. **New deposits:** are placed once, at the first terrain draw after loading
3. **Existing ones:** stay as they are, even after complete depletion; nothing is refilled
4. **Shape:** elongated, branched fields with fading richness

### Rules

✅ **Allowed:**
- Stretched fields with irregular edges
- Occasional branches
- Real gaps and gradually falling richness
- Several separated pieces per field

❌ **Not allowed:**
- Placement in water
- Less distance to the shore than `generation_shore_m`
- Overlap with other deposits, including the game's oil, iron, coal, uranium and bauxite; the gravel/rock zone only with `generation_block_gravel = 1`
- Leaving the country border (without a border polygon the rectangular building limits apply)

A field counts only if at least 60 % of its area remains after clipping. If there is not enough room, fewer or no fields are created; the log states target, result and the reasons for rejection.

### First load of an old saved game

⚠️ **Important: adopt old saved games safely**

```
1. Back up the complete saved-game folder
2. Do NOT remove or reorder the existing deposits in the INI
   (old saves without a mapping list depend on the order)
3. Load the save → save under a new name
   → tesmio_deposits.bin is created
4. From the second load on only the token matters, the order no longer does
```

### Adding a new deposit

```
1. Save the game
2. Add a new section to deposits_plus.ini with its own token and type
   (in Republic Mod Manager: Deposits tab, plus button)
3. Restart the game completely (loading from the main menu is not enough)
4. Load the save → only the new deposit is placed
5. Save
```

**Brush pictures:** a brush in the terrain editor needs two pictures, `tool_paint_<name>.png` and `tool_erase_<name>.png` (96 x 96 pixels); `<name>` is the value of `editor`. The game loads them from `tesmioloader\vfs\media_soviet\editor`. For the shipped deposits the pictures come in the package under `plugins\deposits_plus\editor`: the plugin copies them into the vfs folder at game start when none of that name is there yet. For a deposit of your own you put your two pictures into the vfs folder yourself. If a picture is missing, the button shows a picture from the base game; the brush works all the same. The name on the brush comes from the resource named under `icon`; if none is named, the brush is called like the deposit.

---

## 🏞️ Sandy meadow (visual texture)

### What does it do?

Blends a sandy, patchy ground texture over sand deposits, more clearly on rich deposits. **Purely visual – no gameplay effect.** Saved game and maps are not changed.

- **Meadow:** sand patches on green meadow in summer, on brown meadow in autumn
- **Siberia:** own entries for summer and the snow-dusted autumn (`grass2snow.dds`)
- **Jungle:** summer with sand; autumn runs through the normal autumn meadow
- **Desert and winter:** always native - desert is sand already, snow covers everything
- **With mining:** the texture disappears with the resource

Requirement: the deposit named in `sand_surface_token` has its own channel from resourcemap3 upwards, for sand through `independent_map = 1`. Otherwise the log reports "no independent map; disabled".

### Setting

```ini
[deposits_plus]
; 1 = on, 0 = off
sand_surface = 1
; 0 to 1, 1.0 = full look
sand_surface_strength = 1.0
sand_surface_token = $TYPE_MINE_SAND
```

### Tile table

One section `[sand_tile:<name>]` per ground texture: `base` is the texture the map's material.mtl names on slot 5, folder included; `color` and `normal` are your DDS files under `deposits_plus\assets`, which the DLL looks for beside itself first (package: `plugins\deposits_plus\assets`) and then under `tesmioloader\plugins\deposits_plus\assets`. The files may sit in set folders (`Siberia/sand_meadow_siberia_color.dds`); `..` and absolute paths are refused. In Republic Mod Manager the table lives on the Sand tiles tab: the file fields list every DDS file grouped by set folder, and before saving RMM checks that the file exists and has the right format.

```ini
[sand_tile:meadow]
base   = tiles_normal/grass2.dds
color  = Vanilla/sand_meadow_color.dds
normal = Vanilla/sand_meadow_normal.dds

[sand_tile:siberia_autumn]
base   = dlc2/tiles_siberia/grass2snow.dds
color  = Siberia/sand_meadow_autumn_siberia_color.dds
normal = Siberia/sand_meadow_autumn_siberia_normal.dds
```

Shipped sets: `Vanilla` (meadow summer/autumn), `Siberia` (summer and snow-dusted autumn), `Asia - Jungle` (summer) and `Ultimate Vanilla +` (a meadow pair matching that texture pack, not the default: point the meadow entries at its files when you use the pack). A ground texture without an entry stays native, and so does an entry whose files are missing (log line `sand surface WARN tile`). Known ground textures: `tiles_normal/grass2.dds`, `tiles_normal/grass2_fall.dds`, `dlc2/tiles_siberia/grass2.dds`, `dlc2/tiles_siberia/grass2snow.dds`, `dlc2/tiles_asia/jungle_swamp_dm.dds`.

### Files

```
Vanilla\sand_meadow_color.dds                          (meadow summer colour)
Vanilla\sand_meadow_normal.dds                         (meadow summer normal map)
Vanilla\sand_meadow_autumn_color.dds                   (meadow autumn colour)
Vanilla\sand_meadow_autumn_normal.dds                  (meadow autumn normal map)
Siberia\sand_meadow_siberia_color.dds                  (Siberia summer)
Siberia\sand_meadow_siberia_normal.dds
Siberia\sand_meadow_autumn_siberia_color.dds           (Siberia snow-dusted autumn)
Siberia\sand_meadow_autumn_siberia_normal.dds
Asia - Jungle\sand_meadow_jungle_color.dds             (jungle summer)
Asia - Jungle\sand_meadow_jungle_normal.dds
Ultimate Vanilla +\sand_meadow_color_ultimatevanilla.dds          (meadow summer for Ultimate Vanilla+)
Ultimate Vanilla +\sand_meadow_normal_ultimatevanilla.dds
Ultimate Vanilla +\sand_meadow_autumn_color_ultimatevanilla.dds   (meadow autumn for Ultimate Vanilla+)
Ultimate Vanilla +\sand_meadow_autumn_normal_ultimatevanilla.dds
```

**Format:** square, power of two from 256 to 4096 (1024 or 2048 recommended), complete mipmap chain
- **Colour:** BC1/DXT1
- **Normal:** BC3/DXT5

### In-game test

```
1. Restart the game completely
2. Look at a sand deposit on green meadow → patches visible?
3. Autumn → brown meadow with sand patches?
4. Snow → snow cover overlays everything?
5. Reload from the main menu → same look?
```

Expected in `tesmioloader.log`:
```
sand surface assets from <folder>
sand surface shader preparation: 13/13 verified native programs augmented
```

---

## 🚜 Working vehicles

### What does it do?

A deposit can borrow the vehicle skill of gravel mining. Example: sand with the **excavators that also mine gravel**.

### Setting

```ini
[sand]
token                 = $TYPE_MINE_SAND
working_vehicle_skill = gravelmining
building_type         = 7
```

**Supported values:**
- `none` (default, no vehicle assignment)
- `gravelmining` (the skill of gravel mining)

Any other value produces a warning in the log and acts like `none`. Case does not matter.

**Requirement**
- `building_type = 7` (mine). Otherwise the vehicle assignment is disabled with a warning; the deposit itself is kept.

### How does it work?

- The mine's `building.ini` keeps its token, for example `$TYPE_MINE_SAND`
- Vehicles use their existing skill `$SKILL_GRAVELMINING`
- **No new vehicle type** – existing excavators are simply assigned
- Needs `$WORKING_VEHICLES_NEEDED` and parking slots in the `building.ini` as always
- Resources, production, fuel: everything as usual

### Gameplay test

```
1. Open a sand mine with working-vehicle slots
2. Buy and assign an excavator
3. Without workers → does the excavator mine sand?
4. Fuel empty → does it stop correctly?
5. Old gravel and bauxite mines → no change
```

---

## 🔗 Relation to the original plugin

### Coexistence with `deposits.dll`

Deposits Plus and the original plugin `deposits` share:
- **the same registry service:** `deposits`
- **the same save file:** `tesmio_deposits.bin`
- **compatibility:** existing saved games and type numbers stay valid as long as type numbers and channels are not changed

### If both are installed

**Deposits Plus loads only while the original is switched off.**

If the original is loaded (from `plugins` or from the Workshop folder), or `plugins\deposits.dll` exists and is enabled in tesmioloader.ini:
- Deposits Plus stays idle before it patches anything
- In the log: `deposits_plus  idle - the original deposits.dll is loaded` (or `… is present in plugins\ and enabled in tesmioloader.ini`)
- The original loads as usual

**Solution:** switch the original off in TesmioLauncher or in Republic Mod Manager. Deleting is not necessary.

### Own deposits from the original

If you added deposits of your own to the original's `deposits.ini`, Deposits Plus takes them over once: with the same type number and the same token, so your saves keep fitting. The four standard deposits (copper, sand, clay, gas) come with Deposits Plus itself.

- **With Republic Mod Manager** (0.6.7 or newer): on the Deposits Plus page they stand as your own deposits, and a notice names them. The next save takes them over.
- **Without Republic Mod Manager:** at the first game start the plugin creates `tesmioloader\plugins\deposits_plus.ini`: the shipped list plus your deposits. From then on that file counts. New default values from a Steam update arrive again only after you have saved once in Republic Mod Manager; your deposits stay.
- The `deposits.ini` is looked for in `tesmioloader\plugins`, otherwise next to the original's DLL in the Workshop folder: in the package folder itself, in its `plugins` folder or in a subfolder of that.
- If the type number or the token of a deposit is already in use, it is not taken over; the log and the page in Republic Mod Manager name it.
- If you delete a deposit that was taken over, it does not come back.

---

## 💾 Compatibility

### Save format

- **The DDS maps of the saved game** hold the mining state as always; the maps resourcemap3 and higher created by the plugin are saved with the terrain
- **`tesmio_deposits.bin`** additionally stores:
  - deposit identities (tokens)
  - channel assignments
  - the generation seed (same seed, same layout)
  - the list of removed deposits

### Backing up saves

When copying or backing up, **always take the complete saved-game folder**:
```
MyGame\
├── resourcemap*.dds        (the deposit maps)
├── buildings.bin
├── ...
└── tesmio_deposits.bin     (← do not forget)
```

### Version compatibility

- **Within the 0.4 series:** updates carry save games and `tesmio_deposits.bin` over automatically; type numbers, map slots and tokens of running save games stay off limits (see above)
- **With Deposit Depletion:** keeps working as before; the key `deplete` is passed on to the depletion plugin
- **With Soviet Mod Loader:** SML brings deposits of its own. While it runs, this plugin pauses and says so in the log; if you want Deposits Plus instead, switch `soviet_mod_loader` off in `tesmioloader.ini` (in Republic Mod Manager the "Plugin active" switch)
- **0.4.19:** for WRSR 1.2.0.9 (hotfix for the rocket DLC); otherwise unchanged.
- **0.4.18:** for WRSR 1.2.0.8 with the rocket DLC; otherwise unchanged.
- **0.4.17:** the game starts faster when you are subscribed to many Workshop items: the `deposits.ini` of the original is only looked for where a package keeps its plugins, no longer in the whole Workshop folder
- **0.4.16:** a brush without pictures of its own now really shows a picture from the base game; before, the picture of the brush before it appeared. A brush whose deposit names no resource is now called like the deposit instead of "Bauxite"
- **0.4.15:** deposits of your own from the original's `deposits.ini` are taken over once, with the same type number and the same token (see "Own deposits from the original")
- **0.4.14:** the brush pictures for sand, clay, gas and copper now come with the package. If one is missing in the loader folder under `vfs\media_soviet\editor`, the plugin copies it there at game start; a picture that is already there stays untouched. Plus tidied log texts
- **0.4.13:** loaded from the Workshop, the plugin now finds its deposits without Republic Mod Manager as well: when there is no `tesmioloader\plugins\deposits_plus.ini`, it reads the INI next to its DLL
- **0.4.12:** new key `legacy_adopt_min_cells` (default 500): on the first load of a save made with the original deposits plugin, an old sand channel with only a few stray cells counts as empty and sand is distributed like on a new map. Before, such a save stayed without sand
- **0.4.11:** for WRSR 1.1.1.13. The plugin now also recognises the original deposits when it is loaded from the Workshop folder, and then stays idle
- **0.4.10:** safety round, no new feature: the extra maps are hooked only once the type patch is in place; the terrain editor overlay and the brush restore their state after a fault and switch themselves off; the extra map table is read under the lock; a blank DDS file that could not be written completely is reported with the error code and removed; `building_type` must be 7 or 92, otherwise the deposit is rejected; `minimap` must be 0 or 1, otherwise WARN and default; the headers of the game's engine DLL are checked before they are read. In RMM: at most 32 deposits and 16 sand tiles (the plugin's limits), the card "Natural generation" now sits on the General tab
- **0.4.9:** the guard against Soviet Mod Loader also works when Deposits Plus loads before SML (local copy in `plugins`, alphabetical order): it is enough that `plugins\soviet_mod_loader.dll` exists and is switched on in `tesmioloader.ini`.
- **0.4.8:** the plugin steps aside on its own under Soviet Mod Loader
- **0.4.7:** the brush name may now be up to 31 characters. Existing names and the two PNG files stay as they are

---

## ⚙️ Troubleshooting

### Common problems

| Problem                    | Cause                                                      | Solution                                                                        |
|----------------------------|------------------------------------------------------------|---------------------------------------------------------------------------------|
| Deposits Plus does nothing | original deposits enabled                                  | check the log for `deposits_plus  idle`, switch the original off                |
| Deposits not generated     | `generation = 0` or an invalid value                       | check the log for `generation WARN`, bring the value into range                 |
| Textures not visible       | assets folder missing or no independent channel            | `deposits_plus\assets` beside the DLL or under `plugins`; `independent_map = 1` |
| Excavators do not work     | `working_vehicle_skill` wrong or `building_type` not 7     | check the log for `vehicles WARN`                                               |
| Old deposits gone          | INI heavily rebuilt                                        | restore the saved game, adopt it again                                          |

### Logging

Every message is in `tesmioloader.log`.
- In **Republic Mod Manager** the log icon in the sidebar opens the log view with filter and sender.

Search for:
- `generation` → generation
- `vehicles` → working vehicles
- `sand surface` → ground texture
- `deposits_plus` → general messages, rejected deposits

---

## 📦 File structure

**Workshop package** (Steam subscription)
```
<Workshop item>\
├── plugins\
│   ├── deposits_plus.dll           (plugin)
│   ├── deposits_plus.ini           (original INI)
│   ├── deposits_plus\assets\       (textures of the sandy meadow)
│   │   ├── Vanilla\                (sand_meadow_*.dds)
│   │   ├── Siberia\                (sand_meadow_siberia_*.dds, sand_meadow_autumn_siberia_*.dds)
│   │   ├── Asia - Jungle\          (sand_meadow_jungle_*.dds)
│   │   └── Ultimate Vanilla +\     (sand_meadow_*_ultimatevanilla.dds)
│   └── deposits_plus\editor\       (brush pictures: tool_paint_<name>.png, tool_erase_<name>.png)
├── config\                         (editor schema for Republic Mod Manager)
│   ├── deposits_plus.launcher.ini
│   └── languages\
│       ├── de.ini
│       └── en.ini
├── rmm.mod.ini                     (manifest for Republic Mod Manager)
├── workshopconfig.ini              (Steam Workshop entry)
├── previewimage.png
├── LICENSE                         (GNU GPL v3)
├── README_DE.md
├── README_EN.md
└── THIRD-PARTY-LICENSE.txt         (licence of the included shader compiler code)
```

**Loader folder** (only with method 2 or with Republic Mod Manager)
```
<game>\tesmioloader\
├── plugins\
│   ├── deposits_plus.dll           (method 2 only)
│   ├── deposits_plus.ini           (effective INI)
│   └── deposits_plus\              (assets and editor, method 2 only)
├── vfs\media_soviet\editor\        (brush pictures; the plugin copies missing ones at game start)
└── user_config\
    └── deposits_plus.editor.ini    (personal values from Republic Mod Manager)
```

Loaded from the Workshop, DLL and textures stay in the package; `tesmioloader\plugins` then holds only the effective INI, once you have saved in Republic Mod Manager.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. Deposits Plus is a fork of the `deposits` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3, https://github.com/MaxLegend/TesmioLoader; service name and savegame file deliberately stay identical to the original. **Code from the DirectX Shader Compiler** (DXIL signature of the adjusted shaders) – see `THIRD-PARTY-LICENSE.txt`. The complete source of Deposits Plus lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/deposits_plus.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Can I use both plugins at the same time?**
A: No. Either the original OR Deposits Plus. While the original is enabled, Deposits Plus stays idle.

**Q: Do I lose my saved games?**
A: No. Old saves keep working. `tesmio_deposits.bin` is created on the first save after that.

**Q: Can I change the generation later?**
A: Yes, but only deposits not yet placed are generated. Existing fields stay.

**Q: Do I need all functions?**
A: No. The code patch for the deposit types is the minimum. Generation, sand texture and working vehicles are optional.

**Q: Does Deposit Depletion still work?**
A: Yes, fully compatible. Mining works as always.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager shows every setting with a description, checks the value ranges and proposes the next free type number for new deposits.

---

**Last update:** Deposits Plus 0.4.19  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
