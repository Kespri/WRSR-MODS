# 🛠️ Republic Mod Manager

[English](README_EN.md) | **Deutsch**

Einstellungen, Ein- und Ausschalten und Starten deiner TesmioLoader-Plugins in einem Fenster. Dieses Workshop-Paket bringt das Programm `rmm.exe`, Einstellungsseiten für viele Loader-Plugins, eine korrigierte `tesmioloader.dll` und die drei Plugins Resources Plus, Needs Plus und Buildings Plus mit.

---

## 📋 Inhaltsverzeichnis

1. [Schnellstart](#-schnellstart)
2. [Was drin ist](#-was-drin-ist)
3. [Installation](#-installation)
4. [Update und Entfernen](#-update-und-entfernen)
5. [Dateistruktur](#-dateistruktur)
6. [Fehlerbehandlung](#-fehlerbehandlung)
7. [Lizenz & Credits](#-lizenz--credits)
8. [FAQ](#-faq)

---

## 🚀 Schnellstart

### Voraussetzungen

- *Workers & Resources: Soviet Republic* 1.2.0.9
- **TesmioLoader** von MaxLegend ab b0.4.5, im Spielordner installiert (im Steam-Workshop abonnieren und nach seiner Anleitung einrichten). Dieses Paket bringt eine korrigierte Fassung seiner `tesmioloader.dll` mit (b0.4.5-rmm.2, gleiche API, Quelltext im GitHub-Repo) und ersetzt damit die vorhandene; Launcher und `tesmioloader.ini` bleiben die von MaxLegend
- Windows 10 oder 11 (.NET Framework 4 ist dabei)

### In drei Schritten

1. In Steam dieses Workshop-Objekt abonnieren und warten, bis der Download fertig ist.
2. Den Ordner des Objekts öffnen (`Steam\steamapps\workshop\content\784150\<Objektnummer>`) und **Install-RMM.bat** doppelklicken. Das Fenster sagt am Ende „Republic Mod Manager ist installiert“.
3. `rmm.exe` im Ordner `<Spiel>\tesmioloader` starten, oder die Desktop-Verknüpfung, wenn du sie angelegt hast.

---

## ✨ Was drin ist

### 🎯 Republic Mod Manager (`rmm.exe`)

- Eine Liste mit jedem Plugin, das der TesmioLoader findet: in deinen abonnierten Workshop-Objekten und im Ordner `tesmioloader\plugins`.
- Einstellungsseiten aus dem Schema des Pakets: Reiter, Karten, Schalter, Zahlenfelder mit Plus und Minus, Listen, Auswahlfenster für Gebäude, Forschungen und Spieltexte, alles mit Beschreibung und Bereichsprüfung.
- Schalter „Plugin aktiv“ je Plugin. Deine persönlichen Werte liegen getrennt von der ausgelieferten INI, ein Paket-Update überschreibt sie nie.
- Ersatz-Plugins: Ist ein Plugin der Ersatz für ein anderes (etwa Resources Plus für Resources), sorgt der Manager dafür, dass nie beide zugleich laufen.
- Prüfung vor dem Spielstart: Fehlt einem Plugin etwas, das es braucht, sagt der Manager das vorher.
- Profile und Wiederherstellungspunkte für die ganze Plugin-Konfiguration.
- Protokollfenster mit `tesmioloader.log` und allen Plugin-Protokollen, Probleme hervorgehoben.
- „Speichern + Starten“ startet das Spiel über `tesmiolauncher.exe`.
- Deutsch und Englisch, folgt der Windows-Sprache.

### 🔧 Korrigierte Loader-DLL (`tesmioloader.dll`)

MaxLegends TesmioLoader b0.4.5 mit Korrekturen. Für dich wichtig: Mit dieser Fassung kommen die Werte, die du im Manager einstellst, auch bei fremden Plugins an. Der Installer ersetzt die vorhandene DLL und sichert sie vorher.

### 🏭 Buildings Plus (`plugins\buildings_plus`)

Neue Gebäude aus einer Erklärung: Du nennst ein Spendergebäude des Spiels und die Zeilen, die anders sein sollen, und beim Spielstart entsteht daraus ein vollständiges Workshop-Objekt unter `media_soviet\workshop_wip`. Gebraucht für neue Ressourcen, die eine Mine oder Fabrik brauchen. Im Manager unter „Buildings Plus“ legst du Gebäude an.

### 📦 Resources Plus (`plugins\resources_plus`)

Eigene Ressourcen, als Ersatz für das Original-Plugin resources: dieselben Einträge, dieselben Spielstände, ohne den Absturz beim Beenden, den das Original ab sieben eigenen Ressourcen hat. Beim ersten Spielstart übernimmt Resources Plus deine Liste aus `tesmioloader\plugins\resources.ini` nach `resources_plus.ini` im selben Ordner, in derselben Reihenfolge. Hast du auch das Original, schaltet der Manager es aus, sobald Resources Plus eingeschaltet ist; zurück geht es mit „Plugin aktiv“ beim Original.

### 📦 Needs Plus (`plugins\needs_plus`)

Eigene Bürgerbedürfnisse, als Ersatz für das Original-Plugin needs: dieselben Einträge, dieselben Spielstände, in einer eigenen Datei. Beim ersten Spielstart übernimmt Needs Plus deine Liste aus `tesmioloader\plugins\needs.ini` nach `needs_plus.ini`. Für das Original gilt dasselbe wie bei Resources Plus.

Diese drei Plugins lädt der TesmioLoader direkt aus diesem Workshop-Objekt, nichts wird kopiert. Steam hält sie aktuell. Brauchst du eines nicht, schalte es im Manager oder im TesmioLauncher aus.

### 📄 Einstellungsseiten (`settings_schemas\`)

Deutsche und englische Einstellungsseiten für Plugins, die keine eigene mitbringen: Accumulator, Cities, Day and Night, Depletion, Deposits, Easy Start, Needs, Resources, Walking Distance, dazu die Seiten der drei Plus-Plugins.

---

## 💾 Installation

### Weg 1️⃣: Installer (empfohlen)

1. Spiel, TesmioLauncher und einen laufenden Republic Mod Manager beenden.
2. Im Ordner des Workshop-Objekts **Install-RMM.bat** doppelklicken.

Der Installer macht der Reihe nach:

- findet den Spielordner (er sucht zuerst in derselben Steam-Bibliothek wie der Workshop-Ordner, dann in deinen anderen Steam-Bibliotheken; findet er nichts, fragt er dich),
- sucht den Loader-Ordner – den Ordner mit `tesmioloader.dll`, `tesmiolauncher.exe` und `tesmioloader.ini`, normalerweise `<Spiel>\tesmioloader` – und bricht mit einem Hinweis ab, wenn er fehlt,
- kopiert `rmm.exe`, `rmm.ini`, die Anleitungen, `settings_schemas\` und die `tesmioloader.dll` aus dem Paket dorthin; ersetzte Dateien landen in `tesmioloader\rmm_install_backup\<Datum>`,
- lässt eine vorhandene `rmm.ini` unangetastet (deine Grundeinstellungen),
- räumt Reste einer älteren Installation in die Sicherung: die Workshop Bridge und alte Kopien der drei Plus-Plugins in `tesmioloader\plugins` (deine Listen `resources_plus.ini`, `needs_plus.ini` und `buildings_plus.ini` bleiben liegen),
- fragt, ob eine Desktop-Verknüpfung angelegt werden soll.

Geht etwas schief, bleibt das Fenster offen und zeigt in Rot, was fehlgeschlagen ist und warum. Läuft alles durch, schließt es sich nach fünf Sekunden von selbst.

Wenn der Spielordner nicht gefunden wird, kannst du ihn mitgeben:

```
Install-RMM.bat -GamePath "D:\Spiele\Steam\steamapps\common\SovietRepublic"
```

### Weg 2️⃣: Von Hand

Der Ordner **Manual Installation** enthält den Ordner `tesmioloader` genau so, wie er im Spielordner liegen muss.

1. Spiel, TesmioLauncher und Republic Mod Manager beenden.
2. Den Ordner `Manual Installation\tesmioloader` auf den Spielordner ziehen und das Zusammenführen bestätigen.
3. `rmm.exe` im Ordner `<Spiel>\tesmioloader` starten.

Die Datei `Manual Installation\WOHIN.txt` wiederholt diese Schritte.

---

## 🔄 Update und Entfernen

**Update:** Die drei Plus-Plugins aktualisiert Steam von selbst. Für `rmm.exe` und die Loader-DLL aktualisiert Steam nur den Ordner des Workshop-Objekts, nicht die Kopie im Spielordner: Nach einem Update einfach **Install-RMM.bat** erneut ausführen. Deine `rmm.ini`, deine Listen und alles unter `user_config` bleiben erhalten.

**Entfernen:** **Uninstall-RMM.bat** doppelklicken. Es löscht die installierten Dateien und entfernt die Desktop-Verknüpfung; das klappt auch, wenn du den TesmioLoader schon entfernt hast. Stehen bleiben `rmm.ini` (deine Einstellungen des Republic Mod Manager), deine persönlichen Werte unter `user_config` und deine Listen in `plugins`. Der TesmioLoader wird nicht angefasst, die `tesmioloader.dll` aus diesem Paket bleibt also liegen. Hat die Installation eine andere Fassung ersetzt, liegt diese in `tesmioloader\rmm_install_backup`; das Fenster nennt dir den Ordner. Die drei Plus-Plugins laden weiter, solange du dieses Objekt abonniert hast – abbestellen entfernt sie.

---

## 📦 Dateistruktur

**Workshop-Paket** (das, was du abonnierst)
```
<Objektnummer>\
├── Install-RMM.bat                 (Installer, Doppelklick)
├── Uninstall-RMM.bat               (Entfernen, Doppelklick)
├── Install-RMM.ps1                 (macht die eigentliche Arbeit)
├── Manual Installation\
│   ├── WOHIN.txt / WHERE.txt       (die Schritte von Hand)
│   └── tesmioloader\               (Zielstruktur, zum Drüberziehen)
│       ├── tesmioloader.dll        (korrigierte Loader-DLL b0.4.5-rmm.2)
│       ├── rmm.exe
│       ├── rmm.ini
│       ├── rmm.README_DE.md, rmm.README_EN.md   (Anleitung)
│       ├── rmm.SCHEMA_DE.md, rmm.SCHEMA_EN.md   (Referenz für Plugin-Autoren)
│       └── settings_schemas\
├── plugins\                        (lädt der TesmioLoader direkt von hier)
│   ├── buildings_plus\             (buildings_plus.dll, buildings_plus.ini, rmm.mod.ini)
│   ├── resources_plus\             (resources_plus.dll, resources_plus.ini, rmm.mod.ini)
│   └── needs_plus\                 (needs_plus.dll, needs_plus.ini, rmm.mod.ini)
├── workshopconfig.ini              (Steam-Workshop-Eintrag)
├── previewimage.png
├── LICENSE
├── README_DE.md
└── README_EN.md
```

**Spielordner** nach der Installation
```
SovietRepublic\tesmioloader\
├── tesmioloader.dll                (TesmioLoader b0.4.5-rmm.2, aus diesem Paket)
├── tesmiolauncher.exe, tesmioloader.ini   (TesmioLoader von MaxLegend, nicht aus diesem Paket)
├── rmm.exe, rmm.ini, rmm.README_DE/EN.md, rmm.SCHEMA_DE/EN.md
├── settings_schemas\
├── plugins\
│   ├── resources_plus.ini          (deine Ressourcenliste; beim ersten Start angelegt)
│   ├── needs_plus.ini              (deine Bedürfnisliste; beim ersten Start angelegt)
│   └── buildings_plus.ini          (deine Gebäude, sobald du im Manager speicherst)
├── user_config\                    (deine persönlichen Werte, vom Manager geschrieben)
├── logs\                           (Detail-Protokolle der Plugins)
└── rmm_install_backup\<Datum>\     (nur, wenn der Installer Dateien ersetzt hat)
```

---

## ⚙️ Fehlerbehandlung

| Meldung des Installers | Ursache | Abhilfe |
|---|---|---|
| Der TesmioLoader ist nicht installiert | Im Ordner `tesmioloader` fehlen `tesmioloader.dll`, `tesmiolauncher.exe` oder `tesmioloader.ini` | TesmioLoader von MaxLegend (ab b0.4.5) abonnieren und nach seiner Anleitung installieren, dann erneut starten |
| Der Spielordner wurde nicht gefunden | Der Installer hat das Spiel in keiner deiner Steam-Bibliotheken gefunden, oder im angegebenen Ordner liegt keine `SOVIET64.exe` | `Install-RMM.bat -GamePath "<Spielordner>"` |
| Das Spiel oder der Republic Mod Manager läuft noch | Dateien sind in Benutzung | Programme beenden, erneut starten |
| Datei konnte nicht kopiert werden | Schreibrechte oder Virenscanner | Als Administrator starten oder den Ordner im Virenscanner freigeben |
| Das Paket ist unvollständig | Steam-Download unvollständig | Objekt abbestellen und neu abonnieren |

Windows SmartScreen kann `rmm.exe` beim ersten Start als unbekanntes Programm melden, weil es nicht signiert ist. „Weitere Informationen“ und „Trotzdem ausführen“ genügt einmal.

Der Manager selbst hat ein Protokollfenster (Symbol in der Seitenleiste) mit `tesmioloader.log` und den Plugin-Protokollen aus `tesmioloader\logs`. Die Anleitung des Managers liegt als `rmm.README_DE.md` (englisch `rmm.README_EN.md`) neben `rmm.exe`.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Republic Mod Manager ist eine eigene Entwicklung; die mitgelieferte `tesmioloader.dll` ist eine korrigierte Fassung von MaxLegends TesmioLoader b0.4.5 (GPL v3), Launcher und INI bleiben sein Werk. Resources Plus, Needs Plus und Buildings Plus sind Forks seiner Plugins resources, needs und buildings (alle GPL v3). Der vollständige Quelltext von Plugins, Manager und Loader liegt unter https://github.com/Kespri/WRSR-MODS.

**Genosse, Achtung:** Dieses Programm wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Brauche ich den Soviet Mod Loader oder die Workshop Bridge?**
A: Nein. Der TesmioLoader findet abonnierte Plugins ab b0.3.7 selbst. Der Manager arbeitet nur mit ihm.

**F: Warum reicht das Abonnieren allein nicht?**
A: Für die Plugins reicht es. Nur `rmm.exe` und die Loader-DLL müssen in den Spielordner, und Steam legt Workshop-Dateien nur in seinen eigenen Ordner. Das Kopieren macht der Installer.

**F: Verliere ich meine Einstellungen bei einem Update?**
A: Nein. `rmm.ini` wird nur angelegt, wenn sie fehlt, und deine Listen in `plugins` und alles unter `user_config` fasst der Installer nie an.

**F: Wo sind die anderen Plugins?**
A: Jedes ist ein eigenes Workshop-Objekt. Abonnieren, Spiel starten, fertig; einstellen kannst du sie im Manager.

---

**Letzte Aktualisierung:** Republic Mod Manager 0.7.2  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
