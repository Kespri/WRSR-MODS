# 📦 Resources Plus 0.1.10

**Erweiterung des TesmioLoader-Plugins resources**

Eigene Ressourcen für *Workers & Resources: Soviet Republic* 1.2.0.9: neue Güter mit Symbol, Frachtmodellen, Preis und Handel im Zollhaus, ganz aus einer INI heraus.

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

### In drei Schritten
1. **Abonnieren:** Resources Plus steckt im Workshop-Objekt Republic Mod Manager. Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner.
2. **Original ausschalten:** Hast du auch das Plugin resources von MaxLegend, schalte es aus. Republic Mod Manager macht das beim Speichern von selbst; im TesmioLauncher entfernst du das Häkchen bei resources. Abbestellen ist nicht nötig.
3. **Spiel starten:** Resources Plus legt `tesmioloader\plugins\resources_plus.ini` an und übernimmt dabei deine vorhandene `tesmioloader\plugins\resources.ini` unverändert. Danach pflegst du die Liste im Republic Mod Manager unter „Resources Plus“ oder direkt in der Datei.

---

## ✨ Features

### 🎯 Originalfunktionen (erhalten)
- ✅ Neue Ressourcen als Kopie eines Vorbilds (`= steel, Cable`) oder ganz eigen (`= custom` plus `[custom:<name>]`)
- ✅ Symbol, Frachtmodelle, Transportklasse, Preisart und Marktwerte je Ressource
- ✅ Preise aus der Produktionskette, `[base_price]` und `[price]` zum Übersteuern
- ✅ Zollhäuser bekommen neue Ressourcen auch nachträglich als Handelsplatz
- ✅ Bis zu 256 Einträge, das Plugin vergrößert die Ressourcentabelle des Spiels selbst

### 🆕 Verbesserungen (neu in Resources Plus)

#### 1️⃣ **Kein Absturz mehr beim Beenden**
Das Original ließ das Spiel beim Beenden abstürzen, sobald mehr als sechs eigene Ressourcen eingetragen waren (Windows meldete `ucrtbase.dll`, Code `c0000409`). Spielstände waren nie betroffen, aber jeder Ausstieg hinterließ einen Absturzbericht. Resources Plus legt die vergrößerte Tabelle so an, wie das Spiel sie beim Beenden wieder freigibt.

#### 2️⃣ **Eigene Datei, automatisch übernommen**
Resources Plus liest `tesmioloader\plugins\resources_plus.ini`, auch wenn die DLL aus dem Workshop kommt. Fehlt sie, entsteht sie beim ersten Start aus dem, was du schon hast: aus `tesmioloader\plugins\resources.ini` des Original-Plugins, in derselben Reihenfolge, mit einem Kopfkommentar, woher sie stammt. Das Original wird dabei nur gelesen, nie verändert. Stammt die `resources.ini` vom Soviet Mod Loader (der schreibt sie beim Spielstart mit allen Paketen zusammen neu), nimmt Resources Plus stattdessen SMLs Grundfassung, damit keine Paketressource doppelt erscheint. Gibt es beides nicht, nimmt es die `resources.ini` neben der DLL des Originals im Workshop-Ordner und erst danach die INI neben seiner eigenen DLL.

#### 3️⃣ **Doppelte Namen werden abgewiesen**
Steht ein Name zweimal in `[list]`, zählt der erste, der zweite wird mit einer Zeile im Protokoll übergangen. Das Original hätte ihn zweimal angemeldet.

#### 4️⃣ **Tritt von selbst zur Seite**
Solange das Original geladen ist oder der Soviet Mod Loader läuft, bleibt Resources Plus untätig und schreibt eine Zeile ins Protokoll. Das Original erkennt es auch, wenn es aus dem Workshop-Ordner geladen wird. Es gibt nie zwei Plugins, die dieselbe Ressource anmelden.

#### 5️⃣ **Namen in der Spielsprache**
Ein Abschnitt `[names:German]` gibt einer Ressource für das deutsche Spiel einen eigenen Namen, `[names:French]` für das französische und so weiter. Im deutschen Spiel steht dann „Kabel“ statt „Cable“: im Zollhaus, in Lagern, bei Fahrzeugen und am Pinsel von Deposits Plus. Welche Sprache läuft, erfährt das Plugin beim Start vom Spiel selbst.

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden, und Resources Plus und das Original-Plugin resources laufen nie gleichzeitig.

### ⚠️ Wichtig: Originales Plugin ausschalten

Hast du auch das Plugin resources von MaxLegend (abonniert oder in `tesmioloader\plugins`):
- **Republic Mod Manager:** schaltet resources beim Speichern von selbst aus, sobald Resources Plus eingeschaltet ist
- **TesmioLauncher:** das Häkchen bei resources entfernen
- **Abbestellen oder Löschen ist nicht nötig** – solange das Original geladen ist, bleibt Resources Plus von selbst untätig (Logzeile `resources_plus  idle`)

**Zurück zum Original:** im Republic Mod Manager resources einschalten (Resources Plus geht dabei von selbst aus), oder im TesmioLauncher das Häkchen bei resources setzen und bei resources_plus entfernen. Die DLL des Originals wird nie gelöscht oder überschrieben.

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Das Workshop-Objekt Republic Mod Manager abonnieren – Resources Plus
   liegt darin im Ordner plugins\resources_plus
2. Der TesmioLoader (ab b0.4.6) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. Beim ersten Start legt das Plugin deine Liste an:
   <Spiel>\tesmioloader\plugins\resources_plus.ini
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager.

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere resources_plus.dll aus plugins\resources_plus\
   → <Spiel>\tesmioloader\plugins\
2. Nur wenn dort noch keine resources.ini des Originals liegt:
   kopiere auch resources_plus.ini dorthin
3. Mit einer resources.ini lässt du resources_plus.ini weg – das Plugin
   legt sie beim ersten Start aus deiner resources.ini an
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

- Der Eintrag **Resources Plus** bearbeitet `tesmioloader\plugins\resources_plus.ini`: Liste, Vorbild, Anzeigename, Transportklasse, Preisart, Marktwerte. Die Einstellungsseite bringt Republic Mod Manager (ab Version 0.6.3) selbst mit. Fehlt die Datei noch, legt RMM sie beim Öffnen der Seite an: aus der Liste des Originals (auch wenn die nur im Workshop-Ordner neben dessen DLL liegt), sonst aus der INI neben der DLL von Resources Plus. Ein blauer Hinweis nennt die Quelle.
- Resources Plus und resources sind im RMM ein **Ersatz-Paar**: Schaltest du eines ein, schaltet RMM das andere beim Speichern aus. Sind beide eingeschaltet, leuchtet das Original **orange** („Wird ersetzt“). Beide Einträge bleiben in der Liste.
- **Inhaltspakete** aus dem Workshop (Ressourcen ohne eigene DLL) trägt RMM mit „Im Spiel bereitstellen“ in `resources_plus.ini` ein und nimmt sie beim Ausschalten wieder heraus.
- Der Schalter **„Plugin aktiv“** bei Resources Plus schreibt den Eintrag in die `tesmioloader.ini`.
- Das Protokollfenster zeigt `logs\tesmioloader.resources_plus.log`.

---

## ⚙️ Konfiguration

### Hauptdatei: `resources_plus.ini`

Die Liste liegt immer in `<Spiel>\tesmioloader\plugins\resources_plus.ini`, auch wenn die DLL aus dem Workshop kommt. Fehlt die Datei, legt das Plugin sie beim ersten Spielstart einmal an, aus der ersten Quelle, die es findet:
1. `tesmioloader\plugins\resources.ini` des Original-Plugins, mit denselben Einträgen in derselben Reihenfolge
2. die Grundfassung des Soviet Mod Loader, wenn die `resources.ini` von ihm erzeugt wurde oder fehlt
3. die `resources.ini` neben der DLL des Originals im Workshop-Ordner, wenn der Loader das Original von dort nimmt
4. die INI neben der DLL, im Workshop-Objekt `plugins\resources_plus\resources_plus.ini`

Die INI neben der DLL ist also nur die Vorlage für den ersten Start. Danach zählt allein die Datei im Loader-Ordner; ein Steam-Update ändert deine Liste nicht.

| Abschnitt | Inhalt |
|---|---|
| `[list]` | Eine Zeile je Ressource: `<name> = <Vorbild oder custom>[, <Anzeigename>]` |
| `[names:<Sprache>]` | Namen für eine Spielsprache: `<name> = <Anzeigename>`. `<Sprache>` steht so im Dateinamen `soviet<Sprache>.btf` des Spiels, zum Beispiel `German` |
| `[custom:<name>]` | Feinabstimmung: `transport`, `kind`, `cargo`, `price`, `market_rub`, `market_usd` und mehr |
| `[base_price]` / `[price]` | Preise vor bzw. nach der Preisrechnung des Spiels übersteuern |
| `[resources]` | Wie das Plugin das Spiel erreicht: `hook`, `resource_capacity`, `price_hook`, `price_report`, `custom_report` |
| `[customs]` | Zollhaus-Anbindung: `hook`, `probe` |

Das Format ist das des Original-Plugins, jeder Schlüssel ist in der Datei selbst erklärt. Die Vorbildressource entscheidet über Transportklasse und Frachtform, darum: `open` für Stückgut (Vorbild steel), Silogüter über alumina, Schüttgut über rawgravel oder gravel, Nahrungsmittel über food.

### Wichtig zu wissen
- **Spielstände hängen an `[list]`.** Ein Spielstand mit zwei eigenen Ressourcen lädt nicht ohne sie, ein Spielstand ohne eigene Ressourcen nicht mit ihnen. Änderungen also nur mit einem passenden Spielstand oder einem neuen Spiel. Die Übernahme aus `resources.ini` behält die Reihenfolge bei, deine Spielstände laden also weiter.
- **Namen je Sprache:** Der Anzeigename in `[list]` gilt für jede Sprache, die keinen eigenen Abschnitt hat; lass ihn also englisch. Deutsche Namen kommen unter `[names:German]`. Die mitgelieferte Liste und die Inhaltspakete bringen ihre deutschen Namen schon mit. Hast du schon eine eigene `resources_plus.ini`, liest das Plugin die Namen der mitgelieferten Ressourcen zusätzlich aus der INI neben seiner DLL; steht ein Name auch in deiner Datei, gilt deiner. Für eigene Ressourcen trägst du die Namen im Republic Mod Manager ein (ab Version 0.6.6, Felder „Namen je Spielsprache“ ganz unten in den Eigenschaften der Ressource) oder von Hand in `resources_plus.ini`. Namen gelten ab dem nächsten Spielstart, Spielstände hängen nicht daran.
- Symbol: `media_soviet\resources\<name>.png` (48×48, RGBA), am besten über `tesmioloader\vfs\`.
- Frachtmodelle: `resources\<name>.nmf` bei Stückgut, `<name>1..4.nmf` plus `<name>_vehicle.nmf` bei Schüttgut.

---

## 🔗 Verhältnis zum Original-Plugin

### Koexistenz mit `resources.dll`

Resources Plus und das Original-Plugin `resources` nutzen:
- **denselben Registrierungsdienst:** `resources`
- **dasselbe Dateiformat**, nur getrennte Dateien: `resources_plus.ini` und `resources.ini`, beide in `tesmioloader\plugins`
- **dieselben Text-Nummern** für die Anzeigenamen (ab 1000000)
- **Kompatibilität:** Spielstände bleiben gültig, solange `[list]` in derselben Reihenfolge dieselben Einträge hat

### Falls beide installiert sind

**Resources Plus lädt nur, wenn das Original ausgeschaltet ist.**

Ist das Original geladen (aus dem Workshop-Ordner oder aus `tesmioloader\plugins`), oder liegt `tesmioloader\plugins\resources.dll` und ist in tesmioloader.ini eingeschaltet:
- Resources Plus bleibt untätig, bevor es irgendetwas patcht
- Im Log: `resources_plus  idle - the original resources.dll is loaded`
- Das Original lädt normal

Läuft der Soviet Mod Loader, gilt dasselbe: `resources_plus  idle - soviet_mod_loader.dll brings its own resources`.

**Lösung:** Original oder SML im TesmioLauncher oder in Republic Mod Manager ausschalten. Löschen ist nicht notwendig.

---

## 💾 Kompatibilität

### Speicherformat
Unverändert gegenüber dem Original: Die Anzahl der Ressourcen ist Teil des Spielstands, die Anzeigenamen bekommen dieselben Text-Nummern. Ein Spielstand, der mit dem Original oder unter SML entstand, lädt mit Resources Plus, solange `[list]` dieselben Einträge in derselben Reihenfolge hat. Genau das stellt die automatische Übernahme sicher.

### Versionskompatibilität
- Mit Soviet Mod Loader: SML bringt Ressourcen selbst mit. Läuft er, pausiert dieses Plugin und schreibt eine Zeile ins Protokoll; willst du stattdessen Resources Plus, schalte `soviet_mod_loader` in der `tesmioloader.ini` aus (im Republic Mod Manager der Schalter „Plugin aktiv“)
- 0.1.10: Für TesmioLoader b0.4.6 neu gebaut; sonst unverändert.
- 0.1.9: Nur Texte: Anleitung, Workshop-Text und Einstellungsseite auf den aktuellen Stand gebracht; im Spiel ändert sich nichts.
- 0.1.8: Fehler behoben: Ein fester Preis aus `[price]` kam am Zoll und im Handelsfenster nicht an, weil das Spiel den Preis danach noch einmal neu ausrechnet. Er wird jetzt auch hinter dieser zweiten Rechnung gesetzt. Im Log stehen Rubel und Dollar jetzt in der richtigen Spalte.
- 0.1.7: Für WRSR 1.2.0.9 (Hotfix zum Raketen-DLC); sonst unverändert.
- 0.1.6: Für WRSR 1.2.0.8 mit dem Raketen-DLC. Das Plugin kennt die vier neuen Ressourcen des Spiels (`kerosene`, `lox`, `udmh`, `n2o4`) und die zwei neuen Transportklassen `cryo` und `hypergolic`. Alte Adress-Zeilen in Deiner `resources_plus.ini` stören nicht, das Plugin nimmt dann seine eigenen.
- 0.1.5: Die erste Liste entsteht jetzt auch aus der `resources.ini` des Originals, wenn das Original nur im Workshop-Ordner liegt. Vorher sah das Plugin nur im Loader-Ordner nach und nahm sonst die mitgelieferte Liste
- 0.1.4: Namen in der Spielsprache: neue Abschnitte `[names:<Sprache>]` in der Liste; die mitgelieferte Liste bringt deutsche Namen mit. Ohne solche Abschnitte ändert sich nichts
- 0.1.3: Für WRSR 1.1.1.13. Das Plugin erkennt das Original resources jetzt auch, wenn es aus dem Workshop-Ordner geladen wird, und bleibt dann untätig. Steht in deiner `resources_plus.ini` noch eine Spieladresse der vorigen Spielversion (`resource_rva`, `price_pass_rva`, `customhouse_tick_rva`), übergeht das Plugin sie mit einer Zeile im Protokoll und nimmt die eingebaute
- 0.1.2: Sicherheitsrunde ohne neue Funktion: das Zollhaus-Abgleichen läuft unter demselben Lock wie das Verschieben der Ressourcentabelle und prüft die Push-Funktion des Spiels vor dem ersten Aufruf auf ihre Bytes; Transportklassen ausserhalb der 18 des Datensatzes werden nicht mehr gelesen; das Anmelden der Ressourcen und das Preis-Bracket schalten sich nach einem Fehler ab, statt ihn zu wiederholen; `packed = auto` tut wieder, was der Kommentar sagt; eine UTF-8-BOM in resources_plus.ini versteckt `[list]` nicht mehr; `resource_capacity`, `resource_vector_rva`, `packed` und eine Zahl bei `family` werden geprüft und mit WARN abgewiesen; Schreibfehler nennen den Windows-Fehlercode
- 0.1.1: Die Bremse gegenüber Soviet Mod Loader greift auch, wenn Resources Plus vor SML geladen wird (alphabetische Reihenfolge in `plugins`): `plugins\soviet_mod_loader.dll` vorhanden und eingeschaltet reicht. Ohne diesen Fix blockierte SML den Spielstart mit „resources registration hook was not installed".
- 0.1.0: erste Fassung als Fork von resources 1.7. Gleiche Funktionen, dazu der Fix beim Beenden, die eigene Datei mit automatischer Übernahme, die Prüfung doppelter Namen und die Bremse gegenüber Original und SML.

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Keine eigenen Ressourcen im Spiel | Original oder SML eingeschaltet | Log lesen: `resources_plus  idle …`, dann das andere abschalten |
| „Spielstand braucht Mods“ beim Laden | `[list]` passt nicht zum Spielstand | Dieselben Einträge in derselben Reihenfolge eintragen |
| Eine Ressource fehlt, im Log steht „listed twice“ | Name zweimal in `[list]` | Zweite Zeile entfernen |
| Lager zeigt 0.00 von 0.00 t | Transportklasse des Vorbilds passt nicht zum `$STORAGE` des Gebäudes | Anderes Vorbild wählen oder `transport =` in `[custom:<name>]` setzen |
| Ein Zollhaus, das schon stand, handelt die neue Ressource nicht | `hook = 0` in `[customs]` | `hook = 1` setzen, oder das Zollhaus abreißen und neu bauen |
| Kein Symbol | PNG fehlt | `media_soviet\resources\<name>.png`, 48×48 RGBA |

### Logging
- Zusammenfassung in `tesmioloader.log`: die Zeile des Loaders mit `resources_plus` und der Version `0.1.8`, beim ersten Start `created plugins\resources_plus.ini from …`, dann je Ressource `published as index …`; mit `price_report = 1` dazu die Preistabelle, mit `custom_report = 1` die Datensätze
- Details in `logs\tesmioloader.resources_plus.log`: jeder Ressourcenname, den das Spiel abgefragt hat

---

## 📦 Dateistruktur

**Workshop-Objekt Republic Mod Manager** (Steam-Abo)
```
<Workshop-Objekt>\
└── plugins\
    └── resources_plus\
        ├── resources_plus.dll          (Plugin)
        ├── resources_plus.ini          (ausgelieferte Liste, Vorlage für den ersten Start)
        └── rmm.mod.ini                 (Manifest für Republic Mod Manager)
```

**Loader-Ordner**
```
<Spiel>\tesmioloader\
├── plugins\
│   ├── resources_plus.dll              (nur bei Methode 2)
│   ├── resources_plus.ini              (deine Liste; beim ersten Spielstart oder beim ersten Öffnen im RMM angelegt)
│   └── resources.ini                   (Original-Plugin, bleibt unangetastet)
└── logs\
    └── tesmioloader.resources_plus.log
```

Aus dem Workshop geladen, bleibt die DLL im Workshop-Objekt; in `tesmioloader\plugins` liegt dann nur deine Liste.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Resources Plus ist ein Fork des Plugins `resources` aus dem TesmioLoader von MaxLegend (Tesmio), GPL v3, https://github.com/MaxLegend/TesmioLoader; Servicename, Text-Nummern und Dateiformat sind bewusst mit dem Original identisch geblieben. Der vollständige Quelltext von Resources Plus liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/resources_plus.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Kann ich beide Plugins gleichzeitig benutzen?**
A: Nein. Entweder das Original ODER Resources Plus. Solange das Original eingeschaltet ist, bleibt Resources Plus untätig.

**F: Muss ich meine resources.ini von Hand übertragen?**
A: Nein. Liegt sie in `tesmioloader\plugins`, kopiert Resources Plus sie beim ersten Start nach `resources_plus.ini`, in derselben Reihenfolge. Liegt sie nur im Workshop-Ordner neben der DLL des Originals, übernimmt Resources Plus sie von dort. Danach ist `resources_plus.ini` die Datei, die zählt.

**F: Was ist mit dem Soviet Mod Loader?**
A: SML bringt das Original eingebaut mit. Unter SML pausiert Resources Plus, der Fix beim Beenden wirkt dann nicht. Willst du ihn, schalte SML aus; Inhaltspakete stellt dann der Republic Mod Manager bereit.

**F: Warum ist mein resources ausgeschaltet?**
A: Republic Mod Manager schaltet das Original aus, sobald Resources Plus eingeschaltet ist. So läuft die fehlerbereinigte Fassung und nicht beide gleichzeitig. Gelöscht wird nichts, und ein Schalter bringt das Original zurück.

---

**Letzte Aktualisierung:** Resources Plus 0.1.10  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
