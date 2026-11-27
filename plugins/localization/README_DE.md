# 🌐 Localization 0.4.3

**TesmioLoader-Plugin für eigene Texte und Übersetzungen**

Fügt *Workers & Resources: Soviet Republic* 1.2.0.9 eigene Namen, Beschreibungen und Beschriftungen hinzu, ohne die originalen Sprachdateien anzufassen. Textpakete sind einfache INI-Dateien; beim Spielstart erzeugt das Plugin daraus erweiterte `soviet*.btf`-Sprachdateien im virtuellen Dateisystem (VFS) des TesmioLoaders und stellt anderen Plugins einen Übersetzungsdienst bereit. Das Paket enthält die Textpakete von Research Expansion, Technical Service Storage und das gemeinsame Paket `tesmio_lang`.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Textpakete](#-textpakete)
- [Sprachdateien](#-sprachdateien)
- [Verwendung in anderen Plugins](#-verwendung-in-anderen-plugins)
- [Wertebereiche](#-wertebereiche)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader ab b0.4.6 (API 4) mit aktiviertem VFS
- Mindestens ein gültiges Textpaket (drei sind im Paket enthalten)

### In drei Schritten
1. **Abonnieren:** Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner.
2. **Textpakete prüfen:** Die mitgelieferten Pakete brauchen keine Einstellung. Eigene Pakete kommen als Ordner nach `tesmioloader\plugins\localization\<paketname>` mit `localization.ini` und mindestens der Fallback-Sprachdatei.
3. **Spiel vollständig neu starten.** `logs\tesmioloader.localization.log` nennt Konfigurationsdatei, Paketordner, geladene Pakete und die erzeugten Sprachdateien unter `tesmioloader\vfs\media_soviet`.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Textpakete mit Namensraum: je Plugin ein Ordner mit `localization.ini` und `soviet<Sprache>.ini`-Dateien
- ✅ Fallback-Sprache je Paket und sichtbarer Platzhalter `[MISSING TEXT: …]` für fehlende Schlüssel
- ✅ Text-IDs im reservierten Bereich 2.000.000 bis 2.999.999, bei jedem Start deterministisch neu vergeben
- ✅ Dienst `localization` für andere Plugins: Schlüssel zu Text-ID
- ✅ Strenge Prüfung von Kodierung, Abschnitten, Schlüsseln, Escape-Sequenzen und BTF-Format; ein fehlerhaftes Paket wird allein abgewiesen
- ✅ Erzeugte Dateien werden bei jedem Spielstart zuerst entfernt und mit `enabled = 0` nicht neu gebaut; die Originale unter `media_soviet` werden nie verändert

### 🆕 Neu in 0.4.0
- ✅ **INI und Textpakete neben der DLL:** Fehlt `tesmioloader\plugins\localization.ini`, liest die DLL die INI aus dem eigenen Ordner, also aus dem Workshop-Paket. Textpakete werden aus `tesmioloader\plugins\localization` **und** aus dem Ordner `localization` neben der DLL geladen. Beide Pfade stehen im Log.
- ✅ **Lokale Textpakete werden zusammengeführt statt zu ersetzen:** Ein Paketordner unter `tesmioloader\plugins\localization` legt sich Schlüssel für Schlüssel über den gleichnamigen Ordner neben der DLL. Deine eigenen Texte und Sprachen bleiben, neue Schlüssel und Sprachen aus einem Workshop-Update kommen trotzdem an; bei gleichem Schlüssel gilt dein lokaler Text. Die lokale `localization.ini` bestimmt Namensraum, Fallback und missingText.
- ✅ Die Textpakete `research_expansion`, `technical_service_storage` und `tesmio_lang` reisen im Paket mit.
- ✅ Republic Mod Manager: Reiter Allgemein (Hinweise, Anleitung, Fehlersuche) und Plugin („Erweiterung anwenden“ mit Abschaltreihenfolge, Textpakete als Liste mit Kurztext und Herkunft).

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden, und der Dienstname `localization` darf nur einmal registriert sein.

---

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Workshop-Objekt abonnieren
2. Der TesmioLoader (ab b0.4.6) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. INI und Textpakete findet die DLL neben sich im Paket
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager. Lies vor dem Ausschalten den Warnhinweis unter Konfiguration.

---

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere localization.dll und localization.ini aus plugins\
   → <Spiel>\tesmioloader\plugins\

2. Kopiere den Ordner plugins\localization (die Textpakete)
   → <Spiel>\tesmioloader\plugins\localization\
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

Das Paket enthält im Ordner `config` ein Darstellungsschema. Republic Mod Manager (ab Version 0.6.3) zeigt Localization damit in zwei Reitern, deutsch und englisch: auf **Allgemein** die Hinweise, den Knopf für diese Anleitung und die Karte Fehlersuche (Ausführliches Protokoll), auf **Plugin** den Schalter „Erweiterung anwenden“ und eine Übersicht der Textpakete. Textpakete selbst werden nicht im Manager bearbeitet; sie sind Ordner mit INI-Dateien.

Die INI im Paket ändert Republic Mod Manager nie. Deine persönlichen Werte legt er in `tesmioloader\user_config\localization.ini` ab und schreibt beim Speichern die fertige INI nach `tesmioloader\plugins\localization.ini` – dort liest das Plugin sie.

---

## ⚙️ Konfiguration

### Hauptdatei: `localization.ini`

Die DLL liest in dieser Reihenfolge:
- **Zuerst:** `tesmioloader\plugins\localization.ini`, falls vorhanden (von Hand installiert oder die fertige INI aus Republic Mod Manager)
- **Sonst:** die INI neben der DLL, im Workshop-Paket `plugins\localization.ini`

Die Hauptdatei muss **UTF-8 ohne BOM** sein. Erlaubt ist genau ein Abschnitt `[localization]` mit den Schlüsseln `enabled` und `verbose`, beide genau `0` oder `1`. Unbekannte oder doppelte Abschnitte und Schlüssel werden abgelehnt; dann bleibt das Plugin inaktiv und die Vanilla-Sprachdateien gelten.

```ini
[localization]
; 1 = Textpakete laden, Dienst bereitstellen, Sprachdateien erzeugen; 0 = erzeugte Dateien entfernen, inaktiv
enabled = 1
; 1 = technische Pfade im Detailprotokoll
verbose = 0
```

⚠️ **Vor dem Abschalten der DLL** erst `enabled = 0` setzen und das Spiel einmal starten: Nur so entfernt das Plugin die erzeugten `soviet*.btf`-Dateien aus dem VFS. Wird die DLL direkt im Launcher abgeschaltet, bleiben die erweiterten Sprachdateien liegen.

### Paketordner

Textpakete werden aus zwei Ordnern geladen:
- `tesmioloader\plugins\localization\<paketname>` (von Hand installiert, eigene Pakete)
- der Ordner `localization` neben der DLL, im Workshop-Paket `plugins\localization`

Ein Paketordner unter `tesmioloader\plugins\localization` legt sich Schlüssel für Schlüssel über den gleichnamigen Ordner neben der DLL: lokale Texte gewinnen, alles andere kommt weiter aus dem Paket, auch Sprachen und Schlüssel, die ein Workshop-Update neu bringt. So kannst du ein mitgeliefertes Textpaket lokal anpassen, ohne den Anschluss zu verlieren. Zusammen höchstens 256 Paketordner.

---

## 📦 Textpakete

Jedes Textpaket ist ein Ordner mit einer `localization.ini` und mindestens der Fallback-Sprachdatei:

```text
research_expansion\
├── localization.ini
├── sovietEnglish.ini
└── sovietGerman.ini
```

```ini
[localization]
namespace = research_expansion
fallback = sovietEnglish
missingText = [MISSING TEXT: {key}]
```

| Einstellung | Bedeutung |
|---|---|
| `namespace` | eindeutiger Name des Pakets: Kleinbuchstaben, Ziffern, `_`, `-`, bis 80 Zeichen |
| `fallback` | Sprache ohne `.ini`, die gilt, wenn die Spielsprache oder ein Text darin fehlt; die Datei muss vorhanden und gültig sein |
| `missingText` | Ersatztext, `{key}` wird durch den vollständigen Schlüssel ersetzt; ohne Angabe `[MISSING TEXT: {key}]` |

Paketkonfiguration und Sprachdateien dürfen UTF-8 mit oder ohne BOM sein. Zwei Pakete mit demselben Namensraum werden beide abgewiesen.

**Mitgelieferte Pakete**

| Paket | Namensraum | Inhalt |
|---|---|---|
| `research_expansion` | `research_expansion` | Namen und Beschreibungen neuer Forschungen von Research Expansion |
| `technical_service_storage` | `technical_service_storage` | Beschriftungen des Streuguttanks und der Depotwarnungen von Technical Service Storage |
| `tesmio_lang` | `tesmio_lang` | gemeinsame Texte, zum Beispiel `depletion.deposit_remaining` für die Vorkommen-Überschrift von Depletion |

---

## 🔤 Sprachdateien

Sprachdateien heißen wie die Sprachdateien des Spiels: `sovietEnglish.ini`, `sovietGerman.ini`, `sovietFrench.ini` und so weiter. Jede hat genau einen Abschnitt `[strings]`:

```ini
[strings]
quartz_smasher.name = Quarzbrecher
quartz_smasher.desc = Zerkleinert Quarz für die Glasproduktion.\nBraucht zusätzliche Arbeiter.
```

- Schlüssel: Kleinbuchstaben, Ziffern, `_`, `-` und einzelne Punkte, bis 160 Zeichen, je Datei nur einmal; der Namensraum wird automatisch vorangestellt (`research_expansion.quartz_smasher.name`)
- Escape-Sequenzen: nur `\n` (Zeilenumbruch) und `\\` (Backslash); jede andere macht die ganze Sprachdatei ungültig
- Ein Text darf `=` enthalten, getrennt wird am ersten Gleichheitszeichen; leere Texte sind nicht erlaubt; Kommentare nur in eigenen Zeilen mit `;` oder `#`

**Fallback:** Für jeden Text sucht das Plugin erst in der aktiven Spielsprache, dann in der Fallback-Sprache, zuletzt im Ersatztext. Ein Schlüssel, der in keiner Sprachdatei vorkommt, wird nicht registriert und liefert die ID 0.

---

## 🔌 Verwendung in anderen Plugins

Das Plugin stellt den TesmioLoader-Dienst `localization` in Version 1 bereit. Ein anderes Plugin fragt ihn in `TsmPluginStart()` ab:

```cpp
static const TsmLocalizationApi* L = nullptr;

extern "C" __declspec(dllexport) int TsmPluginStart(void)
{
    L = (const TsmLocalizationApi*)H->consume(TSM_SERVICE_LOCALIZATION, TSM_LOCALIZATION_VERSION);
    if (!L) return 1;
    int nameId = L->resolveFull("research_expansion.quartz_smasher.name");
    int descId = L->resolve("research_expansion", "quartz_smasher.desc");
    return (nameId && descId) ? 0 : 1;
}
```

Die IDs liegen zwischen 2.000.000 und 2.999.999 und passen zu `$NAME` und `$DESC` in Spieldateien. Ungültiger Namensraum, ungültiger Schlüssel oder ein Schlüssel ohne Sprachdatei liefern 0.

---

## 📏 Wertebereiche

| Größe | Grenze |
|---|---|
| `enabled`, `verbose` | genau 0 oder 1 |
| Größe je INI-Datei | 16 MiB |
| Größe je gelesener oder erzeugter BTF-Datei | 256 MiB |
| Paketordner (beide Pfade zusammen) | 256 |
| registrierte Textschlüssel | 100.000 |
| Textlänge | 65.535 UTF-16-Codeeinheiten |
| Namensraum / Schlüssel | 80 / 160 Zeichen |
| reservierte IDs | 2.000.000 bis 2.999.999 |

---

## 💾 Kompatibilität

### Spielstände
Sprachdateien sind nicht Teil des Spielstands. Texte einer Forschung oder Ressource, die ein Spielstand kennt, brauchen nach einem Update denselben Schlüssel: Plugins fragen die ID bei jedem Start über den Schlüssel neu ab. Die ID selbst kann sich ändern, sobald Schlüssel dazukommen oder wegfallen, denn IDs werden bei jedem Start in fester Reihenfolge der Pakete und Schlüssel neu vergeben.

### Andere Plugins
- **Research Expansion** braucht diesen Dienst zwingend.
- **Technical Service Storage** nutzt ihn für übersetzte Beschriftungen, läuft aber auch ohne (eingebaute Ersatztexte).
- **Depletion** ab 1.1.2 nutzt `tesmio_lang.depletion.deposit_remaining`, wenn der Dienst da ist.
- Andere Plugins, die `soviet*.btf` im VFS ersetzen, werden nicht zusammengeführt.

### Versionskompatibilität
- **0.4.3:** Für TesmioLoader b0.4.6 neu gebaut; sonst unverändert.
- **0.4.2:** Texte für WRSR 1.2.0.9; das Plugin nutzt keine Spieladresse und ist sonst unverändert.
- **0.4.1:** Für WRSR 1.2.0.8 und TesmioLoader b0.4.5 neu gebaut; sonst unverändert.
- **0.4.0:** erste veröffentlichte Fassung; Research Expansion verlangt `tesmio.localization >= 0.4.0`

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| `main-config`, `unknown-section`, `unknown-key`, `invalid-value` | Hauptdatei fehlerhaft | nur `[localization]` mit `enabled`/`verbose` als 0 oder 1 |
| `utf8-bom` | Hauptdatei mit BOM gespeichert | als UTF-8 ohne BOM speichern |
| `pack-root` | weder `tesmioloader\plugins\localization` noch ein Ordner `localization` neben der DLL | Textpakete bereitstellen |
| `Pack was rejected because its localization.ini is invalid` | Pflichtangabe fehlt oder Wert ungültig | Paketkonfiguration prüfen |
| `Fallback ... has no valid language file` | Fallback-Datei fehlt oder ist fehlerhaft | Datei anlegen oder korrigieren |
| `Invalid escape sequence` | andere Sequenz als `\n` oder `\\` | Text korrigieren |
| `namespace-collision` | zwei Pakete mit demselben Namensraum | eines umbenennen; unter `tesmioloader\plugins\localization` und neben der DLL zählt nur der Ordnername, nicht der Namensraum |
| `missing-key` | ein Plugin fragt einen Schlüssel ab, der in keiner Sprachdatei steht | Schlüssel in der Fallback-Datei anlegen |
| `id-collision` | vergebene Text-ID kollidiert mit einer vorhandenen | Log prüfen; alle erzeugten Dateien werden entfernt |
| `cleanup`, `cleanup-scan` | erzeugte VFS-Datei konnte nicht entfernt werden | Spiel und zugreifende Programme schließen |
| Texte fehlen im Spiel | Plugin nicht aktiv oder Paket abgewiesen | Log lesen: `Ready: N pack(s)` und die Fehler davor |

### Logging

Alle Meldungen stehen in `tesmioloader.log` und im Detail-Log `logs\tesmioloader.localization.log` mit Datei, Zeile, Regel und Begründung.
- In **Republic Mod Manager** öffnet das Protokoll-Symbol in der Seitenleiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `Configuration file` und `Pack folders` → welche INI und welche Paketordner gewählt wurden
- `Ready:` → Anzahl geladener Pakete und Schlüssel
- `local folder merged over the shipped one` → ein lokales Paket wurde über die Paketkopie gelegt (mit der Zahl der neuen und ersetzten Schlüssel)

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo)
```
<Workshop-Objekt>\
├── plugins\
│   ├── localization.dll            (Plugin)
│   ├── localization.ini            (Original-INI)
│   └── localization\               (Textpakete)
│       ├── research_expansion\
│       ├── technical_service_storage\
│       └── tesmio_lang\
├── config\                         (Schema für Republic Mod Manager)
│   ├── localization.launcher.ini
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
│   ├── localization.dll            (nur bei Methode 2)
│   ├── localization.ini            (Methode 2, oder die fertige INI aus Republic Mod Manager)
│   └── localization\               (eigene Textpakete; bei Methode 2 auch die mitgelieferten)
├── logs\
│   └── tesmioloader.localization.log
├── vfs\media_soviet\               (erzeugte soviet*.btf)
└── tesmioloader.log
```

Aus dem Workshop geladen, bleiben DLL, INI und die mitgelieferten Textpakete im Paket; in `tesmioloader\plugins\localization` liegen dann nur deine eigenen oder angepassten Textpakete.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/localization.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Werden die Sprachdateien des Spiels verändert?**
A: Nein. Die erweiterten Dateien entstehen im VFS des Loaders; die Originale unter `media_soviet` werden nur gelesen.

**F: Wie füge ich eigene Texte hinzu?**
A: Ordner unter `tesmioloader\plugins\localization\<name>` mit `localization.ini` (Namensraum, Fallback) und mindestens der Fallback-Sprachdatei anlegen, Schlüssel unter `[strings]`, Spiel neu starten.

**F: Kann ich ein mitgeliefertes Textpaket anpassen?**
A: Ja: den Ordner nach `tesmioloader\plugins\localization` kopieren und dort ändern. Deine Schlüssel gewinnen gegen die Paketkopie, der Rest kommt weiter aus dem Paket, und Workshop-Updates lassen deine Dateien in Ruhe.

**F: Wie werde ich die erweiterten Sprachdateien wieder los?**
A: Erst `enabled = 0` setzen („Erweiterung anwenden“ AUS im Republic Mod Manager), das Spiel einmal starten, dann die DLL abschalten.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Die beiden Schalter bietet Republic Mod Manager an; Textpakete sind Ordner mit INI-Dateien.

---

**Letzte Aktualisierung:** Localization 0.4.3  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
