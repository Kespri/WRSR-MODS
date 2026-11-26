# 🏭 Buildings Plus 0.2.3

**TesmioLoader plugin for new buildings from a configuration file**

Adds new buildings to *Workers & Resources: Soviet Republic* 1.2.0.9 without modelling or copying anything by hand: you name a donor building of the game (say, the bauxite mine) and say with commands what is to be different in its building.ini (say: replace `$PRODUCTION rawbauxite 0.50` by `$PRODUCTION raw_salt 1.0`). At every game start the plugin writes a complete Workshop item into `media_soviet\workshop_wip` from that, with model, textures, collision data, icon and the adjusted building.ini. Typical use: a salt mine and a salt refinery for new resources, without touching the game's coal mine.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Replacement rules](#-replacement-rules)
- [Value ranges](#-value-ranges)
- [Safety rules](#-safety-rules)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.2.0.9 (the plugin reads no game address, a game update can at most make a building.ini line invalid)
- TesmioLoader b0.4.5 or newer (API 4)
- Resources a building consumes or produces must exist in the game, for example through Resources Plus or the resources plugin.

### In three steps
1. **Subscribe:** Buildings Plus is part of the Workshop item Republic Mod Manager. The TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Declare buildings:** in Republic Mod Manager press "Add" on the Buildings tab and fill in object name, donor, name in the game and the lines (the Workshop id may stay empty). By hand: one section in `tesmioloader\plugins\buildings_plus.ini`, like the shipped example `[example_pharmacy]` (disabled).
3. **Restart the game completely.** The building appears in the build menu under its `name`; the log shows one line per section with folder and donor.

---

## ✨ Features

### 🎯 Core function
- ✅ One section per building: the donor and commands for what is to be different (append, replace, remove, insert)
- ✅ A complete Workshop item at game start: model, material (texture paths rewritten), the donor's emissive material, collision box, fire points, icon, workshopconfig.ini, renderconfig.ini, building.ini
- ✅ Every command touches exactly one place; everything else of the donor stays (connections, construction phases, particles, vehicle stations, the other storages)
- ✅ Folders are rewritten only when the section, the plugin or a donor file changed
- ✅ A folder without the plugin's stamp is never touched; real Workshop subscriptions are safe
- ✅ `prune` removes the folders of disabled or deleted sections again
- ✅ The same section format as `tesmio\buildings.ini` in content packages: Republic Mod Manager enters their buildings into your list

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice.

---

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item Republic Mod Manager – Buildings Plus
   sits in its folder plugins\buildings_plus
2. The TesmioLoader (b0.4.5 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. Declare buildings (Republic Mod Manager or INI), restart the game completely
4. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

---

### Method 2️⃣: By hand into the loader folder

```
1. Copy buildings_plus.dll and buildings_plus.ini from plugins\buildings_plus\
   → <game>\tesmioloader\plugins\

2. Declare sections in the INI, restart the game completely
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

Republic Mod Manager (version 0.7.0 and later) brings the settings page itself. It shows Buildings Plus in three tabs, German and English:

- **General:** notes, the card "Generated folders" with the switches Fill in the owner id and Clean up folders (prune), plus the card "Troubleshooting" with Always rewrite (always) and Detailed log
- **Buildings:** one row per building with its switch, name, number, donor, state and the buttons Change, Open and Delete. "Add" and "Change…" open the very same window with two tabs: **Details** (object name, Workshop id, donor, name in the game, description, life) and **Lines** (the donor's building.ini on the left, your commands on the right; you click a line, type the new one below it and choose Append, Replace, Before or After - the "Result" view shows the file that comes out of it)
- **Generated buildings:** what has already been generated under `media_soviet\workshop_wip`, with the "Fill in the owner" button that fills in a missing owner id

Republic Mod Manager does not offer the `object` key - there the object name is always the section name. Written by hand, `object` still counts.

Personal buildings live in `user_config\buildings_plus.editor.ini`, the effective file is `tesmioloader\plugins\buildings_plus.ini`; the INI in the Workshop item stays untouched. Buildings from content packages (Workshop items without a DLL of their own) are entered into the same file by RMM with "Provide in the game".

---

## ⚙️ Configuration

### Main file: `buildings_plus.ini`

The DLL reads in this order:
- **First:** `<game>\tesmioloader\plugins\buildings_plus.ini` if it exists (installed by hand, or the effective INI written by Republic Mod Manager)
- **Otherwise:** the INI next to the DLL, in the Workshop item `plugins\buildings_plus\buildings_plus.ini`

By hand, enter your own buildings only into the file in the loader folder (copy the INI from the Workshop item there): Steam overwrites the Workshop folder with every update.

⚠️ **Comments only on their own lines with `;`.** The file must be UTF-8 without BOM. Never write a `$TOKEN` into a comment line inside a building section. Changes take effect after a complete game restart.

### Section `[buildings_plus]`

```ini
[buildings_plus]
; 1 generates the buildings at game start, 0 does not load the plugin (folders stay)
enabled = 1
; 1 removes folders of deleted or disabled sections (only with the plugin's stamp)
prune = 0
; 1 rewrites every folder at every start
always = 0
; 1 logs every copied file and every removed donor line
verbose = 0
; 1 also fills the Steam id into generated folders of other generators
repair_owner_ids = 1
```

### Buildings: one section per building

```ini
[salt_mine]
enabled = 1
; Workshop id, optional: leave it out and the plugin assigns one from 9300000000 and remembers it
; id = 9300000001
; donor: a name under media_soviet\buildings_types without .ini
donor = bauxite_mine
; object name (folder and OBJECT_BUILDING), default = section name
object = SaltMine
; name in the build menu and in the Workshop entry, no quotes
name = Salt Mine
; description, one desc line per line
desc = Mines raw salt from a salt deposit.
; commands: what is to be different in the donor's building.ini
replace = $PRODUCTION rawbauxite 0.50 | $PRODUCTION raw_salt 1.0
replace = $STORAGE_EXPORT RESOURCE_TRANSPORT_GRAVEL 15.00 | $STORAGE_EXPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 20 raw_salt
remove = $RESOURCE_VISUALIZATION 0
```

**The four commands.** Each one touches exactly one place of the donor's file. Everything else stays as the donor has it.

| Command | Written as | What happens |
|---|---|---|
| Append | `add = new line` | The new line goes to the end of the file, right in front of the closing `end` |
| Replace | `replace = old line \| new line` | The old line goes, the new one takes its place |
| Remove | `remove = old line` | The old line goes |
| Insert | `insert = 0 \| anchor \| new line` | The new line goes **in front of** the anchor |
| Insert | `insert = 1 \| anchor \| new line` | The new line goes **behind** the anchor |

The old line and the anchor are lines of the donor, written exactly as they stand there (spaces at both ends do not count). Each must stand in the donor **exactly once**. The anchor `end` is the end of the file. All four commands may be written as often as you like.

An example: an extra storage for sand right behind the donor's fuel storage.

```ini
insert = 1 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand
```

**Entries that span several lines.** Some building.ini tokens carry their values in the lines below them — a connection its points, a goods display its placement. Such lines are one block. You write it into one command, with `|` between its lines:

```ini
add = $CONNECTION_WATERPIPE_INPUT | -92.5 -3.2 5.0 | -91.5 -3.2 5.0
replace = $RESOURCE_VISUALIZATION 1 | $RESOURCE_VISUALIZATION 0 | position 15.2912 0.0 -51.6213 | rotation 0.0 | scale 1 1 1 | numstepx 3.0 12 | numstept 4.0 3
```

Removing or replacing the token line takes its data lines with it. When the same token line stands twice in the donor, add its first data line as a second field — that says which one is meant:

```ini
remove = $RESOURCE_VISUALIZATION 2 | position -78.7332 0.0 -15.8727
```

**What else you need to know.**
- Storages count from 0 in the order they stand in the finished file, and a goods display (`$RESOURCE_VISUALIZATION`) names the number of its storage. So put a new storage where it belongs with Insert. Water and sewage storages go last.
- An insert may also hang on a line that an earlier command of the same section writes. If two earlier commands write the same line, add its data lines as further fields here as well to say which one is meant.
- One command per place: a line that a command already replaces or removes takes no second replace or remove.
- The plugin takes the commands in the order add, replace, remove, insert, whatever their order in the file.
- A section is all or nothing: one command that does not fit the donor and the building is not written. The log names every command that failed.

**The older way** with `line` and `strip` still works; a section may use it instead of the commands. How it works is described under "Replacement rules". Republic Mod Manager always writes new buildings as commands.

The donor sets the shape: a mine wants a mine as donor (conveyor, animation), a factory a factory, a shop a shop. `life` (default 3000) is the LIFE value in renderconfig.ini.

**A Workshop building as the donor.** Instead of a base-game name you may point at a building of a Workshop item you are subscribed to:

```ini
donor = 1872558150\gravelmine
```

The item id on the left, the building's folder inside that item on the right — the spelling Vanilla Buildings uses for its target files. The item is looked for in the game's Workshop folder and in `media_soviet\workshop_wip`, so a building of your own that is not published yet works as well.

What happens: the chosen folder is copied, together with every loose file of the item and every subfolder that holds no building of its own — asset folders are called `mtl`, `Textures` or `materials` depending on who built them, so what counts is what is in a folder, never its name. The other buildings of a multi-building item stay behind: one section is one building. The donor's `workshopconfig.ini` is not taken over; your clone gets one of its own that lists your building alone. The donor's `renderconfig.ini` stays, because it knows the donor's own file names.

Two things to keep in mind: you have to be subscribed to the item, or the section is skipped with an error line — and nothing of it ships with this plugin, the copy is made on your own machine. If you want to publish the result on the Workshop yourself, you need the original author's permission.

**Workshop id:** just leave `id` out. At game start the plugin looks for the highest number between 9300000000 and 9399999999 (in the catalog, in the INI and among the folders under workshop_wip, foreign ones included) and assigns the next one. The number then lives in `tesmioloader\plugins\buildings_plus.ids.ini` under the section name and stays there for good, because saved games know the building by its folder `workshop_wip\<number>`. The plugin always writes this file into the loader folder, never into the Workshop folder. A renamed section is a new building with a new number; a deleted section does not free its number. An explicit `id` (9000000000 to 9999999999, not 9100000000 to 9199999999) still works and wins. Keep the catalog file together with your saved games; profiles in Republic Mod Manager include it.

**A name from the text pack.** Normally you just write the name down, `name = Salt Mine`. It then sits in the file and reads the same in every game language.

With the Localization plugin running you may write a translation key instead:

```ini
name = localization.lang.salt_mine
```

Buildings Plus looks the key up at game start and writes the id it finds into building.ini, exactly the way the base game does it for its own buildings. The caption then comes from the Localization pack: in the game language when the pack carries it, in its fallback language otherwise. A value counts as a key when it holds at least one dot and nothing but letters, digits, dot, underscore and hyphen, so a name with spaces can never be mistaken for one.

Without Localization, or with a key it does not know, Buildings Plus uses the part after the last dot as the name, here `salt_mine`, and writes one warning into the log. The building still works.

---

## 🛠️ Replacement rules

This section is about the older way with `line` and `strip` only. There your lines stand in front of the donor's, and the donor's building.ini is taken over line by line. A donor line is dropped only when one of your lines replaces it or `strip` names it. Entries that span several lines are written there as several `line` rows one below the other; a `line` without a `$TOKEN` belongs to the line above it.

| Your line starts with | Dropped from the donor |
|---|---|
| the same token | the same line |
| `$NAME_STR` (from `name`) or `$NAME` | every `$NAME` line |
| a `$TYPE_*` | every `$TYPE_*` line, only one type is in effect |
| a `$STORAGE*` | every `$STORAGE*` line and `$RESOURCE_VISUALIZATION`, because the visualisation counts storages from zero |
| `$PRODUCTION`, `$CONSUMPTION` or `$CONSUMPTION_PER_SECOND` | all three: a recipe is replaced as a whole |

`$PRODUCTION_SEWAGE_POLLUTION` and `$CONSUMPTION_WATER_REQUIRED_QUALITY` are settings, not a recipe, and stay. What counts is the first `$TOKEN` anywhere in the line, the way the game's parser does it.

---

## 📏 Value ranges

| Quantity | Limit |
|---|---|
| `enabled`, `prune`, `always`, `verbose`, `repair_owner_ids` | exactly 0 or 1 |
| building sections | at most 256 |
| `id` | optional; number from 9000000000 to 9999999999, but not 9100000000 to 9199999999; unique in the file; assigned from 9300000000 when absent |
| `object`, section name | letters, digits, `_` and `-`, at most 64 characters |
| `donor` | a base-game name like `object`; a Workshop donor `<item id>\<folder>`, at most 96 characters per part, no `..` |
| `name` | at most 128 characters, no quotes; with dots it is a translation key |
| `desc` | at most 4096 characters, no quotes |
| `life` | 1 to 1000000 |
| `line` per section | at most 512, each at most 4096 characters; the first one needs a `$TOKEN` |
| `strip` | exactly one `$TOKEN` per line, at most 64 lines |
| `add`, `replace`, `remove`, `insert` per section | at most 512 each, each at most 4096 characters; the new line begins with a `$TOKEN` |
| text file read | at most 4 MiB |

A section with an error is skipped and named in the log; the other sections continue.

---

## 🔒 Safety rules

- Writing happens only under `media_soviet\workshop_wip`; no game file is modified, Steam's verification stays happy.
- Every generated folder carries `tesmioloader.stamp`. A folder without that stamp is never touched, even with the same id; the section is refused.
- `prune` deletes only folders with this plugin's stamp, never subscriptions and never folders of another generator.
- In a folder of another generator at most a missing `$OWNER_ID` is filled in - one number, not a single byte more. The stamp is left alone: Soviet Mod Loader closes the game when a folder in its id range carries none.
- Ids below 9000000000 are refused, so no real Steam number can be hit.
- Automatically assigned numbers are kept in `tesmioloader\plugins\buildings_plus.ids.ini` and are never handed out a second time, not even after a section is deleted.
- Files are written under a temporary name first and then moved into place; a crash leaves no half-written building.ini.
- The plugin checks the shape of the declaration, not the meaning of the game directives. A wrong line is reported by the game in its own log.

---

## 💾 Compatibility

### Saved games
Generated buildings are Workshop items with a fixed id. A saved game containing one needs the folder when loading; remove sections only when no saved game uses the building any more.

The plugin writes your **Steam id** into the `workshopconfig.ini` - the id of the player whose machine the building is generated on. When a saved game is loaded the game checks who owns the Workshop items it uses; with a zero in there it reports "the Workshop items used in this saved game were not found". The save still loads, but the message comes back every time. The number is read from the registry of the signed-in Steam account, otherwise from the `loginusers.vdf` of your Steam installation; if neither answers the zero stays and the next start with Steam signed in fills it in. None of this travels with the package - every copy carries the id of whoever generated it.

**Buildings of other generators too.** Soviet Mod Loader brings its own buildings component and always writes `$OWNER_ID 0`, so the message hits everyone whose buildings SML generated, and there is hardly a way to get rid of it by hand. With `repair_owner_ids = 1` (the default) Buildings Plus also looks into generated folders it did not write itself and fills in **only the missing number**; every other byte of the file stays as it was, line endings included. All four conditions have to hold: the folder name is a generated id (9000000000 to 9999999999), the folder carries a `tesmioloader.stamp`, the file names no owner at all, and your own id could be determined. A folder that already names somebody is never touched, and neither is the stamp. The timing fits: SML generates during its start-up phase, Buildings Plus runs after it - a folder just rewritten is put right in the same launch.

### Content packages
A Workshop item without a DLL of its own can bring buildings along (`[content] buildings = tesmio\buildings.ini` in its `rmm.mod.ini`). Republic Mod Manager enters them into `tesmioloader\plugins\buildings_plus.ini` with "Provide in the game", and Buildings Plus generates them at the next game start. The section format is the same.

### Other plugins
Resources in `$PRODUCTION`, `$CONSUMPTION` and `$STORAGE_*` must exist in the game (base game, Resources Plus or Resources). Deposits for mines come from Deposits Plus. Vanilla Buildings changes existing buildings, Buildings Plus adds new ones.

### Version compatibility
- **0.2.3:** bug fixed: a data line that begins with a minus sign (for example the point `-5.2 4.4` of an area or a connection) now belongs to the line above it. It used to stay behind when that line was replaced or removed. All buildings are generated once more at the next game start.
- **0.2.2:** game buildings whose model or material has another name than the building itself can be donors now (for example the gravel processing plant `gravel_processing` and the brick factory `brick_factory`). The plugin used to stop there with "no material".
- **0.2.1:** two commands on the same place are refused instead of silently passed over. An insert behind a line an earlier command writes finds it by its data lines too.
- **0.2.0:** buildings are now described with commands - add, replace, remove, insert. Each touches exactly one place of the donor file and leaves everything else alone, so a single storage can be put behind an existing one. The older way with `line` and `strip` still works. For WRSR 1.2.0.9.
- **0.1.12:** rebuilt for WRSR 1.2.0.8 and TesmioLoader b0.4.5; otherwise unchanged.
- **0.1.11:** loaded from the Workshop, the plugin now reads `tesmioloader\plugins\buildings_plus.ini` when it exists (that is where Republic Mod Manager writes), otherwise the INI next to its DLL. The assigned numbers (`buildings_plus.ids.ini`) always live in `tesmioloader\plugins` and never in the Workshop folder, which Steam overwrites with every update
- **0.1.10:** safety round: a folder stamped by another generator is no longer taken over; a section the check refuses keeps its folder even with `prune = 1` (a typo never deletes a building); a rebuild from a Workshop donor keeps the stamp, so a failed write no longer locks the folder for good; leading zeros in `id` are dropped; control characters in a `line` are refused while quotes stay allowed; at most 512 `line` and 64 `strip` keys, 4096 files and 16 folder levels per Workshop donor, 256 MB per file; a missing reference is always reported; every write error names the Windows error code
- **0.1.9:** if Soviet Mod Loader has already numbered a building with the same section name (`soviet_mod_loader\catalog.ini`, `[building_ids]`), the plugin adopts that number and writes the same folder — both load paths then generate the same building, a save finds it under either, and the build menu lists it once. An explicit `id` inside SML's range is accepted when SML's catalogue gives exactly that number to the section. An older folder of our own stays (`prune = 1` removes it).
- **0.1.8:** the missing Steam id is filled into generated folders of other generators as well — Soviet Mod Loader always leaves a zero there; switch `repair_owner_ids`, on by default
- **0.1.7:** the generated `workshopconfig.ini` carries the Steam id of the player who generated it — without it the game reports "the Workshop items used in this saved game were not found" on every load
- **0.1.6:** a `line` without a `$TOKEN` belongs to the line above it — connections, goods displays and everything else that needs data lines can now be declared
- **0.1.5:** ids between 9100000000 and 9199999999 are refused — Soviet Mod Loader reserves that range for its own buildings, and a foreign folder in it stops the game from starting
- **0.1.4:** `donor` may be a building of a subscribed Workshop item, written `<item id>\<folder>`; the clone is made on your own machine
- **0.1.3:** `name` may be a translation key; the name is then written as `$NAME` with the resolved id and the caption comes from the Localization pack
- **0.1.2:** first published version

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| building missing from the build menu | section `enabled = 0`, plugin off or an error in the section | read the log, enable the section, restart the game |
| `id must lie between ...` | id outside 9000000000 to 9999999999 | change the id |
| `exists and was not written by this plugin` | folder without stamp under workshop_wip | pick another id or check the foreign folder |
| building appears twice after renaming a section | new section name = new number, the old folder stays | switch `prune` on or delete the old folder |
| `donor "..." has no ...` | donor name wrong or not installed | name from `media_soviet\buildings_types` without `.ini` |
| game crashes on the first frame | donor with emissive material but `material_e.mtl` missing | delete the folder, restart the game; look for "emissive material" in the log |
| `name must be plain text`, `desc must be plain text` | quotes in `name` or `desc` | remove the quotes |

### Logging

Results go to `tesmioloader.log` (sender `buildings_plus`), everything else to the detail log `logs\tesmioloader.buildings_plus.log`. With `verbose = 1` it lists every copied file and every removed donor line.
- In **Republic Mod Manager** the document icon at the bottom of the plugin bar opens the log view with filter and sender.

Search for:
- `configuration:` → which INI the DLL chose and how many sections
- `-> <id>\<object> from "<donor>"` → a building was written
- `up to date` → nothing changed
- `prune:` → a folder was removed
- `ERROR:` → a section was skipped, with line and reason

---

## 📦 File structure

**Workshop item Republic Mod Manager** (Steam subscription)
```
<Workshop item>\
└── plugins\
    └── buildings_plus\
        ├── buildings_plus.dll      (plugin)
        ├── buildings_plus.ini      (original INI, example disabled)
        └── rmm.mod.ini             (manifest for Republic Mod Manager)
```

**Loader folder**
```
<game>\tesmioloader\
├── plugins\
│   ├── buildings_plus.dll          (method 2 only)
│   ├── buildings_plus.ini          (effective INI; created by hand or written by Republic Mod Manager)
│   └── buildings_plus.ids.ini      (assigned Workshop ids, written by the plugin)
├── user_config\
│   └── buildings_plus.editor.ini   (personal buildings from Republic Mod Manager)
└── logs\
    └── tesmioloader.buildings_plus.log
```

Loaded from the Workshop, the DLL stays in the Workshop item; `tesmioloader\plugins` then holds only your INI and the assigned numbers.

**Generated buildings**
```
media_soviet\workshop_wip\<id>\
├── tesmioloader.stamp              (the plugin's mark and checksum)
├── workshopconfig.ini
├── previewimage.png
├── material.mtl                    (texture paths rewritten)
├── material_e.mtl                  (only when the donor has one)
└── <object>\
    ├── building.ini                (your lines, then the donor without the replaced lines)
    ├── renderconfig.ini
    ├── model.nmf
    ├── building.bbox
    ├── building.fire
    └── imagegui.png
```

---

## 📜 Licence & Credits

**GNU GPL v3**, see `LICENSE` in the package. Buildings Plus is a fork of the `buildings` plugin from TesmioLoader by MaxLegend (GPL v3); layout and replacement rules come from there, the validation, the cleanup, the detail log and the path handling are new. The complete source code is at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/buildings_plus.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Are my game files modified?**
A: No. Only new folders under `media_soviet\workshop_wip` appear, which the game reads like unpublished Workshop items.

**Q: Can I swap the donor's model?**
A: Not through the plugin. Make a Workshop item of your own for that; Buildings Plus is for buildings that look like a donor and behave differently.

**Q: Why can the id not be smaller?**
A: Real Steam items have numbers around 3.8 billion. From 9 billion on there can never be an overlap.

**Q: Do I have to give an id?**
A: No. Without `id` the plugin assigns the next free number from 9300000000 and keeps it in `tesmioloader\plugins\buildings_plus.ids.ini`. The range deliberately sits beside the one Soviet Mod Loader uses (9100000000 upwards).

**Q: What happens when I delete a section?**
A: With `prune = 1` the generated folder disappears at the next start; otherwise it stays and the game keeps loading the building.

---

**Last update:** Buildings Plus 0.2.3  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
