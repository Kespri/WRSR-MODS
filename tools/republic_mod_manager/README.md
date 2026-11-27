# Republic Mod Manager – source

Windows program (C#, WinForms, .NET Framework 4) that lists the plugin DLLs the TesmioLoader
finds (in its `plugins` folder and in subscribed Workshop items), renders their settings from
the package schema, switches plugins on and off, checks the start and starts the game through
the TesmioLauncher.
The user guide is `README_EN.md` / `README_DE.md`, the reference for package authors
`SCHEMA_EN.md` / `SCHEMA_DE.md`.
Distributed as a Steam Workshop item; the item's installer and guides are in `workshop/`.

## Layout

| Folder / file | Content |
|---|---|
| `src/*.cs` | application sources (`App.cs` is the entry point) |
| `tests/CoreTests.cs`, `tests/UiTests.cs` | offline tests, run against the fixture package in `packages/` |
| `tests/BuildIcon.cs` | builds `assets/rmm.ico` from `assets/rmm-icon.png` during the build |
| `languages/de.ini`, `languages/en.ini` | user interface strings, embedded into the executable |
| `languages/<code>.ini` | the user interface in 18 more languages, installed beside the executable as `languages\<code>.ini` |
| `settings_schemas/` | settings pages for plugins that bring no package schema of their own (texts in `settings_schemas/languages/<code>.ini`, 20 languages; EN fallbacks in the `.launcher.ini`), and `replacements.ini`, the table of replacement pairs |
| `packages/fixture_vehicle_materials/` | test fixture: a Vehicle Materials package without its DLL |
| `workshop/` | the Steam Workshop item: `Install-RMM.bat`, `Uninstall-RMM.bat`, `Install-RMM.ps1`, guides and `WOHIN.txt` / `WHERE.txt` |

## Building

Windows with .NET Framework 4 (the compiler `csc.exe` ships with it), then:

```
build.bat
```

Output in `bin\`: `rmm.exe`, `CoreTests.exe`, `UiTests.exe`, plus a copy of `settings_schemas\`.
The build uses `/warnaserror+`, so every warning stops it.

## Tests

```
bin\CoreTests.exe packages\fixture_vehicle_materials tests\runs\core_<stamp>
bin\UiTests.exe packages\fixture_vehicle_materials tests\runs\ui_<stamp>
```

The output folder must not exist yet. Some core tests expect the fixture's DLL beside its INI
(`packages\fixture_vehicle_materials\hooks\vehicle_materials.dll`); build the Vehicle Materials plugin from
`plugins/vehicle_materials` and copy the DLL there before running them.

## Installing into the game

Copy `bin\rmm.exe`, `settings_schemas\`, the two guides (as `rmm.README_DE.md` / `rmm.README_EN.md`)
and the two references (as `rmm.SCHEMA_DE.md` / `rmm.SCHEMA_EN.md`) into `<game>\tesmioloader`, or use the Workshop
item's `Install-RMM.bat`, which also installs the corrected loader DLL built from `src/tesmioloader.cpp`.

## Licence

GNU GPL v3, see the repository `LICENSE`.
