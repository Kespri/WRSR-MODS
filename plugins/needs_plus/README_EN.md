# 📦 Needs Plus 0.1.10

**Extension of the TesmioLoader plugin needs**

Your own citizen needs for *Workers & Resources: Soviet Republic* 1.2.0.9: your citizens want one more good, the shops stock it, all from one INI.

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
- TesmioLoader b0.4.6 or newer (API 4)
- A resource list that holds the wanted good (Resources Plus or the original resources plugin)

### In three steps
1. **Subscribe:** Needs Plus is part of the Workshop item Republic Mod Manager. The TesmioLoader finds the plugin in the Workshop folder by itself on the next game start.
2. **Switch the original off:** if you also have the needs plugin by MaxLegend, switch it off. Republic Mod Manager does that by itself on saving; in TesmioLauncher clear the checkbox of needs. Unsubscribing is not necessary.
3. **Start the game:** Needs Plus creates `tesmioloader\plugins\needs_plus.ini` and takes over your existing `tesmioloader\plugins\needs.ini` unchanged. From then on you edit the list in Republic Mod Manager under "Needs Plus", or directly in the file.

---

## ✨ Features

### 🎯 Original features (kept)
- ✅ One new need per line: `<resource> = <donor>[, <factor>[, <category>[, <chance>[, <unhappiness>]]]]`
- ✅ The need is cloned from its donor (food, meat, clothes, eletronics) and lands in exactly the shops that stock the donor
- ✅ Shop category per need: like the donor, grocery only, department store only, none
- ✅ Chance and unhappiness per need
- ✅ Up to 16 needs in the list; how many a citizen may carry at once is configurable (`max_demands`, at most 7)

### 🆕 Improvements (new in Needs Plus)

#### 1️⃣ **Its own file, taken over automatically**
Needs Plus reads `tesmioloader\plugins\needs_plus.ini`, also when the DLL comes from the Workshop. If it is missing, it is created at the first start from what you already have: from the original plugin's `tesmioloader\plugins\needs.ini`, in the same order, with a header line saying where it came from. The original is only read, never changed. If that `needs.ini` was written by Soviet Mod Loader (which rewrites it at every game start with all packages merged in), Needs Plus takes SML's baseline instead, so no package need appears twice. With neither of those, it takes the `needs.ini` next to the original's DLL in the Workshop folder, and only then the INI next to its own DLL.

#### 2️⃣ **Duplicate names are refused**
If a resource is listed twice in `[list]`, the first line counts and the second is skipped with a log line. The original would have given the citizens the need twice.

#### 3️⃣ **Steps aside by itself**
While the original is loaded or Soviet Mod Loader is running, Needs Plus stays idle and writes one line to the log. It recognises the original also when it is loaded from the Workshop folder. No citizen ever gets a need twice.

---

## 💾 Installation

Choose **one** of the two methods. The same DLL must never be loaded twice, and Needs Plus and the original needs plugin never run at the same time.

### ⚠️ Important: switch the original plugin off

If you also have the needs plugin by MaxLegend (subscribed or in `tesmioloader\plugins`):
- **Republic Mod Manager:** switches needs off by itself on saving as soon as Needs Plus is switched on
- **TesmioLauncher:** clear the checkbox of needs
- **Unsubscribing or deleting is not necessary** – while the original is loaded, Needs Plus stays idle by itself (log line `needs_plus  idle`)

**Back to the original:** switch needs on in Republic Mod Manager (Needs Plus then goes off by itself), or tick needs and clear needs_plus in TesmioLauncher. The original's DLL is never deleted or overwritten.

### Method 1️⃣: Steam Workshop (recommended)

```
1. Subscribe to the Workshop item Republic Mod Manager – Needs Plus
   sits in its folder plugins\needs_plus
2. The TesmioLoader (b0.4.6 or newer) finds the DLL in the Workshop folder
   by itself on the next game start – nothing is copied
3. At the first start the plugin creates your list:
   <game>\tesmioloader\plugins\needs_plus.ini
4. Steam updates apply from the next game start
```

A newly found plugin is switched on. You switch it off and on in TesmioLauncher or with "Plugin active" in Republic Mod Manager.

### Method 2️⃣: By hand into the loader folder

```
1. Copy needs_plus.dll from plugins\needs_plus\
   → <game>\tesmioloader\plugins\
2. Only if there is no needs.ini of the original in that folder yet:
   copy needs_plus.ini there as well
3. With a needs.ini, leave needs_plus.ini out – the plugin
   creates it from your needs.ini at the first start
```

For everyone who wants to use the plugin without the Steam subscription. A DLL in `tesmioloader\plugins` takes priority over the one of the same name from the Workshop; you then copy updates yourself.

---

## 🧰 Republic Mod Manager

- The entry **Needs Plus** edits `tesmioloader\plugins\needs_plus.ini`: list, donor, factor, category, chance, unhappiness and the plugin switches. Republic Mod Manager (version 0.6.3 and later) brings the settings page itself. If the file does not exist yet, RMM creates it when the page opens: from the original's list (also when that lies only in the Workshop folder next to the original's DLL), otherwise from the INI next to the DLL of Needs Plus. A blue notice names the source.
- The + dialog only offers resources that are in the resource list (Resources Plus or the original).
- In RMM, Needs Plus and needs are a **replacement pair**: when you switch one on, RMM switches the other one off on saving. While both are switched on, the original shows **orange** ("Being replaced"). Both entries stay in the list.
- **Content packages** from the Workshop (needs without a DLL of their own) are entered into `needs_plus.ini` by RMM with "Provide in the game" and taken out again when you switch them off.
- The **"Plugin active"** switch for Needs Plus writes the entry in `tesmioloader.ini`.
- The log window shows `logs\tesmioloader.needs_plus.log`.

---

## ⚙️ Configuration

### Main file: `needs_plus.ini`

The list always lives in `<game>\tesmioloader\plugins\needs_plus.ini`, also when the DLL comes from the Workshop. If the file is missing, the plugin creates it once at the first game start, from the first source it finds:
1. the original plugin's `tesmioloader\plugins\needs.ini`, with the same entries in the same order
2. Soviet Mod Loader's baseline, when the `needs.ini` was generated by it or is missing
3. the `needs.ini` next to the original's DLL in the Workshop folder, when the loader takes the original from there
4. the INI next to the DLL, in the Workshop item `plugins\needs_plus\needs_plus.ini`

So the INI next to the DLL is only the template for the first start. After that only the file in the loader folder counts; a Steam update does not change your list.

| Section | Content |
|---|---|
| `[list]` | One line per need: `<resource> = <donor>[, <factor>[, <category>[, <chance>[, <unhappiness>]]]]` |
| `[needs]` | How the plugin reaches the game: `enabled`, `demand`, `storage`, `max_demands`, `when_full`, `probe`, `log_seconds` |

| Field | Meaning |
|---|---|
| Donor | An existing citizen need: `food`, `meat`, `clothes` or `eletronics` (the game's spelling) |
| Factor | Amount relative to the donor, `1.0` = the same |
| Category | `auto` (like the donor), one of the game's shop classes: `basic` (grocery), `medium`, `advanced` (large department store), `mediumadvanced`, `hotel`, or `none` (only your own shops with `$STORAGE_SPECIAL`) |
| Chance | 0 to 1, how often a citizen picks the need up at all |
| Unhappiness | 0 to 1, what a citizen loses per failed shopping trip |

The format is the original plugin's; every key is explained in the file itself.

### Good to know
- **The resource has to exist.** It sits in the resource list (Resources Plus: `tesmioloader\plugins\resources_plus.ini`), and its transport class has to fit the donor's shops. Easiest: clone the resource from the same donor there.
- **Saves depend on the list.** Every need adds a storage slot to the affected shops, and the slot list is part of the save. Test changes on a copy first.

---

## 🔗 Relation to the original plugin

### Coexistence with `needs.dll`

Needs Plus and the original plugin `needs` share:
- **the same hook sites** in the game (citizen daily plan, storage slots, shop tick)
- **the same file format**, in separate files: `needs_plus.ini` and `needs.ini`, both in `tesmioloader\plugins`
- **compatibility:** saves stay valid as long as `[list]` has the same entries

### If both are installed

**Needs Plus only loads when the original is switched off.**

If the original is loaded (from the Workshop folder or from `tesmioloader\plugins`), or `tesmioloader\plugins\needs.dll` exists and is enabled in tesmioloader.ini:
- Needs Plus stays idle before it patches anything
- In the log: `needs_plus  idle - the original needs.dll is loaded`
- The original loads normally

The same applies under Soviet Mod Loader: `needs_plus  idle - soviet_mod_loader.dll brings its own needs`.

**Solution:** switch the original or SML off in TesmioLauncher or Republic Mod Manager. Deleting is not necessary.

---

## 💾 Compatibility

### Save format
Unchanged from the original: the storage slots of the shops are part of the save. A save made with the original or under SML loads with Needs Plus as long as `[list]` has the same entries. That is exactly what the automatic takeover guarantees.

### Version compatibility
- With Soviet Mod Loader: SML brings needs of its own. While it runs, this plugin pauses and says so in the log; if you want Needs Plus instead, switch `soviet_mod_loader` off in `tesmioloader.ini` (in Republic Mod Manager the "Plugin active" switch)
- 0.1.10: internal only: the search for the original's list in the Workshop folder now lives in one place for Resources Plus and Needs Plus; nothing changes in the game.
- 0.1.9: rebuilt for TesmioLoader b0.4.6; otherwise unchanged.
- 0.1.8: texts only: manual, Workshop text and settings page brought up to date; nothing changes in the game.
- 0.1.7: when a saved game loads, the empty slot for furniture, table salt and the like disappears from works that got it from an earlier version - only while it is empty and only if the building does not stock the good itself (a furniture factory keeps its furniture storage). Shops and hotels stay as they are. Needs the loader from Republic Mod Manager (b0.4.5-rmm.3 or newer).
- 0.1.6: new goods like furniture or table salt now only go to shops and hotels. Before, factories got an empty storage slot for them too, for example the airplane and rocket works one for furniture. A works that is already built kept that empty slot; since 0.1.7 it is removed when a saved game loads.
- 0.1.5: for WRSR 1.2.0.9 (hotfix for the rocket DLC); otherwise unchanged.
- 0.1.4: for WRSR 1.2.0.8 with the rocket DLC; otherwise unchanged.
- 0.1.3: the first list is now also made from the original's `needs.ini` when the original lies only in the Workshop folder. Before, the plugin looked in the loader folder only and took the shipped list otherwise
- 0.1.2: for WRSR 1.1.1.13. The plugin now also recognises the original needs when it is loaded from the Workshop folder, and then stays idle. The game function that gives shops their storage slots is checked before its first call; if it does not fit the game version, the shops are not stocked this session and the log says so
- 0.1.1: safety round, no new feature: its own log `logs\tesmioloader.needs_plus.log` for `probe`, the summary stays in `tesmioloader.log`; factor, chance and unhappiness of a line as well as `max_demands`, `when_full` and `log_seconds` are checked and set to the allowed value with a WARN line; a UTF-8 BOM in needs_plus.ini no longer hides `[list]`; more than 16 needs are reported once instead of being cut off silently; the shop slot fix switches itself off after a fault instead of repeating it
- 0.1.0: first version as a fork of needs 1.0. Same features, plus its own file with automatic takeover, the duplicate-name check and the guard against the original and SML.

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| No own needs in the game | Original or SML enabled | Read the log: `needs_plus  idle …`, then switch the other one off |
| Log says "no resource named …" | Resource missing from the resource list | Create it in Resources Plus, then restart |
| Log says "cannot be stored in transport class …" | Transport class does not fit the donor's shops | Clone the resource from the same donor in the resource list |
| A need is missing, the log says "listed twice" | Name twice in `[list]` | Remove the second line |
| Citizens want the good, shops do not have it | `storage = 0` | Set `storage = 1` or stock the shops yourself |

### Logging
- Summary in `tesmioloader.log`: the loader's line with `needs_plus` and the version `0.1.7`, at the first start `created plugins\needs_plus.ini from …`, then one line per need with donor, factor and category; invalid INI values show up there as WARN lines
- With `probe = 1` the plugin writes the citizens' demand lists and the shops' storage slots into its own log `logs\tesmioloader.needs_plus.log`

---

## 📦 File structure

**Workshop item Republic Mod Manager** (Steam subscription)
```
<Workshop item>\
└── plugins\
    └── needs_plus\
        ├── needs_plus.dll              (plugin)
        ├── needs_plus.ini              (shipped list, template for the first start)
        └── rmm.mod.ini                 (manifest for Republic Mod Manager)
```

**Loader folder**
```
<game>\tesmioloader\
├── plugins\
│   ├── needs_plus.dll              (method 2 only)
│   ├── needs_plus.ini              (your list; created at the first game start or when the RMM page first opens)
│   └── needs.ini                   (original plugin, left untouched)
└── logs\
    └── tesmioloader.needs_plus.log (detailed log, created at the first start)
```

Loaded from the Workshop, the DLL stays in the Workshop item; `tesmioloader\plugins` then holds only your list.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. Needs Plus is a fork of the `needs` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3, https://github.com/MaxLegend/TesmioLoader; hook sites and file format deliberately stay identical to the original. The complete source of Needs Plus lives at https://github.com/Kespri/WRSR-MODS/tree/main/plugins/needs_plus.

**Attention, comrade:** this plugin was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Can I use both plugins at the same time?**
A: No. Either the original OR Needs Plus. While the original is enabled, Needs Plus stays idle.

**Q: Do I have to carry my needs.ini over by hand?**
A: No. If it lies in `tesmioloader\plugins`, Needs Plus copies it to `needs_plus.ini` at the first start. If it lies only in the Workshop folder next to the original's DLL, Needs Plus takes it over from there. From then on `needs_plus.ini` is the file that counts.

**Q: What about Soviet Mod Loader?**
A: SML ships the original built in. Under SML, Needs Plus pauses. If you want it, switch SML off; Republic Mod Manager then provides content packages.

**Q: Why is my needs switched off?**
A: Republic Mod Manager switches the original off as soon as Needs Plus is switched on, so that the two do not run at once. Nothing is deleted, and one switch brings the original back.

---

**Last update:** Needs Plus 0.1.10  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
