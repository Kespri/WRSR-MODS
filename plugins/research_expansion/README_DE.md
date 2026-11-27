# 🔬 Research Expansion 0.4.5

**TesmioLoader-Plugin für neue Forschungen und Änderungen am Forschungsbaum**

Fügt *Workers & Resources: Soviet Republic* 1.2.0.9 eigene Forschungseinträge hinzu, hängt sie an bestehende Forschungen, positioniert ihre Freischaltungen und ändert Vanilla-Forschungen gezielt, ohne die originale `research.ini` anzufassen. Die erweiterte Datei entsteht bei jedem Spielstart im virtuellen Dateisystem (VFS) des TesmioLoaders.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Neue Forschungen](#-neue-forschungen)
- [Vanilla-Forschungen ändern](#-vanilla-forschungen-ändern)
- [Icons und Texte](#-icons-und-texte)
- [Wertebereiche](#-wertebereiche)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader ab b0.4.6 (API 4)
- **Pflicht:** das Localization-Plugin (ab 0.4.0), eingeschaltet. Ohne seinen Dienst wird die Erweiterung nicht angewendet. Republic Mod Manager prüft das vor dem Spielstart und bietet an, Research Expansion auszuschalten, wenn Localization fehlt.
- Für neue Forschungen: ein Localization-Textpaket mit den Namen und Beschreibungen (Namensraum `research_expansion`); es wird mit dem Localization-Paket ausgeliefert.

### In drei Schritten
1. **Abonnieren:** Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner. Localization muss ebenfalls abonniert und eingeschaltet sein.
2. **Forschung eintragen:** Neue Forschungsblöcke in `research_expansion.ini` schreiben (Beispiele stehen auskommentiert darin), Icons als 128 × 128 PNG unter dem Namen `<forschungs_id>.png` in den Ordner `tesmioloader\vfs\media_soviet\research` legen (Republic Mod Manager zeigt und öffnet ihn in der Karte Forschungs-Icons). Änderungen an Vanilla-Forschungen am bequemsten in Republic Mod Manager.
3. **Spiel vollständig neu starten.** Die erzeugte `research.ini` liegt danach unter `tesmioloader\vfs\media_soviet\research`.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Neue Forschungsblöcke mit Typ, Kosten, Freischaltungen, Name und Beschreibung in einer INI
- ✅ Abhängigkeiten zu Vanilla- oder neuen Forschungen per `+`-Zeile; die umgekehrten `$UNLOCK_RESEARCH`-Zeilen entstehen automatisch und lassen sich mit `@before_` und `@after_` positionieren
- ✅ Gezielte Änderungen an bestehenden Forschungen: Zeilen ersetzen, entfernen, hinzufügen, einfügen oder verschieben
- ✅ Eigene Icons je Forschung mit Ersatz-Icon
- ✅ Jeder Fehler weist die ganze Erweiterung ab und lässt die Vanilla-Forschung aktiv; das Log nennt Datei, Regel und Zeile
- ✅ Originaldateien bleiben unverändert; die erzeugte Datei liegt im VFS des Loaders

### 🆕 Neu in 0.4.2
- Nur Protokolltext: Die Startmeldung nennt jetzt die Versionsnummer des laufenden Plugins statt einer festen Zahl.

### 🆕 Neu in 0.4.1
- Nur Protokolltext: Die Meldung zu den ungenutzten Schlüsseln `icon_folder` und `noimage_name` ist klarer formuliert.

### 🆕 Neu in 0.4.0
- ✅ Fertig für Republic Mod Manager: Reiter Neue Forschungen, Localization und Vanilla-Änderungen mit „Forschung wählen…“, „Zeile wählen…“, dem Kasten „Original im Spiel“ und der Prüfung unbekannter Forschungen vor dem Speichern.
- ✅ **Neue Forschung als INI-Abschnitt:** `[research:<id>]` mit den Schlüsseln `type`, `cost`, `name`, `desc`, `requires`, `unlock` und `line` ist dieselbe Forschung wie ein `$RESEARCH`-Block, nur als Abschnitt, damit Republic Mod Manager sie bearbeiten kann. Beide Formen dürfen gemischt werden und laufen durch dieselbe Prüfung; Abschnitte werden nach den freien Blöcken eingeordnet.
- ✅ **Kurze Textschlüssel:** In `[research:<id>]` reichen `name = quartz_smasher` oder gar nichts; das Plugin macht daraus `research_expansion.quartz_smasher.name` und `.desc`. Ein Schlüssel mit Punkten gilt wörtlich.
- ✅ **Kosten ändern ohne Ersetzen-Befehl:** In `[modify:<id>]` reicht `cost = 1800`; das Plugin ersetzt die `$COST`-Zeile des Vanilla-Blocks selbst. Im Republic Mod Manager wählst du die Forschung über „Forschung wählen…“, siehst den Originalblock und baust jede Befehlszeile über „Zeile wählen…“ aus den echten Zeilen.
- ✅ **Ein Icon-Ordner:** Die Icons liegen in `tesmioloader\vfs\media_soviet\research`, dem Ordner, aus dem das Spiel sie liest. Ein Icon, das dort liegt, bleibt unangetastet; fehlt eines, erzeugt das Plugin es beim Start aus `noimage.png` (aus `research_expansion\icons` neben der DLL, sonst aus `tesmioloader\plugins\research_expansion\noimage.png`).
- ✅ **INI neben der DLL:** Fehlt `tesmioloader\plugins\research_expansion.ini`, liest die DLL die INI aus dem eigenen Ordner, also aus dem Workshop-Paket. Der gewählte Pfad steht im Log.

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden. Localization muss in jedem Fall abonniert oder installiert und eingeschaltet sein.

---

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Workshop-Objekt abonnieren, ebenso das Paket Localization
2. Der TesmioLoader (ab b0.4.6) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. INI und Ersatz-Icon findet die DLL neben sich im Paket
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager. Lies vor dem Ausschalten den Warnhinweis unter Konfiguration.

---

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere research_expansion.dll und research_expansion.ini aus plugins\
   → <Spiel>\tesmioloader\plugins\

2. Kopiere den Ordner plugins\research_expansion (das Ersatz-Icon)
   → <Spiel>\tesmioloader\plugins\research_expansion\
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

Das Paket enthält im Ordner `config` ein Editor-Schema. Republic Mod Manager (ab Version 0.6.3) zeigt Research Expansion damit in vier Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, Knopf für diese Anleitung und die Plugin-Einstellungen (Erweiterung anwenden, Forschungs-Icons, Fehlersuche)
- **Neue Forschungen:** links die Liste, rechts die gewählte Forschung mit Typ, Kosten, Vorgängern, Freischaltungen, Name und Beschreibung; gespeichert als Abschnitt `[research:<id>]`
- **Localization:** Namen und Beschreibungen deiner Forschungen je Spielsprache; das Textpaket liegt dafür als eigene Kopie unter `tesmioloader\plugins\localization\research_expansion`
- **Vanilla-Änderungen:** links die geänderten Forschungen, rechts die gewählte mit Schalter und je einem Mehrzeilenfeld für ersetzen, entfernen, hinzufügen, einfügen und verschieben; der Plus-Knopf legt eine Änderung für eine Vanilla-Forschungs-ID an

Freie Forschungsblöcke (`$RESEARCH … $RESEARCH_ADD`) werden weiterhin in der INI geschrieben; Republic Mod Manager lässt sie unverändert stehen. Persönliche Änderungen liegen in `user_config\research_expansion.editor.ini`, die wirksame Datei ist `tesmioloader\plugins\research_expansion.ini`; die INI im Paket bleibt unverändert.

---

## ⚙️ Konfiguration

### Hauptdatei: `research_expansion.ini`

Die DLL liest in dieser Reihenfolge:
- **Zuerst:** `tesmioloader\plugins\research_expansion.ini`, falls vorhanden (von Hand installiert oder die von Republic Mod Manager geschriebene wirksame INI)
- **Sonst:** die INI neben der DLL, im Workshop-Paket `plugins\research_expansion.ini`

⚠️ **Kommentare nur in eigenen Zeilen mit `;`.** UTF-8 ohne BOM, höchstens 8 MiB. Direktivennamen sind schreibungsabhängig. In `[general]` sind nur `enabled` und `debug` erlaubt (`icon_folder` und `noimage_name` werden ignoriert). Änderungen gelten nach einem vollständigen Neustart.

### Abschnitt `[general]`

```ini
[general]
; 1 erzeugt die Erweiterung, 0 entfernt beim nächsten Start die erzeugte research.ini aus dem VFS
enabled = 1
; 1 protokolliert vorbereitete Befehle, Positionierungen und lokale Pfade
debug = 0
```

⚠️ **Vor dem Abschalten der DLL** erst `enabled = 0` setzen und das Spiel einmal starten: Nur so entfernt das Plugin die erzeugte `research.ini` aus dem VFS. Wird die DLL direkt im Launcher abgeschaltet, bleibt die Datei liegen und das Spiel liest weiter die erweiterte Forschung.

---

## 🧪 Neue Forschungen

Ein Block beginnt mit `$RESEARCH <id>` und endet mit `$RESEARCH_ADD`; er steht frei in der INI, nicht in einem `[Abschnitt]`:

```ini
$RESEARCH quartz_smasher
+faculty_geology
@before_uranium_study
---------------------------------------
$TYPE_TECHNICAL
$COST 1800
$UNLOCK_BUILDING_PRODUCTION raw_quartz
$NAME research_expansion.quartz_smasher.name
$DESC research_expansion.quartz_smasher.desc
$RESEARCH_ADD
```

| Zeile | Bedeutung |
|---|---|
| `$RESEARCH <id>` | Eindeutige ID aus Buchstaben, Ziffern und `_`; bestimmt auch den Icon-Namen `<id>.png` |
| `+<forschung>` | Voraussetzung; mehrere `+`-Zeilen sind eine UND-Bedingung. Im Vorgänger entsteht automatisch `$UNLOCK_RESEARCH <id>` |
| `@before_<x>` / `@after_<x>` | Position dieser Freischaltung im Vorgänger, bezogen auf dessen Zeile `$UNLOCK_RESEARCH <x>`; gilt für die zuletzt genannte `+`-Zeile, wird nicht ins Spiel kopiert |
| `$TYPE_TECHNICAL`, `$TYPE_SOVIET`, `$TYPE_MEDICAL` | genau einer |
| `$COST <zahl>` | 1 bis 2.147.483.647 |
| `$NAME`, `$DESC` | Sprachschlüssel `namensraum.schlüssel` aus dem Localization-Textpaket; die aufgelösten IDs müssen zwischen 2.000.000 und 2.999.999 liegen |
| `$UNLOCK_…`, `$YEAR`, `$LOCK_AFTER_DAYS`, `$AVAILABLE`, `$IGNORE_…` | optionale Spieldirektiven wie in der originalen research.ini |

Regeln: Unbekannte Direktiven, fehlende Pflichtfelder, doppelte Direktiven, fehlende Vorgänger, Selbstbezüge und Kreise werden abgewiesen. `$AVAILABLE` verträgt sich nicht mit `+`-Zeilen. Eine Zeile nur aus Bindestrichen ist ein erlaubter Trenner.

### Als INI-Abschnitt

Dieselbe Forschung als Abschnitt, so schreibt sie Republic Mod Manager:

```ini
[research:quartz_smasher]
requires = faculty_geology | before | uranium_study
type = technical
cost = 1800
unlock = $UNLOCK_BUILDING_PRODUCTION raw_quartz
```

| Schlüssel | Bedeutung |
|---|---|
| `enabled` | `0` lässt den Abschnitt stehen, wendet ihn aber nicht an; fehlt der Schlüssel, gilt `1` |
| `type` | `technical`, `soviet` oder `medical` |
| `cost` | positive ganze Zahl |
| `name`, `desc` | optional: ein Wort ohne Punkt wird zu `research_expansion.<wort>.name` bzw. `.desc`, fehlt der Schlüssel, zählt die Forschungs-ID; ein Schlüssel mit Punkten gilt wörtlich |
| `requires` | eine Zeile je Vorgänger: `<forschung>`, optional `\| before` oder `\| after` und `\| <anker>`; entspricht `+`-Zeile plus `@before_`/`@after_` |
| `unlock` | eine `$UNLOCK_…`-Zeile je Eintrag |
| `line` | jede andere Direktivzeile, unverändert übernommen |

Freie Blöcke und Abschnitte dürfen gemischt werden; eine ID darf nur einmal vorkommen. Abschnitte werden nach den freien Blöcken eingeordnet.

---

## ✏️ Vanilla-Forschungen ändern

Je bestehender Forschung ein Abschnitt `[modify:<id>]`, in Republic Mod Manager ein Eintrag im Reiter Vanilla-Änderungen:

```ini
[modify:faculty_geology]
enabled = 1
replace = $COST 1500 | $COST 1800
move_before = $UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH bauxite_study
```

| Befehl | Bedeutung |
|---|---|
| `remove = ZEILE` | genau eine passende vollständige Zeile entfernen |
| `replace = ALT \| NEU` | genau eine Zeile ersetzen |
| `add = ZEILE` | unmittelbar vor `$RESEARCH_ADD` einfügen |
| `insert_before = ANKER \| ZEILE` / `insert_after = …` | vor bzw. hinter einer eindeutigen Ankerzeile einfügen |
| `move_before = ZEILE \| ANKER` / `move_after = …` | vorhandene Zeile unverändert verschieben |
| `cost = PUNKTE` | Kosten setzen: ersetzt die `$COST`-Zeile des Blocks |

Die Befehle laufen in Dateireihenfolge, nach den automatischen Freischaltungen neuer Blöcke; spätere sehen die Änderungen früherer. `$RESEARCH` und `$RESEARCH_ADD` selbst dürfen nicht angefasst werden. Eine per Befehl ergänzte `+`-Zeile ist wörtlich, ohne automatischen umgekehrten Unlock. `enabled = 0` überspringt den Abschnitt, seine Syntax muss trotzdem stimmen. Der Vergleich verlangt die vollständige Zeile samt Schreibweise; `|` ist als Trenner reserviert.

---

## 🖼️ Icons und Texte

**Icons:** PNG, genau 128 × 128 Pixel, höchstens 4 MiB, Dateiname `<forschungs_id>.png` im Ordner `tesmioloader\vfs\media_soviet\research`. Das Plugin legt den Ordner beim ersten Start an. Ein Icon, das dort liegt, bleibt; fehlt eines, wird es aus `noimage.png` erzeugt und eine Warnung protokolliert (Quelle: `research_expansion\icons` neben der DLL, sonst `tesmioloader\plugins\research_expansion\noimage.png`). Ein unbrauchbares Icon im Ordner oder ein fehlendes `noimage.png` weist die Erweiterung ab.

**Texte:** `$NAME` und `$DESC` verweisen auf ein Localization-Textpaket, etwa `tesmioloader\plugins\localization\research_expansion\sovietGerman.ini`:

```ini
[strings]
quartz_smasher.name = Quarzbrecher
quartz_smasher.desc = Zerkleinert Quarz für die Glasproduktion.\nBraucht zusätzliche Arbeiter.
```

Das Textpaket gehört zum Localization-Plugin und wird mit dessen Paket ausgeliefert. Reine Vanilla-Änderungen mit vorhandenen numerischen Text-IDs unter 2.000.000 brauchen kein Textpaket und keine Icons; der Localization-Dienst bleibt trotzdem Pflicht.

---

## 📏 Wertebereiche

| Größe | Grenze |
|---|---|
| `enabled`, `debug` | genau 0 oder 1 |
| `$COST` | 1 bis 2.147.483.647 |
| aufgelöste Text-IDs | 2.000.000 bis 2.999.999 |
| INI | höchstens 8 MiB |
| aktive Forschungsblöcke | höchstens 256 |
| Änderungsabschnitte / Befehle | höchstens 256 / 4096 |
| Zeile in einem Befehl | höchstens 4096 Bytes |
| `+`- und `$UNLOCK_RESEARCH`-Zeilen neuer Blöcke | zusammen höchstens 4096 |
| Icon | PNG, 128 × 128, höchstens 4 MiB |

---

## 💾 Kompatibilität

### Spielstände
Forschungen sind Teil des Spielstands: Eine neue Forschung, die ein Spielstand bereits kennt, sollte ihre ID behalten. Die Vanilla-Datei bleibt unverändert; ohne das Plugin liest das Spiel wieder seine eigene `research.ini`, sobald die VFS-Kopie entfernt ist (siehe `enabled = 0`).

### Andere Plugins
Localization ist Pflicht. Andere Plugins, die `research.ini` ersetzen, werden nicht zusammengeführt.

### Versionskompatibilität
- **0.4.5:** Für TesmioLoader b0.4.6 neu gebaut; sonst unverändert.
- **0.4.4:** Texte für WRSR 1.2.0.9; das Plugin nutzt keine Spieladresse und ist sonst unverändert.
- **0.4.3:** Für WRSR 1.2.0.8 und TesmioLoader b0.4.5 neu gebaut; sonst unverändert.
- **0.4.2:** nur Protokolltext; Logik und INI unverändert
- **0.4.1:** nur Protokolltext; Logik und INI unverändert
- **0.4.0:** erste veröffentlichte Fassung

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Erweiterung wird nicht angewendet | Localization fehlt oder ist aus | Localization abonnieren oder installieren und einschalten |
| `Localization key ... could not be resolved` | Schlüssel fehlt im Textpaket | Textpaket prüfen, Schlüssel exakt schreiben |
| `No icon for ... and no usable noimage.png` | Icon und Ersatz-Icon fehlen | 128 × 128 PNG als `<id>.png` nach `vfs\media_soviet\research` legen oder `noimage.png` bereitstellen |
| `Icon for ... is unusable` | PNG im VFS-Ordner kaputt oder falsche Größe | Datei ersetzen oder löschen, dann entsteht sie neu aus `noimage.png` |
| `Unknown directive` | Tippfehler oder nicht unterstützte Direktive | Zeile mit der originalen research.ini vergleichen |
| `Research ... already exists` | ID doppelt | eindeutige ID wählen |
| `Circular dependency found`, `New link creates a cycle` | Kreis in den `+`- oder `$UNLOCK_RESEARCH`-Zeilen | Abhängigkeiten entwirren |
| Forschung bleibt nach Abschalten sichtbar | DLL abgeschaltet, ohne vorher `enabled = 0` | Plugin einschalten, `enabled = 0`, Spiel einmal starten |

### Logging

Alle Meldungen stehen in `tesmioloader.log` und im Detail-Log `logs\tesmioloader.research_expansion.log`, mit Regelnamen wie `placement-target`, `modify-match`, `research-cycle`.
- In **Republic Mod Manager** öffnet das Protokoll-Symbol in der Seitenleiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `Configuration file`, `Icon store` und `Fallback icon` → welche INI, welcher Icon-Ordner und welches Ersatz-Icon gewählt wurden
- `research_expansion` → alle Meldungen des Plugins, Ablehnungen mit Regel und Zeile

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo)
```
<Workshop-Objekt>\
├── plugins\
│   ├── research_expansion.dll      (Plugin)
│   ├── research_expansion.ini      (Original-INI, Beispiele auskommentiert)
│   └── research_expansion\icons\
│       └── noimage.png             (Ersatz-Icon)
├── config\                         (Editor-Schema für Republic Mod Manager)
│   ├── research_expansion.launcher.ini
│   └── languages\
│       ├── de.ini
│       └── en.ini
├── rmm.mod.ini                     (Manifest für Republic Mod Manager)
├── workshopconfig.ini              (Steam-Workshop-Eintrag)
├── previewimage.png
├── LICENSE                         (GNU GPL v3)
├── README_DE.md
└── README_EN.md
```

**Loader-Ordner**
```
<Spiel>\tesmioloader\
├── plugins\
│   ├── localization\research_expansion\   (deine Kopie des Textpakets, sobald du sie im Republic Mod Manager anlegst)
│   ├── research_expansion.dll      (nur bei Methode 2)
│   ├── research_expansion.ini      (wirksame INI)
│   └── research_expansion\icons\noimage.png (nur bei Methode 2)
├── user_config\
│   └── research_expansion.editor.ini (persönliche Änderungen aus Republic Mod Manager)
└── vfs\media_soviet\research\      (erzeugte research.ini und die Icons <id>.png)
```

Aus dem Workshop geladen, bleiben DLL und Ersatz-Icon im Paket; in `tesmioloader\plugins` liegt dann nur die wirksame INI, sobald du im Republic Mod Manager gespeichert hast.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/research_expansion.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Wird meine research.ini verändert?**
A: Nein. Die erweiterte Datei entsteht im VFS des Loaders; das Original bleibt unangetastet.

**F: Warum sehe ich meine neue Forschung nicht?**
A: Meist fehlt Localization oder ein Sprachschlüssel, oder ein Fehler hat die ganze Erweiterung abgewiesen. Das Detail-Log nennt die Ursache.

**F: Kann ich nur Vanilla-Forschungen ändern, ohne neue anzulegen?**
A: Ja. Änderungsabschnitte funktionieren ohne neue Blöcke; nur der Localization-Dienst muss laufen.

**F: Wie werde ich die Erweiterung wieder los?**
A: Erst `enabled = 0` setzen („Erweiterung anwenden“ AUS im Republic Mod Manager), das Spiel einmal starten, dann die DLL abschalten. So räumt das Plugin das VFS auf.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Neue Forschungen, Änderungen an Vanilla-Forschungen und die Plugin-Einstellungen bietet Republic Mod Manager als Liste mit Beschreibung an. Nur freie `$RESEARCH`-Blöcke schreibst du weiter von Hand.

---

**Letzte Aktualisierung:** Research Expansion 0.4.5  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
