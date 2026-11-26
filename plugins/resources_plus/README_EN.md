# 📦 Resources Plus 0.1.8

**Extension of the TesmioLoader plugin resources**

Your own resources for *Workers & Resources: Soviet Republic* 1.2.0.9: new goods with an icon, cargo models, a price and trade at the customs house, all from one INI.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Relation to the original plugin](#-relation-to-the-original-plugin)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)
- [Licence & credits](#-licence--credits)
- [FAQ](#-faq)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader b0.4.5 or newer (API 4)

### In three steps
1. **Subscribe:** Resources Plus is part of the Workshop item Republic Mod Manager. The TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Switch the original off:** if you also have the resources plugin by MaxLegend, switch it off. Republic Mod Manager does that by itself on saving; in TesmioLauncher clear the checkbox of resources. Unsubscribing is not necessary.
3. **Start the game:** Resources Plus creates `tesmioloader\plugins\resources_plus.ini` and takes over your existing `tesmioloader\plugins\resources.ini` unchanged. From then on you edit the list in Republic Mod Manager under "Resources Plus", or directly in the file.

---

## ✨ Features

### 🎯 Original features (kept)
- ✅ New resources as a copy of a template (`= steel, Cable`) or fully your own (`= custom` plus `[custom:<name>]`)
- ✅ Icon, cargo models, transport class, price kind and market values per resource
- ✅ Prices from the production chain, `[base_price]` and `[price]` to override them
- ✅ Customs houses pick up new resources as trade slots, even ones built earlier
- ✅ Up to 256 entries; the plugin grows the game's resource table itself

### 🆕 Improvements (new in Resources Plus)

#### 1️⃣ **No more crash on exit**
The original crashed the game on exit as soon as more than six own resources were declared (Windows reported `ucrtbase.dll`, code `c0000409`). Saves were never affected, but every exit left a crash report behind. Resources Plus allocates the enlarged table the way the game releases it on exit.

#### 2️⃣ **Its own file, taken over automatically**
Resources Plus reads `tesmioloader\plugins\resources_plus.ini`, also when the DLL comes from the Workshop. If it is missing, it is created at the first start from what you already have: from the original plugin's `tesmioloader\plugins\resources.ini`, in the same order, with a header line saying where it came from. The original is only read, never changed. If that `resources.ini` was written by Soviet Mod Loader (which rewrites it at every game start with all packages merged in), Resources Plus takes SML's baseline instead, so no package resource appears twice. With neither a `resources.ini` nor SML's baseline, the plugin takes the INI next to its DLL.

#### 3️⃣ **Duplicate names are refused**
If a name is listed twice in `[list]`, the first one counts and the second is skipped with a log line. The original would have registered it twice.

#### 4️⃣ **Steps aside by itself**
While the original is loaded or Soviet Mod Loader is running, Resources Plus stays idle and writes one line to the log. It recognises the original also when it is loaded from the Workshop folder. There are never two plugins registering the same resource.

#### 5️⃣ **Names in the game language**
A section `[names:German]` gives a resource a name of its own for the German game, `[names:French]` for the French one, and so on. A German game then shows "Kabel" instead of "Cable": in the customs house, in storages, on vehicles and on the brush of Deposits Plus. The plugin learns the running language from the game itself at start.

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice, and Resources Plus and the original resources plugin never run at the same time.

### ⚠️ Important: switch the original plugin off

If you also have the resources plugin by MaxLegend (subscribed or in `tesmioloader\plugins`):
- **Republic Mod Manager:** switches resources off by itself on saving as soon as Resources Plus is switched on
- **TesmioLauncher:** clear the checkbox of resources
- **Unsubscribing or deleting is not necessary** – while the original is loaded, Resources Plus stays idle by itself (log line `resources_plus  idle`)

**Back to the original:** switch resources on in Republic Mod Manager (Resources Plus then goes off by itself), or tick resources and clear resources_plus in TesmioLauncher. The original's DLL is never deleted or overwritten.

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item Republic Mod Manager – Resources Plus
   sits in its folder plugins\resources_plus
2. The TesmioLoader (b0.4.5 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. At the first start the plugin creates your list:
   <game>\tesmioloader\plugins\resources_plus.ini
4. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

### Method 2️⃣: By hand into the loader folder

```
1. Copy resources_plus.dll from plugins\resources_plus\
   → <game>\tesmioloader\plugins\
2. Only if there is no resources.ini of the original in that folder yet:
   copy resources_plus.ini there as well
3. With a resources.ini, leave resources_plus.ini out – the plugin
   creates it from your resources.ini at the first start
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

- The entry **Resources Plus** edits `tesmioloader\plugins\resources_plus.ini`: list, template, display name, transport class, price kind, market values. Republic Mod Manager (version 0.6.3 and later) brings the settings page itself. If the file does not exist yet, RMM creates it when the page opens: from the original's list (also when that lies only in the Workshop folder next to the original's DLL), otherwise from the INI next to the DLL of Resources Plus. A blue notice names the source.
- In RMM, Resources Plus and resources are a **replacement pair**: when you switch one on, RMM switches the other one off on saving. While both are switched on, the original shows **orange** ("Being replaced"). Both entries stay in the list.
- **Content packages** from the Workshop (resources without a DLL of their own) are entered into `resources_plus.ini` by RMM with "Provide in the game" and taken out again when you switch them off.
- The **"Plugin active"** switch for Resources Plus writes the entry in `tesmioloader.ini`.
- The log window shows `logs\tesmioloader.resources_plus.log`.

---

## ⚙️ Configuration

### Main file: `resources_plus.ini`

The list always lives in `<game>\tesmioloader\plugins\resources_plus.ini`, also when the DLL comes from the Workshop. If the file is missing, the plugin creates it once at the first game start, from the first source it finds:
1. the original plugin's `tesmioloader\plugins\resources.ini`, with the same entries in the same order
2. Soviet Mod Loader's baseline, when the `resources.ini` was generated by it or is missing
3. the `resources.ini` next to the original's DLL in the Workshop folder, when the loader takes the original from there
4. the INI next to the DLL, in the Workshop item `plugins\resources_plus\resources_plus.ini`

So the INI next to the DLL is only the template for the first start. After that only the file in the loader folder counts; a Steam update does not change your list.

| Section | Content |
|---|---|
| `[list]` | One line per resource: `<name> = <template or custom>[, <display name>]` |
| `[names:<Language>]` | Names for one game language: `<name> = <display name>`. `<Language>` is written as in the game's file name `soviet<Language>.btf`, for example `German` |
| `[custom:<name>]` | Fine-tuning: `transport`, `kind`, `cargo`, `price`, `market_rub`, `market_usd` and more |
| `[base_price]` / `[price]` | Override prices before or after the game's price pass |
| `[resources]` | How the plugin reaches the game: `hook`, `resource_capacity`, `price_hook`, `price_report`, `custom_report` |
| `[customs]` | Customs house hook: `hook`, `probe` |

The format is the original plugin's; every key is explained in the file itself. The template decides transport class and cargo shape, so: `open` goods via steel, silo goods via alumina, bulk goods via rawgravel or gravel, food via food.

### Good to know
- **Saves depend on `[list]`.** A save with two own resources does not load without them, and a save without own resources does not load with them. Change the list only with a matching save or a new game. The takeover from `resources.ini` keeps the order, so your saves keep loading.
- **Names per language:** the display name in `[list]` applies to every language that has no section of its own, so keep it English. German names go under `[names:German]`. The shipped list and the content packages already bring their German names. If you already have a `resources_plus.ini` of your own, the plugin also reads the names of the shipped resources from the INI beside its DLL; a name that is in your file as well wins. For resources of your own you enter the names in Republic Mod Manager (0.6.6 or newer, fields "Names per game language" at the very end of the resource's properties) or by hand in `resources_plus.ini`. Names apply from the next game start; saves do not depend on them.
- Icon: `media_soviet\resources\<name>.png` (48×48, RGBA), best placed under `tesmioloader\vfs\`.
- Cargo models: `resources\<name>.nmf` for open goods, `<name>1..4.nmf` plus `<name>_vehicle.nmf` for bulk goods.

---

## 🔗 Relation to the original plugin

### Coexistence with `resources.dll`

Resources Plus and the original plugin `resources` share:
- **the same service name:** `resources`
- **the same file format**, in separate files: `resources_plus.ini` and `resources.ini`, both in `tesmioloader\plugins`
- **the same text ids** for the display names (from 1000000)
- **compatibility:** saves stay valid as long as `[list]` has the same entries in the same order

### If both are installed

**Resources Plus only loads when the original is switched off.**

If the original is loaded (from the Workshop folder or from `tesmioloader\plugins`), or `tesmioloader\plugins\resources.dll` exists and is enabled in tesmioloader.ini:
- Resources Plus stays idle before it patches anything
- In the log: `resources_plus  idle - the original resources.dll is loaded`
- The original loads normally

The same applies under Soviet Mod Loader: `resources_plus  idle - soviet_mod_loader.dll brings its own resources`.

**Solution:** switch the original or SML off in TesmioLauncher or Republic Mod Manager. Deleting is not necessary.

---

## 💾 Compatibility

### Save format
Unchanged from the original: the number of resources is part of the save, the display names get the same text ids. A save made with the original or under SML loads with Resources Plus as long as `[list]` has the same entries in the same order. That is exactly what the automatic takeover guarantees.

### Version compatibility
- With Soviet Mod Loader: SML brings resources of its own. While it runs, this plugin pauses and says so in the log; if you want Resources Plus instead, switch `soviet_mod_loader` off in `tesmioloader.ini` (in Republic Mod Manager the "Plugin active" switch)
- 0.1.8: bug fixed: a fixed price from `[price]` did not reach the customhouse or the trade window, because the game works the price out once more afterwards. It is now set behind that second calculation as well. The log shows roubles and dollars in the right column now.
- 0.1.7: for WRSR 1.2.0.9 (hotfix for the rocket DLC); otherwise unchanged.
- 0.1.6: for WRSR 1.2.0.8 with the rocket DLC. The plugin knows the game's four new resources (`kerosene`, `lox`, `udmh`, `n2o4`) and the two new transport classes `cryo` and `hypergolic`. Old address lines in your `resources_plus.ini` do no harm, the plugin then uses its own.
- 0.1.5: the first list is now also made from the original's `resources.ini` when the original lies only in the Workshop folder. Before, the plugin looked in the loader folder only and took the shipped list otherwise
- 0.1.4: names in the game language: new sections `[names:<Language>]` in the list; the shipped list brings German names. Without such sections nothing changes
- 0.1.3: for WRSR 1.1.1.13. The plugin now also recognises the original resources when it is loaded from the Workshop folder, and then stays idle. If your `resources_plus.ini` still carries a game address of the previous game version (`resource_rva`, `price_pass_rva`, `customhouse_tick_rva`), the plugin skips it with a log line and uses its built-in one
- 0.1.2: safety round, no new feature: the customs house sync runs under the same lock as the move of the resource table and checks the game's push function for its bytes before the first call; transport classes beyond the record's 18 are no longer read; registering resources and the price bracket switch themselves off after a fault instead of repeating it; `packed = auto` does what the comment says again; a UTF-8 BOM in resources_plus.ini no longer hides `[list]`; `resource_capacity`, `resource_vector_rva`, `packed` and a numeric `family` are checked and refused with a WARN line; write errors name the Windows error code
- 0.1.1: the guard against Soviet Mod Loader also works when Resources Plus loads before SML (alphabetical order in `plugins`): `plugins\soviet_mod_loader.dll` present and switched on is enough. Without this fix SML blocked the game start with "resources registration hook was not installed".
- 0.1.0: first version as a fork of resources 1.7. Same features, plus the exit fix, its own file with automatic takeover, the duplicate-name check and the guard against the original and SML.

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| No own resources in the game | Original or SML enabled | Read the log: `resources_plus  idle …`, then switch the other one off |
| "Save needs mods" when loading | `[list]` does not match the save | Declare the same entries in the same order |
| A resource is missing, the log says "listed twice" | Name twice in `[list]` | Remove the second line |
| Storage shows 0.00 of 0.00 t | Template's transport class does not match the building's `$STORAGE` | Pick another template or set `transport =` in `[custom:<name>]` |
| A customs house that already stood does not trade the new resource | `hook = 0` in `[customs]` | Set `hook = 1`, or demolish and rebuild the customs house |
| No icon | PNG missing | `media_soviet\resources\<name>.png`, 48×48 RGBA |

### Logging
- Summary in `tesmioloader.log`: the loader's line with `resources_plus` and the version `0.1.8`, at the first start `created plugins\resources_plus.ini from …`, then `published as index …` per resource; with `price_report = 1` the price table as well, with `custom_report = 1` the records
- Details in `logs\tesmioloader.resources_plus.log`: every resource name the game asked for

---

## 📦 File structure

**Workshop item Republic Mod Manager** (Steam subscription)
```
<Workshop item>\
└── plugins\
    └── resources_plus\
        ├── resources_plus.dll          (plugin)
        ├── resources_plus.ini          (shipped list, template for the first start)
        └── rmm.mod.ini                 (manifest for Republic Mod Manager)
```

**Loader folder**
```
<game>\tesmioloader\
├── plugins\
│   ├── resources_plus.dll              (method 2 only)
│   ├── resources_plus.ini              (your list; created at the first game start or when the RMM page first opens)
│   └── resources.ini                   (original plugin, left untouched)
└── logs\
    └── tesmioloader.resources_plus.log
```

Loaded from the Workshop, the DLL stays in the Workshop item; `tesmioloader\plugins` then holds only your list.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. Resources Plus is a fork of the `resources` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3, https://github.com/MaxLegend/TesmioLoader; service name, text ids and file format deliberately stay identical to the original. The complete source of Resources Plus lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/resources_plus.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Can I use both plugins at the same time?**
A: No. Either the original OR Resources Plus. While the original is enabled, Resources Plus stays idle.

**Q: Do I have to carry my resources.ini over by hand?**
A: No. If it lies in `tesmioloader\plugins`, Resources Plus copies it to `resources_plus.ini` at the first start, in the same order. If it lies only in the Workshop folder next to the original's DLL, Resources Plus takes it over from there. From then on `resources_plus.ini` is the file that counts.

**Q: What about Soviet Mod Loader?**
A: SML ships the original built in. Under SML, Resources Plus pauses and the exit fix does not apply. If you want it, switch SML off; Republic Mod Manager then provides content packages.

**Q: Why is my resources switched off?**
A: Republic Mod Manager switches the original off as soon as Resources Plus is switched on. That way the fixed version runs and not both at once. Nothing is deleted, and one switch brings the original back.

---

**Last update:** Resources Plus 0.1.8  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
