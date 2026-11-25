# 🚚 Vehicle Materials 0.4.3

**TesmioLoader plugin for additional vehicle materials**

Freely configurable extra materials for vehicle production in *Workers & Resources: Soviet Republic* 1.2.0.9: glass, cables, copper or any other resource that Resources Plus or Resources registers, set separately for road, rail, ship and aircraft.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Preparing production buildings](#-preparing-production-buildings)
- [Configuration](#-configuration)
- [Value ranges](#-value-ranges)
- [Mapping vehicle types](#-mapping-vehicle-types)
- [Relation to Resources](#-relation-to-resources)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader b0.4.5 or newer (API 4)
- Mandatory: Resources Plus or Resources (Resources Plus replaces Resources) with every resource you want to use as a vehicle material. Without one of the two, Vehicle Materials does not start; the reason is in the log. Where this guide says `resources.ini`, with Resources Plus it means `resources_plus.ini`.

### In three steps
1. **Register the resources:** every material must exist in `resources.ini`, for example `glass` or `cable`. In Republic Mod Manager use the Resources Plus or Resources entry.
2. **Subscribe:** the TesmioLoader finds the plugin in the Workshop folder by itself on the next game start. Then enter the materials with their coefficients and switch the plugin on (`enabled = 1`), most comfortably through Republic Mod Manager.
3. **Adjust the production buildings:** every vehicle factory needs a storage for the new material (see [Preparing production buildings](#-preparing-production-buildings)). Then start the game.

---

## ✨ Features

### 🎯 Core function
- ✅ Additional materials for building vehicles, without changing any game file
- ✅ The game's original material requirements stay in place
- ✅ Up to 32 materials, each one registered by Resources Plus or Resources
- ✅ The configuration is checked completely at start-up; on any error the plugin stays off and the game runs as usual

### 🆕 Settings

#### 1️⃣ **Separate coefficients per vehicle class** (`[road]`, `[rail]`, `[ship]`, `[airplane]`)
- One value per material and class; `0` or a missing entry disables the material for that class
- The coefficient is multiplied with the vehicle's internal production value: bigger vehicles need more

#### 2️⃣ **Personal overlay** (`user_config\vehicle_materials.ini`)
- The DLL reads its normal INI first, then `tesmioloader\user_config\vehicle_materials.ini` key by key on top
- Republic Mod Manager writes only the overlay; the INI in the Workshop package stays untouched, so your values survive every Steam update

#### 3️⃣ **Vehicle type mapping** (`[mapping]`)
- Automatic: type 1 road, type 6 ship, type 7 aircraft, everything else rail
- Overridable by hand per internal type 0 to 15

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice. Resources Plus or Resources has to be present and switched on in every case.

---

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item
2. The TesmioLoader (b0.4.5 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. Make sure Resources Plus or Resources is switched on
   and the materials exist in resources.ini
4. Steam updates apply from the next game start
```

A newly found plugin is switched on in the loader. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager. Vehicle Materials itself ships switched off and without materials (`enabled = 0`, `count = 0`): it only does something once you have entered materials and set `enabled = 1`.

---

### Method 2️⃣: By hand into the loader folder

```
1. Copy vehicle_materials.dll and vehicle_materials.ini from plugins\
   → <game>\tesmioloader\plugins\

2. Make sure Resources Plus or Resources is switched on
   and the materials exist in resources.ini
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

The package ships a launcher schema in the `config` folder. Republic Mod Manager (version 0.6.3 and later) shows Vehicle Materials with four tabs, in German and English:

- **General:** notices, the button for this guide and Troubleshooting (detailed log)
- **Resources:** the material list; the plus button offers only resources that Resources Plus or Resources has registered and creates the coefficient for every vehicle class; the trash button removes material and coefficients again
- **Vehicle classes:** the coefficients as a table of material by class
- **Advanced mapping:** vehicle types 0 to 15

The "Plugin active" switch in the header also sets `enabled = 1` when switched on. Personal values live in `tesmioloader\user_config\vehicle_materials.ini`; the shipped INI stays unchanged. Before a start RMM checks that Resources Plus or Resources is switched on; if neither is, RMM offers to switch Vehicle Materials off for this start. If you prefer editing the INI by hand, everything you need is below.

---

## 🏗️ Preparing production buildings

⚠️ **Every new material needs its own storage line in every vehicle factory that is to build with it.** Otherwise it is neither delivered nor accepted. The plugin adds the vehicle's requirement only; it creates no storage. The factories know the transport classes **COVERED** and **OPEN** from the start; the transport class of the new line must match the resource (see the Resources editor, column transport class). Three ways are open for the storage line:

1. **Copy a building** and enter the storage lines by hand, for example with the TesmioLoader plugin Buildings.
2. **Vanilla Buildings** from the Workshop: adds the storage lines to the game's own buildings.
3. **Manual entry** in the building's `building.ini`.

In all three cases it is the same `$STORAGE_IMPORT_SPECIAL` line, one per material:

```ini
$STORAGE_IMPORT_CARPLANT RESOURCE_TRANSPORT_COVERED 250
$STORAGE_IMPORT_CARPLANT RESOURCE_TRANSPORT_OPEN 300
$STORAGE_EXPORT RESOURCE_TRANSPORT_VEHICLES 15
; new:
$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_OPEN 100 glass
$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_OPEN 50 cable
```

- `100` and `50` are the storage capacity
- `glass` and `cable` are the exact resource IDs from resources.ini
- the transport type must match the resource's transport class; in the example glass and cable are registered as OPEN

This applies to the factories for road vehicles, rail vehicles, ships and aircraft. Buildings can be adjusted through a building mod of your own or a TesmioLoader plugin. Check in the game that the new storage slots appear on the building and get supplied.

---

## ⚙️ Configuration

### Main file: `vehicle_materials.ini`

The DLL reads in this order:
- **Base:** `tesmioloader\plugins\vehicle_materials.ini` if present, otherwise the INI beside the DLL (in the Workshop package `plugins\vehicle_materials.ini`)
- **Overlay:** `tesmioloader\user_config\vehicle_materials.ini`, key by key on top; this is where Republic Mod Manager writes

⚠️ **Comments only on their own lines, starting with `;`.** A comment behind a value is read as part of the value and rejects the whole configuration. The file must be UTF-8 without BOM; all seven sections must exist, even empty ones; unknown or duplicate sections and keys, empty values and invalid numbers reject the whole configuration. No hot reload: edit the INI, restart the game.

### Section `[general]`

```ini
[general]
; 1 enables the plugin, 0 disables it; exactly 0 or 1
enabled = 0
; 1 writes materials and vehicle builds to the detail log tesmioloader.vehicle_materials.log
debug = 0
; upper limit for repeated warnings and diagnostic messages per session, 0 suppresses them
debug_limit = 80
```

### Section `[resources]` and the four vehicle classes

Example with two materials, the way Republic Mod Manager creates them:

```ini
[resources]
; number of entries resource0, resource1, ...
count = 2
; exact resource ID from resources.ini, each one only once
resource0 = glass
resource1 = cable

[road]
; coefficient per material; 0 or missing = no requirement in this class
glass = 0.020
cable = 0.010

[rail]
glass = 0.030
cable = 0.015

[ship]
glass = 0.010
cable = 0.005

[airplane]
glass = 0.015
cable = 0.020

[mapping]
; -1 = automatic, 0 = road, 1 = rail, 2 = ship, 3 = aircraft
type0 = -1
type1 = -1
type2 = -1
type3 = -1
type4 = -1
type5 = -1
type6 = -1
type7 = -1
type8 = -1
type9 = -1
type10 = -1
type11 = -1
type12 = -1
type13 = -1
type14 = -1
type15 = -1
```

Rules:
- numbering starts at `resource0`, `count` must match the number of entries, entries at or above `count` are rejected
- names are unique regardless of case and must not contain spaces, path or special characters
- a key in a vehicle class must be listed under `[resources]`
- a material without a positive value in at least one class is ignored; with `enabled = 1` Republic Mod Manager requires at least one such material
- a material a vehicle already requires is not added a second time

Start with small values and check the quantities in the game.

---

## 📏 Value ranges

The DLL checks these limits at startup. A value outside them rejects the whole configuration; the plugin then installs no hook and the game runs with its original materials.

| Key | Range | Default |
|---|---|---|
| `enabled`, `debug` | exactly 0 or 1 | 0 / 0 |
| `debug_limit` | 0 to 10000 | 80 |
| `count` | 0 to 32 | 0 |
| `resource0` … `resource31` | ID from resources.ini, at most 63 characters | none |
| coefficients in `[road]`, `[rail]`, `[ship]`, `[airplane]` | finite number 0 to 1000000 | none |
| `type0` … `type15` | -1 to 3 | -1 |
| file | at most 1 MiB, keys and values at most 63 characters | |

---

## 🔀 Mapping vehicle types

The game keeps vehicles under internal production types. The plugin maps them like this:

| Internal type | Class |
|---|---|
| 1 | road |
| 6 | ship |
| 7 | aircraft |
| all others | rail |

`[mapping]` overrides this per type 0 to 15:

| Value | Class |
|---|---|
| -1 | automatic |
| 0 | road |
| 1 | rail |
| 2 | ship |
| 3 | aircraft |

Normally the section stays unchanged. With `debug = 1` the detail log states which type was mapped to which class.

---

## 🔗 Relation to Resources

Vehicle Materials creates no resource. At startup it asks the registry service of Resources Plus or Resources and uses only names published there:

- if both are missing or switched off, Vehicle Materials does not start; the reason is in the log
- a material missing from resources.ini rejects the configuration
- the name must be spelled exactly the same in both INIs

When you remove a material from the resource list in Republic Mod Manager, it also cleans up the Vehicle Materials entries on saving, after asking.

---

## 💾 Compatibility

### Saved games

The plugin changes no game file and stores nothing in the saved game. Materials that vehicles require are part of the save's resource list: a resource already used in a saved game must not be removed from resources.ini any more.

### Version compatibility

- **0.4.3:** for WRSR 1.2.0.9 (hotfix for the rocket DLC); any other game version is refused. Otherwise unchanged.
- **0.4.2:** for WRSR 1.2.0.8 with the rocket DLC; another game version is refused. Otherwise unchanged.
- **0.4.1:** for WRSR 1.1.1.13; another game version is refused. The plugin checks two more places in the game before it starts; if one does not match, it stays off and the game runs as usual
- **0.4.0:** first published version; the configuration is read as base plus personal overlay

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| Plugin does not start | Resources Plus and Resources missing or switched off | subscribe to one of the two and switch it on in TesmioLauncher or Republic Mod Manager |
| Configuration rejected | material not in resources.ini, comment behind a value, value out of range | read the log, correct names and values |
| Vehicle does not demand the material | `enabled = 0` or coefficient 0 in this class | switch "Plugin active" on, set the coefficient |
| Factory does not accept the material | no `$STORAGE_IMPORT_SPECIAL` in the building | adjust the building (see above) |
| Wrong vehicle class | internal type differs from the expectation | `debug = 1`, set the mapping in `[mapping]` |

### Logging

Every message is in `tesmioloader.log` and in the detail log `logs\tesmioloader.vehicle_materials.log`; with `debug = 1` both also list the materials and vehicle builds.
- In **Republic Mod Manager** the log icon in the sidebar opens the log view with filter and sender.

Expected on a successful start:
```
vehicle_materials  INFO: - [status] Hook active at SOVIET64.exe+0x3F8C50
vehicle_materials  INFO: - [status] Automatic mapping: type1=road, type6=ship, type7=airplane, other=rail
vehicle_materials  INFO: - [status] v0.4.1 ready
```

Search for:
- `vehicle_materials` → every message of the plugin
- `FATAL` → rejections with cause and recommended action
- `Personal overlay applied` → the overlay from user_config was read

---

## 📦 File structure

**Workshop package** (Steam subscription)
```
<Workshop item>\
├── plugins\
│   ├── vehicle_materials.dll       (plugin)
│   └── vehicle_materials.ini       (original INI, no materials, enabled = 0)
├── config\                         (launcher schema for Republic Mod Manager)
│   ├── vehicle_materials.launcher.ini
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
│   ├── vehicle_materials.dll       (method 2 only)
│   └── vehicle_materials.ini       (method 2 only, original INI)
└── user_config\
    └── vehicle_materials.ini       (personal values from Republic Mod Manager)
```

Loaded from the Workshop, DLL and original INI stay in the package. The personal values live in `user_config` in every case.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. The plugin contains no third-party code; the loader SDK header comes from the TesmioLoader by MaxLegend (GPL v3). The complete source lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/vehicle_materials.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Can I use materials that are not in resources.ini?**
A: No. Vehicle Materials uses only resources that Resources Plus or Resources has registered.

**Q: Why does nobody deliver the material to the factory?**
A: The factory needs its own storage for it, a `$STORAGE_IMPORT_SPECIAL` line in its building.ini. The plugin does not create that storage.

**Q: Are the original materials replaced?**
A: No. Steel, mechanical components and everything else stay; the new materials are added.

**Q: What happens on an error in the INI?**
A: The whole plugin is rejected and installs no hook. The game builds vehicles as without the plugin; the reason is in the log.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager offers the materials from resources.ini with +, creates the coefficients per class and checks the value ranges.

---

**Last update:** Vehicle Materials 0.4.3  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
