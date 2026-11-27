# 🛠️ Republic Mod Manager

**English** | [Deutsch](README_DE.md)

Settings, switching on and off, and starting your TesmioLoader plugins in one window. This Workshop package brings the program `rmm.exe`, settings pages for many loader plugins, a corrected `tesmioloader.dll` and the three plugins Resources Plus, Needs Plus and Buildings Plus.

---

## 📋 Contents

1. [Quick start](#-quick-start)
2. [What is inside](#-what-is-inside)
3. [Installation](#-installation)
4. [Update and removal](#-update-and-removal)
5. [File structure](#-file-structure)
6. [Troubleshooting](#-troubleshooting)
7. [Licence & credits](#-licence--credits)
8. [FAQ](#-faq)

---

## 🚀 Quick start

### Requirements

- *Workers & Resources: Soviet Republic* 1.2.0.9
- **TesmioLoader** by MaxLegend, b0.4.5 or newer, installed in the game folder (subscribe to it in the Steam Workshop and set it up as its guide says). This package brings a corrected build of its `tesmioloader.dll` (b0.4.5-rmm.2, same API, source in the GitHub repository) and replaces the one you have; launcher and `tesmioloader.ini` stay MaxLegend's
- Windows 10 or 11 (.NET Framework 4 is included)

### In three steps

1. Subscribe to this Workshop item in Steam and wait for the download to finish.
2. Open the item's folder (`Steam\steamapps\workshop\content\784150\<item number>`) and double-click **Install-RMM.bat**. The window ends with "Republic Mod Manager is installed".
3. Start `rmm.exe` in the folder `<game>\tesmioloader`, or the desktop shortcut if you created one.

---

## ✨ What is inside

### 🎯 Republic Mod Manager (`rmm.exe`)

- One list with every plugin the TesmioLoader finds: in your subscribed Workshop items and in the folder `tesmioloader\plugins`.
- Settings pages from the package's schema: tabs, cards, switches, number fields with plus and minus, lists, pickers for buildings, research entries and game texts, all with a description and range check.
- A "Plugin active" switch per plugin. Your personal values are kept apart from the shipped INI, so a package update never overwrites them.
- Replacement plugins: when a plugin replaces another one (Resources Plus for Resources, for example), the manager makes sure the two never run at the same time.
- A check before the game starts: when a plugin lacks something it needs, the manager says so beforehand.
- Profiles and restore points for the whole plugin configuration.
- A log window with `tesmioloader.log` and every plugin log, problems highlighted.
- "Save + Start" launches the game through `tesmiolauncher.exe`.
- German and English, following the Windows language.

### 🔧 Corrected loader DLL (`tesmioloader.dll`)

MaxLegend's TesmioLoader b0.4.5 with corrections. What matters for you: with this build the values you set in the manager also reach plugins by other authors. The installer replaces the DLL you have and backs it up first.

### 🏭 Buildings Plus (`plugins\buildings_plus`)

New buildings from a declaration: you name a donor building of the game and the lines that should differ, and at game start a complete Workshop item is generated under `media_soviet\workshop_wip`. Needed for new resources that require a mine or factory. In the manager you create buildings under "Buildings Plus".

### 📦 Resources Plus (`plugins\resources_plus`)

Resources of your own, as a replacement for the original resources plugin: the same entries, the same save games, without the crash on exit the original has from seven own resources on. At the first game start Resources Plus takes over your list from `tesmioloader\plugins\resources.ini` into `resources_plus.ini` in the same folder, in the same order. If you have the original as well, the manager switches it off as soon as Resources Plus is switched on; "Plugin active" on the original takes you back.

### 📦 Needs Plus (`plugins\needs_plus`)

Citizen needs of your own, as a replacement for the original needs plugin: the same entries, the same save games, in a file of its own. At the first game start Needs Plus takes over your list from `tesmioloader\plugins\needs.ini` into `needs_plus.ini`. The original is handled as with Resources Plus.

The TesmioLoader loads these three plugins straight from this Workshop item; nothing is copied. Steam keeps them up to date. If you do not need one, switch it off in the manager or in TesmioLauncher.

### 📄 Settings pages (`settings_schemas\`)

German and English settings pages for plugins that bring none of their own: Accumulator, Cities, Day and Night, Depletion, Deposits, Easy Start, Needs, Resources, Walking Distance, plus the pages of the three Plus plugins.

---

## 💾 Installation

### Way 1️⃣: installer (recommended)

1. Close the game, TesmioLauncher and a running Republic Mod Manager.
2. In the folder of the Workshop item double-click **Install-RMM.bat**.

The installer, step by step:

- finds the game folder (it looks in the same Steam library as the Workshop folder first, then in your other Steam libraries; if it finds nothing, it asks you),
- looks for the loader folder – the folder with `tesmioloader.dll`, `tesmiolauncher.exe` and `tesmioloader.ini`, normally `<game>\tesmioloader` – and stops with a hint when it is missing,
- copies `rmm.exe`, `rmm.ini`, the guides, `settings_schemas\` and the `tesmioloader.dll` from the package there; replaced files go to `tesmioloader\rmm_install_backup\<date>`,
- leaves an existing `rmm.ini` untouched (your base settings),
- moves leftovers of an older installation into the backup: the Workshop Bridge and old copies of the three Plus plugins in `tesmioloader\plugins` (your lists `resources_plus.ini`, `needs_plus.ini` and `buildings_plus.ini` stay),
- asks whether to create a desktop shortcut.

If something goes wrong, the window stays open and shows in red what failed and why. If everything works, it closes by itself after five seconds.

If the game folder is not found, you can pass it:

```
Install-RMM.bat -GamePath "D:\Games\Steam\steamapps\common\SovietRepublic"
```

### Way 2️⃣: by hand

The folder **Manual Installation** contains the folder `tesmioloader` exactly as it has to lie in the game folder.

1. Close the game, TesmioLauncher and Republic Mod Manager.
2. Drag the folder `Manual Installation\tesmioloader` onto the game folder and confirm the merge.
3. Start `rmm.exe` in the folder `<game>\tesmioloader`.

The file `Manual Installation\WHERE.txt` repeats these steps.

---

## 🔄 Update and removal

**Update:** Steam updates the three Plus plugins by itself. For `rmm.exe` and the loader DLL Steam only updates the folder of the Workshop item, not the copy in the game folder: after an update just run **Install-RMM.bat** again. Your `rmm.ini`, your lists and everything under `user_config` are kept.

**Removal:** double-click **Uninstall-RMM.bat**. It deletes the installed files and removes the desktop shortcut; that works even after you have removed the TesmioLoader. What stays: `rmm.ini` (your settings of Republic Mod Manager), your personal values under `user_config` and your lists in `plugins`. The TesmioLoader is not touched, so the `tesmioloader.dll` from this package stays as well. If the installation replaced a different build, that one is in `tesmioloader\rmm_install_backup`; the window names the folder. The three Plus plugins keep loading while you are subscribed to this item – unsubscribing removes them.

---

## 📦 File structure

**Workshop package** (what you subscribe to)
```
<item number>\
├── Install-RMM.bat                 (installer, double-click)
├── Uninstall-RMM.bat               (removal, double-click)
├── Install-RMM.ps1                 (does the actual work)
├── Manual Installation\
│   ├── WOHIN.txt / WHERE.txt       (the steps by hand)
│   └── tesmioloader\               (target structure, to drag over)
│       ├── tesmioloader.dll        (corrected loader DLL b0.4.5-rmm.2)
│       ├── rmm.exe
│       ├── rmm.ini
│       ├── rmm.README_DE.md, rmm.README_EN.md   (guide)
│       ├── rmm.SCHEMA_DE.md, rmm.SCHEMA_EN.md   (reference for plugin authors)
│       └── settings_schemas\
├── plugins\                        (the TesmioLoader loads straight from here)
│   ├── buildings_plus\             (buildings_plus.dll, buildings_plus.ini, rmm.mod.ini)
│   ├── resources_plus\             (resources_plus.dll, resources_plus.ini, rmm.mod.ini)
│   └── needs_plus\                 (needs_plus.dll, needs_plus.ini, rmm.mod.ini)
├── workshopconfig.ini              (Steam Workshop entry)
├── previewimage.png
├── LICENSE
├── README_DE.md
└── README_EN.md
```

**Game folder** after the installation
```
SovietRepublic\tesmioloader\
├── tesmioloader.dll                (TesmioLoader b0.4.5-rmm.2, from this package)
├── tesmiolauncher.exe, tesmioloader.ini   (TesmioLoader by MaxLegend, not from this package)
├── rmm.exe, rmm.ini, rmm.README_DE/EN.md, rmm.SCHEMA_DE/EN.md
├── settings_schemas\
├── plugins\
│   ├── resources_plus.ini          (your resource list; created at the first start)
│   ├── needs_plus.ini              (your needs list; created at the first start)
│   └── buildings_plus.ini          (your buildings, once you save in the manager)
├── user_config\                    (your personal values, written by the manager)
├── logs\                           (detail logs of the plugins)
└── rmm_install_backup\<date>\      (only when the installer replaced files)
```

---

## ⚙️ Troubleshooting

| Installer message | Cause | Remedy |
|---|---|---|
| The TesmioLoader is not installed | `tesmioloader.dll`, `tesmiolauncher.exe` or `tesmioloader.ini` are missing in the folder `tesmioloader` | Subscribe to the TesmioLoader by MaxLegend (b0.4.5 or newer) and install it as its guide says, then run again |
| The game folder was not found | The installer found the game in none of your Steam libraries, or the folder you gave has no `SOVIET64.exe` | `Install-RMM.bat -GamePath "<game folder>"` |
| The game or Republic Mod Manager is still running | Files are in use | Close the programs, run again |
| File could not be copied | Write permissions or antivirus | Run as administrator or allow the folder in the antivirus |
| The package is incomplete | Incomplete Steam download | Unsubscribe and subscribe again |

Windows SmartScreen may report `rmm.exe` as an unknown program on its first start because it is not signed. "More info" and "Run anyway" is needed once.

The manager itself has a log window (icon in the sidebar) with `tesmioloader.log` and the plugin logs from `tesmioloader\logs`. The manager's guide lies next to `rmm.exe` as `rmm.README_EN.md` (German `rmm.README_DE.md`).

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. Republic Mod Manager is a development of its own; the included `tesmioloader.dll` is a corrected build of MaxLegend's TesmioLoader b0.4.5 (GPL v3), launcher and INI remain his work. Resources Plus, Needs Plus and Buildings Plus are forks of his plugins resources, needs and buildings (all GPL v3). The complete source code of plugins, manager and loader is at https://github.com/Kespri/WRSR-MODS.

**Attention, comrade:** this program was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

---

## ❓ FAQ

**Q: Do I need the Soviet Mod Loader or the Workshop Bridge?**
A: No. From b0.3.7 on the TesmioLoader finds subscribed plugins itself. The manager works with it alone.

**Q: Why is subscribing alone not enough?**
A: For the plugins it is. Only `rmm.exe` and the loader DLL have to go into the game folder, and Steam puts Workshop files only into its own folder. The installer does the copying.

**Q: Do I lose my settings on an update?**
A: No. `rmm.ini` is only created when it is missing, and the installer never touches your lists in `plugins` or anything under `user_config`.

**Q: Where are the other plugins?**
A: Each one is a Workshop item of its own. Subscribe, start the game, done; you can configure them in the manager.

---

**Last update:** Republic Mod Manager 0.8.3  
**For:** WRSR 1.2.0.9 | TesmioLoader API 4
