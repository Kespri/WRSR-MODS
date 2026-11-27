# 📦 Needs Plus 0.1.9

**Erweiterung des TesmioLoader-Plugins needs**

Eigene Bedürfnisse für *Workers & Resources: Soviet Republic* 1.2.0.9: Deine Bürger wollen eine weitere Ware, die Läden führen sie, alles aus einer INI heraus.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Verhältnis zum Original-Plugin](#-verhältnis-zum-original-plugin)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)
- [Lizenz & Credits](#-lizenz--credits)
- [FAQ](#-faq)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader ab b0.4.6 (API 4)
- Eine Ressourcenliste, in der die gewünschte Ware steht (Resources Plus oder das Original-Plugin resources)

### In drei Schritten
1. **Abonnieren:** Needs Plus steckt im Workshop-Objekt Republic Mod Manager. Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner.
2. **Original ausschalten:** Hast du auch das Plugin needs von MaxLegend, schalte es aus. Republic Mod Manager macht das beim Speichern von selbst; im TesmioLauncher entfernst du das Häkchen bei needs. Abbestellen ist nicht nötig.
3. **Spiel starten:** Needs Plus legt `tesmioloader\plugins\needs_plus.ini` an und übernimmt dabei deine vorhandene `tesmioloader\plugins\needs.ini` unverändert. Danach pflegst du die Liste im Republic Mod Manager unter „Needs Plus“ oder direkt in der Datei.

---

## ✨ Features

### 🎯 Originalfunktionen (erhalten)
- ✅ Ein neues Bedürfnis je Zeile: `<ressource> = <spender>[, <faktor>[, <kategorie>[, <chance>[, <unzufriedenheit>]]]]`
- ✅ Das Bedürfnis wird vom Spender kopiert (food, meat, clothes, eletronics) und landet in genau den Läden, die den Spender führen
- ✅ Ladenkategorie wählbar: wie der Spender, nur Lebensmittel, nur Kaufhaus, keine
- ✅ Wahrscheinlichkeit und Unzufriedenheit je Bedürfnis
- ✅ Bis zu 16 Bedürfnisse in der Liste; wie viele ein Bürger gleichzeitig tragen darf, ist einstellbar (`max_demands`, höchstens 7)

### 🆕 Verbesserungen (neu in Needs Plus)

#### 1️⃣ **Eigene Datei, automatisch übernommen**
Needs Plus liest `tesmioloader\plugins\needs_plus.ini`, auch wenn die DLL aus dem Workshop kommt. Fehlt sie, entsteht sie beim ersten Start aus dem, was du schon hast: aus `tesmioloader\plugins\needs.ini` des Original-Plugins, in derselben Reihenfolge, mit einem Kopfkommentar, woher sie stammt. Das Original wird dabei nur gelesen, nie verändert. Stammt die `needs.ini` vom Soviet Mod Loader (der schreibt sie beim Spielstart mit allen Paketen zusammen neu), nimmt Needs Plus stattdessen SMLs Grundfassung, damit kein Paketbedürfnis doppelt erscheint. Gibt es beides nicht, nimmt es die `needs.ini` neben der DLL des Originals im Workshop-Ordner und erst danach die INI neben seiner eigenen DLL.

#### 2️⃣ **Doppelte Namen werden abgewiesen**
Steht eine Ressource zweimal in `[list]`, zählt die erste Zeile, die zweite wird mit einer Zeile im Protokoll übergangen. Das Original hätte den Bürgern das Bedürfnis zweimal gegeben.

#### 3️⃣ **Tritt von selbst zur Seite**
Solange das Original geladen ist oder der Soviet Mod Loader läuft, bleibt Needs Plus untätig und schreibt eine Zeile ins Protokoll. Das Original erkennt es auch, wenn es aus dem Workshop-Ordner geladen wird. Kein Bürger bekommt ein Bedürfnis doppelt.

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden, und Needs Plus und das Original-Plugin needs laufen nie gleichzeitig.

### ⚠️ Wichtig: Originales Plugin ausschalten

Hast du auch das Plugin needs von MaxLegend (abonniert oder in `tesmioloader\plugins`):
- **Republic Mod Manager:** schaltet needs beim Speichern von selbst aus, sobald Needs Plus eingeschaltet ist
- **TesmioLauncher:** das Häkchen bei needs entfernen
- **Abbestellen oder Löschen ist nicht nötig** – solange das Original geladen ist, bleibt Needs Plus von selbst untätig (Logzeile `needs_plus  idle`)

**Zurück zum Original:** im Republic Mod Manager needs einschalten (Needs Plus geht dabei von selbst aus), oder im TesmioLauncher das Häkchen bei needs setzen und bei needs_plus entfernen. Die DLL des Originals wird nie gelöscht oder überschrieben.

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Das Workshop-Objekt Republic Mod Manager abonnieren – Needs Plus
   liegt darin im Ordner plugins\needs_plus
2. Der TesmioLoader (ab b0.4.6) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. Beim ersten Start legt das Plugin deine Liste an:
   <Spiel>\tesmioloader\plugins\needs_plus.ini
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager.

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere needs_plus.dll aus plugins\needs_plus\
   → <Spiel>\tesmioloader\plugins\
2. Nur wenn dort noch keine needs.ini des Originals liegt:
   kopiere auch needs_plus.ini dorthin
3. Mit einer needs.ini lässt du needs_plus.ini weg – das Plugin
   legt sie beim ersten Start aus deiner needs.ini an
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

- Der Eintrag **Needs Plus** bearbeitet `tesmioloader\plugins\needs_plus.ini`: Liste, Spender, Faktor, Kategorie, Chance, Unzufriedenheit und die Plugin-Schalter. Die Einstellungsseite bringt Republic Mod Manager (ab Version 0.6.3) selbst mit. Fehlt die Datei noch, legt RMM sie beim Öffnen der Seite an: aus der Liste des Originals (auch wenn die nur im Workshop-Ordner neben dessen DLL liegt), sonst aus der INI neben der DLL von Needs Plus. Ein blauer Hinweis nennt die Quelle.
- Der +-Dialog bietet nur Ressourcen an, die in der Ressourcenliste stehen (Resources Plus oder das Original).
- Needs Plus und needs sind im RMM ein **Ersatz-Paar**: Schaltest du eines ein, schaltet RMM das andere beim Speichern aus. Sind beide eingeschaltet, leuchtet das Original **orange** („Wird ersetzt“). Beide Einträge bleiben in der Liste.
- **Inhaltspakete** aus dem Workshop (Bedürfnisse ohne eigene DLL) trägt RMM mit „Im Spiel bereitstellen“ in `needs_plus.ini` ein und nimmt sie beim Ausschalten wieder heraus.
- Der Schalter **„Plugin aktiv“** bei Needs Plus schreibt den Eintrag in die `tesmioloader.ini`.
- Das Protokollfenster zeigt `logs\tesmioloader.needs_plus.log`.

---

## ⚙️ Konfiguration

### Hauptdatei: `needs_plus.ini`

Die Liste liegt immer in `<Spiel>\tesmioloader\plugins\needs_plus.ini`, auch wenn die DLL aus dem Workshop kommt. Fehlt die Datei, legt das Plugin sie beim ersten Spielstart einmal an, aus der ersten Quelle, die es findet:
1. `tesmioloader\plugins\needs.ini` des Original-Plugins, mit denselben Einträgen in derselben Reihenfolge
2. die Grundfassung des Soviet Mod Loader, wenn die `needs.ini` von ihm erzeugt wurde oder fehlt
3. die `needs.ini` neben der DLL des Originals im Workshop-Ordner, wenn der Loader das Original von dort nimmt
4. die INI neben der DLL, im Workshop-Objekt `plugins\needs_plus\needs_plus.ini`

Die INI neben der DLL ist also nur die Vorlage für den ersten Start. Danach zählt allein die Datei im Loader-Ordner; ein Steam-Update ändert deine Liste nicht.

| Abschnitt | Inhalt |
|---|---|
| `[list]` | Eine Zeile je Bedürfnis: `<ressource> = <spender>[, <faktor>[, <kategorie>[, <chance>[, <unzufriedenheit>]]]]` |
| `[needs]` | Wie das Plugin das Spiel erreicht: `enabled`, `demand`, `storage`, `max_demands`, `when_full`, `probe`, `log_seconds` |

| Feld | Bedeutung |
|---|---|
| Spender | Vorhandenes Bürgerbedürfnis: `food`, `meat`, `clothes` oder `eletronics` (Schreibweise des Spiels) |
| Faktor | Menge im Verhältnis zum Spender, `1.0` = gleich viel |
| Kategorie | `auto` (wie der Spender), eine Ladenklasse des Spiels: `basic` (Lebensmittel), `medium`, `advanced` (großes Kaufhaus), `mediumadvanced`, `hotel`, oder `none` (nur eigene Läden mit `$STORAGE_SPECIAL`) |
| Chance | 0 bis 1, wie oft ein Bürger das Bedürfnis überhaupt aufnimmt |
| Unzufriedenheit | 0 bis 1, was der Bürger je vergeblichem Einkauf an Zufriedenheit verliert |

Das Format ist das des Original-Plugins, jeder Schlüssel ist in der Datei selbst erklärt.

### Wichtig zu wissen
- **Die Ressource muss existieren.** Sie steht in der Ressourcenliste (Resources Plus: `tesmioloader\plugins\resources_plus.ini`), und ihre Transportklasse muss zu den Läden des Spenders passen. Am einfachsten: die Ressource dort vom selben Spender klonen.
- **Spielstände hängen an der Liste.** Jedes Bedürfnis legt in den betroffenen Läden einen Lagerplatz an, und die Lagerliste ist Teil des Spielstands. Änderungen zuerst auf einer Kopie testen.

---

## 🔗 Verhältnis zum Original-Plugin

### Koexistenz mit `needs.dll`

Needs Plus und das Original-Plugin `needs` nutzen:
- **dieselben Einhakpunkte** im Spiel (Tagesplan der Bürger, Lagerplätze, Ladentakt)
- **dasselbe Dateiformat**, nur getrennte Dateien: `needs_plus.ini` und `needs.ini`, beide in `tesmioloader\plugins`
- **Kompatibilität:** Spielstände bleiben gültig, solange `[list]` dieselben Einträge hat

### Falls beide installiert sind

**Needs Plus lädt nur, wenn das Original ausgeschaltet ist.**

Ist das Original geladen (aus dem Workshop-Ordner oder aus `tesmioloader\plugins`), oder liegt `tesmioloader\plugins\needs.dll` und ist in tesmioloader.ini eingeschaltet:
- Needs Plus bleibt untätig, bevor es irgendetwas patcht
- Im Log: `needs_plus  idle - the original needs.dll is loaded`
- Das Original lädt normal

Läuft der Soviet Mod Loader, gilt dasselbe: `needs_plus  idle - soviet_mod_loader.dll brings its own needs`.

**Lösung:** Original oder SML im TesmioLauncher oder in Republic Mod Manager ausschalten. Löschen ist nicht notwendig.

---

## 💾 Kompatibilität

### Speicherformat
Unverändert gegenüber dem Original: Die Lagerplätze der Läden sind Teil des Spielstands. Ein Spielstand, der mit dem Original oder unter SML entstand, lädt mit Needs Plus, solange `[list]` dieselben Einträge hat. Genau das stellt die automatische Übernahme sicher.

### Versionskompatibilität
- Mit Soviet Mod Loader: SML bringt Bedürfnisse selbst mit. Läuft er, pausiert dieses Plugin und schreibt eine Zeile ins Protokoll; willst du stattdessen Needs Plus, schalte `soviet_mod_loader` in der `tesmioloader.ini` aus (im Republic Mod Manager der Schalter „Plugin aktiv“)
- 0.1.9: Für TesmioLoader b0.4.6 neu gebaut; sonst unverändert.
- 0.1.8: Nur Texte: Anleitung, Workshop-Text und Einstellungsseite auf den aktuellen Stand gebracht; im Spiel ändert sich nichts.
- 0.1.7: Beim Laden eines Spielstands verschwindet der leere Platz für Möbel, Speisesalz und Co. aus Werken, die ihn von einer früheren Version bekommen haben – nur wenn er leer ist und das Gebäude die Ware nicht selbst führt (eine Möbelfabrik behält ihr Möbel-Lager). Läden und Hotels bleiben, wie sie sind. Braucht den Loader aus dem Republic Mod Manager (ab b0.4.5-rmm.3).
- 0.1.6: Neue Waren wie Möbel oder Speisesalz landen nur noch in Läden und Hotels. Vorher bekamen auch Fabriken einen leeren Lagerplatz dafür, etwa das Flugzeugwerk und das Raketenwerk einen für Möbel. Ein schon gebautes Werk behielt diesen leeren Platz; seit 0.1.7 verschwindet er beim Laden eines Spielstands.
- 0.1.5: Für WRSR 1.2.0.9 (Hotfix zum Raketen-DLC); sonst unverändert.
- 0.1.4: Für WRSR 1.2.0.8 mit dem Raketen-DLC; sonst unverändert.
- 0.1.3: Die erste Liste entsteht jetzt auch aus der `needs.ini` des Originals, wenn das Original nur im Workshop-Ordner liegt. Vorher sah das Plugin nur im Loader-Ordner nach und nahm sonst die mitgelieferte Liste
- 0.1.2: Für WRSR 1.1.1.13. Das Plugin erkennt das Original needs jetzt auch, wenn es aus dem Workshop-Ordner geladen wird, und bleibt dann untätig. Die Funktion des Spiels, über die Läden ihre Lagerplätze bekommen, wird vor dem ersten Aufruf geprüft; passt sie nicht zur Spielversion, werden die Läden in dieser Sitzung nicht bestückt und das Log sagt es
- 0.1.1: Sicherheitsrunde ohne neue Funktion: eigenes Protokoll `logs\tesmioloader.needs_plus.log` für `probe`, die Zusammenfassung bleibt in `tesmioloader.log`; Faktor, Chance und Unzufriedenheit einer Zeile sowie `max_demands`, `when_full` und `log_seconds` werden geprüft und mit einer WARN-Zeile auf den erlaubten Wert gesetzt; eine UTF-8-BOM in needs_plus.ini versteckt `[list]` nicht mehr; mehr als 16 Bedürfnisse werden einmal gemeldet statt still abgeschnitten; die Lagerplatz-Korrektur der Läden schaltet sich nach einem Fehler ab, statt ihn zu wiederholen
- 0.1.0: erste Fassung als Fork von needs 1.0. Gleiche Funktionen, dazu die eigene Datei mit automatischer Übernahme, die Prüfung doppelter Namen und die Bremse gegenüber Original und SML.

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Keine eigenen Bedürfnisse im Spiel | Original oder SML eingeschaltet | Log lesen: `needs_plus  idle …`, dann das andere abschalten |
| Im Log „no resource named …“ | Ressource fehlt in der Ressourcenliste | Sie in Resources Plus anlegen, dann neu starten |
| Im Log „cannot be stored in transport class …“ | Transportklasse passt nicht zu den Läden des Spenders | Ressource in der Ressourcenliste vom selben Spender klonen |
| Ein Bedürfnis fehlt, im Log steht „listed twice“ | Name zweimal in `[list]` | Zweite Zeile entfernen |
| Bürger wollen die Ware, Läden haben sie nicht | `storage = 0` | `storage = 1` setzen oder die Läden selbst bestücken |

### Logging
- Zusammenfassung in `tesmioloader.log`: die Zeile des Loaders mit `needs_plus` und der Version `0.1.7`, beim ersten Start `created plugins\needs_plus.ini from …`, dann je Bedürfnis eine Zeile mit Spender, Faktor und Kategorie; ungültige Werte in der INI stehen dort als WARN-Zeile
- Mit `probe = 1` schreibt das Plugin die Bedürfnislisten der Bürger und die Lagerplätze der Läden in sein eigenes Protokoll `logs\tesmioloader.needs_plus.log`

---

## 📦 Dateistruktur

**Workshop-Objekt Republic Mod Manager** (Steam-Abo)
```
<Workshop-Objekt>\
└── plugins\
    └── needs_plus\
        ├── needs_plus.dll              (Plugin)
        ├── needs_plus.ini              (ausgelieferte Liste, Vorlage für den ersten Start)
        └── rmm.mod.ini                 (Manifest für Republic Mod Manager)
```

**Loader-Ordner**
```
<Spiel>\tesmioloader\
├── plugins\
│   ├── needs_plus.dll              (nur bei Methode 2)
│   ├── needs_plus.ini              (deine Liste; beim ersten Spielstart oder beim ersten Öffnen im RMM angelegt)
│   └── needs.ini                   (Original-Plugin, bleibt unangetastet)
└── logs\
    └── tesmioloader.needs_plus.log (ausführliches Protokoll, entsteht beim ersten Start)
```

Aus dem Workshop geladen, bleibt die DLL im Workshop-Objekt; in `tesmioloader\plugins` liegt dann nur deine Liste.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Needs Plus ist ein Fork des Plugins `needs` aus dem TesmioLoader von MaxLegend (Tesmio), GPL v3, https://github.com/MaxLegend/TesmioLoader; Einhakpunkte und Dateiformat sind bewusst mit dem Original identisch geblieben. Der vollständige Quelltext von Needs Plus liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/needs_plus.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Kann ich beide Plugins gleichzeitig benutzen?**
A: Nein. Entweder das Original ODER Needs Plus. Solange das Original eingeschaltet ist, bleibt Needs Plus untätig.

**F: Muss ich meine needs.ini von Hand übertragen?**
A: Nein. Liegt sie in `tesmioloader\plugins`, kopiert Needs Plus sie beim ersten Start nach `needs_plus.ini`. Liegt sie nur im Workshop-Ordner neben der DLL des Originals, übernimmt Needs Plus sie von dort. Danach ist `needs_plus.ini` die Datei, die zählt.

**F: Was ist mit dem Soviet Mod Loader?**
A: SML bringt das Original eingebaut mit. Unter SML pausiert Needs Plus. Willst du es, schalte SML aus; Inhaltspakete stellt dann der Republic Mod Manager bereit.

**F: Warum ist mein needs ausgeschaltet?**
A: Republic Mod Manager schaltet das Original aus, sobald Needs Plus eingeschaltet ist, damit nicht beide gleichzeitig laufen. Gelöscht wird nichts, und ein Schalter bringt das Original zurück.

---

**Letzte Aktualisierung:** Needs Plus 0.1.9  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
