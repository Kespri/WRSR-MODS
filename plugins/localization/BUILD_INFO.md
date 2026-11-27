# Localization – build notes

TesmioLoader plugin (API 4) for WRSR 1.2.0.9 (the plugin uses no game address),
GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles `localization.cpp`;
it includes `src/tesmio_plugin.h` and provides the `localization` service (key -> text id in the
reserved range 2,000,000..2,999,999). Output: extended `soviet<Language>.btf` overlays in the
loader's VFS (`<loader>\vfs\media_soviet`), removed again on a start with `enabled = 0`.
User documentation: README_DE.md / README_EN.md. History newest first.

## 0.4.5 (2026-11-27)

- Text pack technical_service_storage: material_priority.off and building_warning.off in the 18 translated languages carry that language's word for OFF (they had the English OFF, following a rule of the translator guide that was meant for the settings pages only); code unchanged apart from the version string.

## 0.4.4 (2026-11-27)

- The settings page (`config\languages`) and the three text packs (`research_expansion`, `technical_service_storage`, `tesmio_lang`: one soviet<Language>.ini per game language) in 18 more languages, translated for the user and checked (keys, placeholders, `\n`, no BOM); code unchanged apart from the version string.

## 0.4.3 (2026-11-27)

- Rebuilt with `/DTSM_RELEASE_VERSION=0.4.6` for TesmioLoader b0.4.6 (launcher 0.4.6 warns about every plugin built for 0.4.5); code unchanged apart from the version string.

## 0.4.2 (2026-11-25)

- Texts only: README, INI head and package name WRSR 1.2.0.9 (the hotfix that followed 1.2.0.8 on
  the same day). The plugin uses no game address; the code is unchanged and was rebuilt only for
  the new version number. Loaded on 1.2.0.9 in the start test of 2026-11-25 (21 language files
  extended, no warning).

## 0.4.1 (2026-11-25)

- Rebuilt for WRSR 1.2.0.8 and TesmioLoader b0.4.5 with `/DTSM_RELEASE_VERSION=0.4.5`, so the DLL
  exports `TsmPluginTargetVersion` and launcher 0.4.5 does not mark it. No change to the code.
- Checked against the new game files: `sovietEnglish.btf` has 8681 entries, the highest id is
  still 580231, so the reserved range from 2,000,000 stays free. Not run in the game yet.

## 0.4.0 (2026-09-10)

- 2026-09-11, version unchanged: the detail log `tesmioloader.localization.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before).
First published version.

- Configuration: `plugins\localization.ini` when present, otherwise the INI beside the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge). Text packs are loaded from
  the folder `localization` beside the DLL first, then from `plugins\localization`; both paths are
  logged.
- Pack folders of the same name are merged instead of shadowed: `MergePack` lays a local folder
  over the shipped one key by key (local `localization.ini` decides namespace, fallback and
  missingText; local language files add or replace keys; shipped-only languages stay). The
  fallback/text checks run in `FinishPack` after the merge, so a local folder that carries only a
  new language plus its `localization.ini` is valid. Log: `Pack '<folder>': local folder merged
  over the shipped one (<n> keys added, <m> replaced, <k> local language file(s))`; a namespace
  mismatch between the two is a WARN, the local one applies. Reason: Republic Mod Manager edits
  the research_expansion pack in `plugins\localization`; a package update by the author must still
  reach players with local edits.
- Shipped text packs: `research_expansion`, `technical_service_storage`, `tesmio_lang`.
- Package: Republic Mod Manager schema with the tabs General (notices, guides, troubleshooting) and
  Plugin ("Apply extension" with the switch-off order, text packs as a folder list with a short text
  per pack, needs Republic Mod Manager 0.4.56). The research_expansion package requires
  `tesmio.localization >= 0.4.0`.
- In-game test 2026-09-10 (user): log "Pack 'research_expansion': local folder merged over the
  shipped one (4 keys added, 4 replaced, 2 local language file(s))", 3 packs / 14 keys ready; the
  local clay_study texts showed in the game.
