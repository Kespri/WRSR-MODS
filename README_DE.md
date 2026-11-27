# Workers & Resources: Soviet Republic – Plugins für den TesmioLoader

[English](README.md) | **Deutsch**

## 🤖 Genosse, Achtung: hier hat eine KI mitgebaut

Diese Plugins wurden mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne
dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht.
Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

## Worum es geht

Native Plugins für *Workers & Resources: Soviet Republic* 1.2.0.9, geladen vom
[TesmioLoader](https://github.com/MaxLegend/TesmioLoader) (API 4). Der Loader findet Plugin-DLLs
selbst, in seinem eigenen Ordner `plugins` und in abonnierten Steam-Workshop-Objekten; der Republic
Mod Manager listet, was der Loader findet, bearbeitet die Einstellungen und prüft den Start. Jedes
Plugin ist als Steam-Workshop-Paket mit einem Einstellungsschema für den Republic Mod Manager
gepackt. Dieses Repository enthält den Quelltext, wie es die GNU GPL v3 verlangt; fertige DLLs,
Texturen und Modelle liegen hier nicht - die kommen mit den Workshop-Paketen.

## Plugins

| Plugin | Version | Was es macht |
|---|---|---|
| [Deposits Plus](plugins/deposits_plus) | 0.4.21 | Neue Vorkommenstypen, natürliche Verteilung, sandige Wiese, Arbeitsfahrzeuge |
| [Localization](plugins/localization) | 0.4.5 | Übersetzungsdienst: Textpakete werden zu erweiterten Sprachdateien |
| [Research Expansion](plugins/research_expansion) | 0.4.6 | Neue Forschungen und Änderungen am Forschungsbaum |
| [Resources Button Fix](plugins/resources_button_fix) | 0.4.8 | Kompakte Werkzeugraster im Geländeeditor |
| [Technical Service Storage](plugins/technical_service_storage) | 0.3.12 | Streugutlager, Materialprioritäten und Schneepflugtanks |
| [UI Layout Fixes](plugins/ui_layout_fixes) | 0.3.6 | Zeilenabstand im Zollhaus, Textumbruch in Info-Fenstern, Liste fehlender Ressourcen in Gebäudefenstern |
| [Vanilla Buildings](plugins/vanilla_buildings) | 0.4.21 | Vorübergehende Änderungen an Gebäudedateien, ohne die Originale anzufassen |
| [Vehicle Materials](plugins/vehicle_materials) | 0.4.17 | Zusätzliche Materialien für die Fahrzeugproduktion |
| [Weather Roads](plugins/weather_roads) | 0.3.9 | Straßenschnee, Schmelze und Schutz nach dem Räumen |

Diese drei reisen im Paket des Republic Mod Manager mit:

| Plugin | Version | Was es macht |
|---|---|---|
| [Buildings Plus](plugins/buildings_plus) | 0.2.11 | Neue Gebäude aus einer Erklärung: Spendergebäude plus Befehle, die Zeilen anhängen, ersetzen, entfernen oder einfügen, beim Spielstart erzeugt |
| [Resources Plus](plugins/resources_plus) | 0.1.11 | Eigene Ressourcen aus `plugins\resources_plus.ini`, Fork von resources: behebt den Absturz beim Beenden, übernimmt einmal die Liste des Originals, Namen je Spielsprache, pausiert, solange das Original geladen ist |
| [Needs Plus](plugins/needs_plus) | 0.1.10 | Eigene Bürgerbedürfnisse aus `plugins\needs_plus.ini`, Fork von needs: übernimmt einmal die Liste des Originals, doppelte Namen abgewiesen, pausiert, solange das Original geladen ist |

Jeder Plugin-Ordner hat eine eigene Anleitung in zwei Sprachen (`README_DE.md`, `README_EN.md`)
und englische Build-Notizen (`BUILD_INFO.md`). Gemeinsame Dateien: `plugins/grit_spreader_api.h`
(Dienst zwischen Technical Service Storage und Weather Roads), `plugins/tesmio_config.h`
(INI-Basis und persönliches Overlay) und `plugins/storage_retrofit.h` (Lager schon gebauter Gebäude,
gemeinsam für Vanilla Buildings, Buildings Plus und Vehicle Materials). `src/` enthält die vier SDK-Header `tesmio_api.h`,
`tesmio_plugin.h`, `tesmio_release.h` und `tesmio_discovery.h` aus dem TesmioLoader von MaxLegend (GPL v3), damit die
Plugins und der Loader mit ihren relativen Include-Pfaden aus diesem Repository heraus gebaut
werden können. `tesmio_plugin.h` trägt eine lokale Änderung: `TsmOpenLog` schreibt die
Detail-Protokolle der Plugins nach `tesmioloader\logs\`. `src/tesmioloader.cpp` ist der Loader
selbst in der korrigierten Fassung b0.4.6-rmm.1, die das Paket des Republic Mod Manager mitbringt
und über die Original-DLL installiert (gleiche API, gleiche Logzeilen; die Änderungen stehen in
`src/BUILD_INFO.md`).

## Werkzeuge

| Werkzeug | Version | Was es macht |
|---|---|---|
| [Republic Mod Manager](tools/republic_mod_manager) | 0.8.18 | Windows-Programm: listet die Plugins, die der Loader findet, bearbeitet ihre Einstellungen, schaltet sie ein und aus und prüft den Start; sein Workshop-Objekt bringt den Installer, den Loader und die drei Plugins oben mit |

## Installation

Workshop-Paket abonnieren; der TesmioLoader findet das Plugin beim nächsten Spielstart von
selbst. Einstellungen und Schalter stehen im Republic Mod Manager. Die Plugin-Anleitungen
beschreiben auch den Weg von Hand: DLL und INI nach `tesmioloader\plugins`.

## Bauen

Visual Studio 2022 oder neuer mit den x64-C++-Werkzeugen, dann in einer Developer Command Prompt
im jeweiligen Plugin-Ordner:

```
cl /nologo /O2 /MT /W3 /EHsc /std:c++17 /LD /Fo"build\\" /Fd"build\\" /Fe"build\<name>.dll" <name>.cpp /link kernel32.lib
```

Der Republic Mod Manager baut mit
`tools/republic_mod_manager/build.bat` (C#-Compiler des .NET Framework 4). Die Exporte
`TsmPluginApiVersion`, `TsmPluginInit` und `TsmPluginStart` sind der Plugin-Vertrag des Loaders
(API 4). Adressen und Signaturen gelten nur für Spielversion 1.2.0.9 (PE-Stempel 0x6B05A574).

## Fehlerberichte

Bei Problemen bitte ein Issue mit Spielversion, Loader-Version, aktiven Plugins und den
Protokollen `tesmioloader.log` aus `tesmioloader` und `tesmioloader.<plugin>.log` aus `tesmioloader\logs` eröffnen.

## Credits und Lizenz

GNU GPL v3, siehe [LICENSE](LICENSE). Deposits Plus, Buildings Plus, Resources Plus und Needs Plus
führen Plugins aus dem TesmioLoader von MaxLegend (Tesmio) weiter, und `src/tesmioloader.cpp` ist
sein Loader mit Korrekturen. `plugins/deposits_plus/third_party` enthält Code von Microsoft unter der
University of Illinois Open Source License (siehe `LICENSE.TXT` dort).
