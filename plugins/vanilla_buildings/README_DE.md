# 🏗️ Vanilla Buildings 0.4.18

**TesmioLoader-Plugin für temporäre Änderungen an Gebäudedateien**

Passt Gebäude des Spiels, der DLCs und des Workshops in *Workers & Resources: Soviet Republic* 1.2.0.9 an, ohne eine Originaldatei zu verändern: Bei jedem Spielstart entstehen temporäre Kopien mit den gewünschten Zeilen, und nur die Leseaufrufe des Spiels werden dorthin umgeleitet. Typischer Einsatz: neuen Materialien wie Streusalz, Kies oder Sand ein Lager in den Technischen Diensten geben, damit sie geliefert und angenommen werden.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Befehle](#-befehle)
- [Wertebereiche](#-wertebereiche)
- [Sicherheitsregeln](#-sicherheitsregeln)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.2.0.9 (Referenzversion; nach einem Spielupdate können sich Originalzeilen ändern, nicht mehr passende Regeln werden dann sicher abgewiesen)
- TesmioLoader ab b0.4.5 (API 4)
- Localization (ab 0.4.0): Das Plugin selbst braucht es nur für übersetzte Gebäudenamen (`name` mit einem Übersetzungsschlüssel) und läuft sonst auch ohne.
- Ressourcen, auf die eine Regel verweist (etwa `road_salt`), müssen im Spiel existieren, zum Beispiel über Resources Plus oder Resources.

### In drei Schritten
1. **Abonnieren:** Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner.
2. **Regelsätze einrichten:** Die mitgelieferte INI enthält ein ausgeschaltetes Beispiel, `[example_plastics_factory]` mit allen Befehlen. In Republic Mod Manager legst du mit + einen eigenen Regelsatz an, zum Beispiel Streugut-Lager für die Technischen Dienste wie unten gezeigt; ein getippter Name wie „Technical Service Storage“ wird zum Abschnittsnamen technical_service_storage.
3. **Spiel vollständig neu starten.** Im Log steht je Ziel `[overlay-opened]`, sobald das Spiel die geänderte Datei liest.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Gebäude des Spiels (`buildings_types\*.ini`), der DLCs (`dlcN\buildings\...\building.ini`) und des Workshops (`WorkshopID\...\building.ini`) in einem Regelsatz
- ✅ Mehrere Zieldateien je Regelsatz teilen sich dieselben Befehle; jedes Ziel wird für sich gegen seine unveränderte Originaldatei geprüft
- ✅ Befehle: Zeile ersetzen, entfernen, hinzufügen, vor einem Anker einfügen; Anschlussblöcke hinzufügen, ersetzen, entfernen
- ✅ Originaldateien im Spiel- und Workshopordner werden nie angefasst; ein Neustart ohne Plugin stellt alles zurück
- ✅ Ein abgewiesenes Ziel blockiert die anderen nicht; jede Ablehnung wird mit Abschnitt, Ziel und Ursache protokolliert

### 🆕 Neu in 0.4.0
- ✅ Alle Texte für Republic Mod Manager im Spieler-Stil: Warnhinweis nur noch in der Karte Hinweise, ein Kopfschalter, Karte Fehlersuche, Hinweiskasten und mehrzeilige Feldtexte auf dem Reiter Gebäude.
- ✅ **`insert` mit Position:** `insert = 0 | ANKER | ZEILE` fügt vor dem Anker ein, `insert = 1 | ANKER | ZEILE` danach. Bei `$COST_RESOURCE_AUTO` hängt 1 das Material an die Phase der `$COST_WORK`-Ankerzeile, 0 an die Phase davor.
- ✅ **INI neben der DLL:** Fehlt `tesmioloader\plugins\vanilla_buildings.ini`, liest die DLL die INI aus dem eigenen Ordner, also aus dem Workshop-Paket. Das Plugin läuft damit direkt aus dem Steam-Abo.
- ✅ Die gewählte Konfigurationsdatei steht beim Start im Log.
- ✅ Editor-Schema für Republic Mod Manager im Paket: Regelsätze mit Zielen und Befehlen als Liste, deutsch und englisch.

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden.

---

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Workshop-Objekt abonnieren
2. Der TesmioLoader (ab b0.4.5) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. Die INI findet die DLL neben sich im Paket
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager.

---

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere vanilla_buildings.dll und vanilla_buildings.ini aus plugins\
   → <Spiel>\tesmioloader\plugins\

2. Regelsätze in der INI einschalten oder anlegen, Spiel vollständig neu starten
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

Das Paket enthält im Ordner `config` ein Editor-Schema. Republic Mod Manager (ab Version 0.6.3) zeigt Vanilla Buildings damit in zwei Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, Knopf für diese Anleitung und die Karte Fehlersuche (Ausführliches Protokoll); ein- und ausgeschaltet wird das Plugin mit „Plugin aktiv“ oben im Kopf
- **Gebäude:** eine Zeile je Regelsatz mit Schalter, Kennung, erster Zieldatei und den Knöpfen Ändern und Löschen. „Hinzufügen“ und „Ändern…“ öffnen dasselbe Fenster mit zwei Reitern: **Angaben** (die Zieldateien) und **Zeilen** (der Zeilen-Editor: oben die Zieldatei wählen, links ihre Zeilen anklicken, daraus Anhängen/Ersetzen/Davor/Danach/Entfernen bauen; zwei Ansichten **Original** und **Ergebnis** – das Ergebnis zeigt schreibgeschützt die Datei, die das Spiel lesen wird, und markiert jede Zeile, die ein Befehl anlegt oder ersetzt; Anschlüsse fasst er als Block an, ein Klick auf eine Punktzeile nimmt den ganzen Anschluss; ein Befehl, der in dieser Datei nicht greift, steht rot mit Begründung, und solange einer rot ist, schreibt das Plugin für diese Datei gar nichts; das Kästchen „Gegen alle Zieldateien prüfen“ unter den Knöpfen sagt, in wie vielen Zielen ein Befehl passt). Jede Zeile in einem Feld ist eine INI-Zeile; unter dem Feld steht die Anzahl, am Griff lässt es sich aufziehen. „Gebäude auswählen…“ öffnet die Liste aller Gebäude des Spiels, der DLCs und der abonnierten Workshop-Objekte, nach Art gruppiert mit Suche und Filtern; angehakte Gebäude werden als Zielzeilen eingetragen

**Umbenennen ist Absicht, kein Nebeneffekt.** Republic Mod Manager bietet dafür kein Feld an: Vanilla Buildings ändert bestehende Gebäude, die Namen des Spiels sollen stehen bleiben; ein eigenes Gebäude baust du mit Buildings Plus. Willst du ein Gebäude trotzdem umbenennen, machst du das im Reiter Zeilen mit einem Ersetzen, etwa `$NAME 6218` → `$NAME_STR "Salzlager"`. Das geht nur in einem Regelsatz mit **genau dieser einen** Zieldatei: die alte Zeile steht in jeder Datei anders, in allen anderen Zielen fände der Befehl seinen Anker nicht, und ein danebengehender Befehl verwirft dort den ganzen Regelsatz. Das Kästchen „Gegen alle Zieldateien prüfen“ zeigt es sofort.

Original-Regelsätze aus dem Paket können ausgeblendet oder Zeile für Zeile übersteuert werden. Persönliche Änderungen liegen in `user_config\vanilla_buildings.editor.ini`, die wirksame Datei ist `tesmioloader\plugins\vanilla_buildings.ini`; die INI im Paket bleibt unverändert. Wer die INI lieber von Hand bearbeitet, findet alles Weitere unten.

---

## ⚙️ Konfiguration

### Hauptdatei: `vanilla_buildings.ini`

Die DLL liest in dieser Reihenfolge:
- **Zuerst:** `tesmioloader\plugins\vanilla_buildings.ini`, falls vorhanden (von Hand installiert oder die von Republic Mod Manager geschriebene wirksame INI)
- **Sonst:** die INI neben der DLL, im Workshop-Paket `plugins\vanilla_buildings.ini`

⚠️ **Kommentare nur in eigenen Zeilen mit `;` oder `#`.** Die Datei muss UTF-8 ohne BOM sein. Abschnitts- und Schlüsselnamen sind ohne Groß-/Kleinschreibung, Spiel-Direktiven und gesuchte Zeilen dagegen genau so wie im Original. Jeder Befehl steht in einer physischen Zeile; `|` trennt seine Felder. Änderungen gelten nach einem vollständigen Neustart des Spiels.

### Abschnitt `[general]`

```ini
[general]
; 1 aktiviert das Plugin, 0 installiert keinen Hook; nur genau 0 oder 1
enabled = 1
; 1 protokolliert zusätzlich ausgeschaltete Regelsätze und jeden umgeleiteten Dateizugriff
debug = 0
; 1 rüstet schon gebaute Gebäude beim Laden eines Spielstands mit den Lagern deiner Regelsätze nach
retrofit_storages = 1
; Versuch, Vorgabe 0: 1 nimmt auch Fahrzeugfabriken mit - nur an einer Kopie des Spielstands testen
retrofit_vehicle_factories = 0
```

Die alten Namen `[vanilla_buildings]` und `verbose` bleiben mit einer Warnung im Log unterstützt; verwende nur einen globalen Abschnitt und nur einen Debug-Schlüssel.

### Regelsätze: ein Abschnitt je Regelsatz

Der Abschnittsname ist frei wählbar und muss eindeutig sein. Jede `target`-Zeile ergänzt eine Zieldatei; alle Befehle gelten für jedes Ziel, egal wo sie im Abschnitt stehen. Beispiel für einen eigenen Regelsatz, der den Technischen Diensten Lager für Streusalz, Kies und Sand gibt:

```ini
[technical_service_storage]
; 0 lässt den Regelsatz in der Datei, wendet aber nichts an
enabled = 1
; Zieldateien, relativ zu media_soviet bzw. zum Workshop-Ordner des Spiels
target = buildings_types\technical_services_small.ini
target = buildings_types\technical_services.ini
target = buildings_types\technical_services_big.ini
target = dlc3\buildings\technical_services_small\building.ini
target = 2496571917\utrzymanie1\building.ini
; neue Lagerzeilen, je eine pro Material
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 road_salt
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 gravel
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand
```

Pfade bestimmen den Stammordner selbst: `buildings_types\...` und `dlcN\buildings\...` liegen unter `<Spiel>\media_soviet`, eine numerische Workshop-ID unter `<Steam-Bibliothek>\steamapps\workshop\content\784150`. DLC und Workshop-Objekt müssen installiert sein. Absolute Pfade, `.` und `..` werden abgewiesen. `target1`, `target2`, `target3` sind optionale Aliase, die zusätzlich die Zielart Spiel, DLC oder Workshop prüfen.

Dieselbe Zieldatei darf in mehreren aktiven Regelsätzen stehen, solange höchstens einer davon mehr tut, als Zeilen mit `add` anzuhängen. Regelsätze, die nur anhängen, vertragen sich untereinander und mit dem einen Regelsatz, der die Datei ändert; ihre Zeilen kommen hinter dessen Änderungen. Zwei Regelsätze, die dieselbe Datei ändern (ersetzen, entfernen, einfügen, Anschlüsse, Name), werden beide abgewiesen: Solche Änderungen gehören in einen Regelsatz. Passt eine der angehängten Zeilen nicht (zum Beispiel, weil sie in der Datei schon steht), wird die Zieldatei für alle beteiligten Regelsätze abgewiesen. Schon gebaute Gebäude bekommen neue Lager beim Laden eines Spielstands nachträglich, siehe [Spielstände](#spielstände).

**Gebäudename ändern:** Mit `name = …` gibst du allen Zielen des Regelsatzes einen neuen Namen im Baumenü. Schreib einfach den Text hin, ohne Anführungszeichen, höchstens 128 Zeichen. Das Plugin ersetzt die vorhandene `$NAME`- oder `$NAME_STR`-Zeile der Zieldatei; die alte Zeile musst du also nicht kennen. `name` darf je Regelsatz nur einmal stehen, und ein Ziel ohne genau eine Namenszeile wird abgewiesen und im Log genannt. Sieht der Wert wie ein Übersetzungsschlüssel aus (mindestens ein Punkt, sonst nur Buchstaben, Ziffern, Punkt, Unterstrich und Bindestrich), holt das Plugin den Text über das Plugin Localization: in der Spielsprache, wenn das Textpaket sie mitbringt, sonst in dessen Rückfallsprache. Ohne Localization oder mit einem unbekannten Schlüssel nimmt es den Teil hinter dem letzten Punkt und schreibt eine Warnung ins Log.

```ini
; fester Text: ergibt $NAME_STR "Medicine Factory"
name = Medicine Factory
; Übersetzungsschlüssel: ergibt $NAME mit der Nummer aus dem Textpaket
name = localization.lang.medicine
```

---

## 🛠️ Befehle

### Einzelzeilen

| Befehl | Bedeutung |
|---|---|
| `replace = ALT \| NEU` | Ersetzt eine vollständige Zeile, die genau einmal vorkommt. |
| `remove = ZEILE` | Entfernt eine vollständige Zeile, die genau einmal vorkommt. |
| `add = ZEILE` | Fügt eine neue Einzelzeile vor dem letzten `end` ein; abgewiesen, wenn sie schon existiert. |
| `insert = 0 \| ANKER \| ZEILE` | Fügt eine Zeile vor einer eindeutigen Ankerzeile ein; mehrere mit demselben Anker behalten ihre Reihenfolge. |
| `insert = 1 \| ANKER \| ZEILE` | Dasselbe nach der Ankerzeile. Der Anker darf auch eine Zeile sein, die ein früherer `add`, `insert` oder `replace` desselben Regelsatzes erzeugt. `insert_before = ANKER \| ZEILE` ist die alte Schreibweise von `insert = 0 \| …`. |

Leerzeichen am Zeilenanfang und -ende werden ignoriert, Unterschiede innerhalb der Zeile nicht.

### Anschlussblöcke

| Befehl | Bedeutung |
|---|---|
| `add_connection = TOKEN \| PUNKT1 \| PUNKT2` | Fügt einen neuen Dreizeilenblock nach dem letzten Anschluss ein. |
| `replace_connection = TOKEN \| PUNKT1 \| PUNKT2 \| NEUES_TOKEN` | Ändert nur das Token des über beide Punkte eindeutig gefundenen Blocks. |
| `replace_connection = TOKEN \| PUNKT1 \| PUNKT2 \| NEUES_TOKEN \| NEUER_PUNKT1 \| NEUER_PUNKT2` | Wie oben, ersetzt zusätzlich beide Punkte, zum Beispiel für eine andere Höhe. Das Token darf dabei gleich bleiben. Die neuen Punkte dürfen keinen anderen Anschluss berühren. |
| `remove_connection = TOKEN \| PUNKT1 \| PUNKT2` | Entfernt den ganzen Dreizeilenblock. |
| `add_connection = TOKEN \| PUNKT` | Einpunktiger Anschluss wie `$CONNECTION_ROAD_DEAD`, wird als eine Zeile `$TOKEN x y z` eingefügt. |
| `replace_connection = TOKEN \| PUNKT \| NEUES_TOKEN` | Ändert das Token eines einpunktigen Anschlusses; mit `\| NEUER_PUNKT` dahinter auch seinen Punkt. |
| `remove_connection = TOKEN \| PUNKT` | Entfernt einen einpunktigen Anschluss, egal ob das Spiel ihn einzeilig oder als Token plus Punktzeile schreibt. |

Ein Punkt besteht aus drei Zahlen, etwa `14.5 0 2`; der Vergleich ist numerisch, `0` und `0.0000` gelten als gleich. Die beiden Punkte müssen verschieden sein und in der Reihenfolge der Originaldatei stehen. Belegte Punkte und neue `*_ALLOWPASS`-Anschlüsse werden abgewiesen.

Beispiel mit sechs Feldern: Der Anschluss der Kunststofffabrik bleibt ein `$CONNECTION_CONNECTION`, sitzt danach aber einen Meter höher.

```ini
replace_connection = $CONNECTION_CONNECTION | -17.7 0.0 -15.38 | -17.7 0.0 -13.38 | $CONNECTION_CONNECTION | -17.7 1.0 -15.38 | -17.7 1.0 -13.38
```

Vollständiges Beispiel mit allen Befehlen, ausgeliefert als `[example_plastics_factory]` mit `enabled = 0`:

```ini
[example_plastics_factory]
enabled = 0
target = buildings_types\plastics_factory.ini
replace = $PRODUCTION plastics 0.11 | $PRODUCTION plastics 0.20
replace_connection = $CONNECTION_CONNECTION | 14.5 0.0 23.3 | 14.5 0.0 21.3 | $CONNECTION_WATERPIPE_INPUT
remove = $CONSUMPTION_PER_SECOND eletric 0.26
remove_connection = $CONNECTION_CONNECTION | -23.4 0.0 15.9 | -21.4 0.0 15.9
add = $PRODUCTION glass 0.45
add_connection = $CONNECTION_WATERPIPE_OUTPUT | 30 0 0 | 32 0 0
insert = 0 | $COST_WORK SOVIET_CONSTRUCTION_STEEL_LAYING 1.0 | $COST_RESOURCE_AUTO steel 2.0
```

---

## 📏 Wertebereiche

| Größe | Grenze |
|---|---|
| `enabled`, `debug`, `retrofit_storages`, `retrofit_vehicle_factories` | genau 0 oder 1 |
| Regelsätze | höchstens 256 |
| Zieldateien insgesamt | höchstens 256 |
| Befehle je Regelsatz | höchstens 512 |
| Abschnittsname | höchstens 128 Bytes |
| `name` | höchstens 128 Zeichen, ohne Anführungszeichen |
| Zielpfad | höchstens 220 Bytes |
| aktive Konfigurationszeile | höchstens 4096 Bytes |
| Plugin-INI und jede Originaldatei | höchstens 4 MiB |
| erzeugte Datei | höchstens 8 MiB |

Fehler in `[general]`, mehrdeutige Abschnittsgrenzen, falsche Kodierung oder überschrittene Dateigrenzen lehnen die ganze INI ab; dann wird kein Hook installiert.

---

## 🔒 Sicherheitsregeln

- Alle Regeln werden gegen die **unveränderte Originaldatei** geprüft, bevor eine Kopie entsteht.
- `replace`, `remove`, `insert` und die Anschlussbefehle brauchen genau einen Treffer; sonst wird das Ziel als Ganzes abgewiesen, ohne Teiländerung.
- Überlappende Änderungen und Anker, die eine andere Regel verändert, sind unzulässig.
- `add` akzeptiert keine `$COST_`-Zeilen; `insert` als neue `$COST_`-Zeile nur `$COST_RESOURCE_AUTO` mit einer eindeutigen `$COST_WORK`-Zeile als Anker (1 = Material dieser Phase, 0 = der Phase davor).
- Nur Leseaufrufe (`fopen`, `fopen_s`, `_wfopen`, `_wfopen_s`, Engine-Pufferleser) auf Dateien unter dem echten Spiel- oder Workshopordner werden umgeleitet; Schreibzugriffe nie.
- Die Prüfung schützt die Patchstruktur, nicht die fachlichen Werte der Spiel-Direktiven.

---

## 💾 Kompatibilität

### Spielstände
Das Plugin ändert Gebäudedefinitionen. Ein Spielstand merkt sich aber für jedes gebaute Gebäude dessen Lager. Deshalb gibt es die **Nachrüstung** (`retrofit_storages`, Vorgabe EIN): Beim Laden eines Spielstands bekommt jedes fertige Gebäude die Lager, die ein eingeschalteter Regelsatz seiner Definition hinzufügt und die ihm noch fehlen.

- Nachgerüstet werden Einzellager für eine Ressource (`$STORAGE_IMPORT_SPECIAL`) der Klassen covered, open und gravel. Sie werden leer eingefügt, an der Stelle, an der sie in der Gebäudedefinition stehen – so wie bei einem Neubau.
- Vorhandene Lager bleiben, wie sie sind: nichts wird vergrößert oder entfernt. Baustellen und Gebäude im Abriss bleiben unberührt; ein Gebäude mit einem Fahrzeuglager (Fahrzeugfabrik, Trockendock) wird ausgelassen und im Protokoll genannt – so ein Gebäude musst du neu bauen, damit es ein neues Lager bekommt.
- ⚠️ Beim nächsten normalen Speichern stehen die neuen Lager im Spielstand. Den Regelsatz ausschalten oder die Nachrüstung auf AUS stellen nimmt sie nicht wieder heraus.
- Dafür ist der Loader aus dem Republic Mod Manager nötig (ab b0.4.5-rmm.3). Mit einem anderen Loader gelten neue Lager wie früher nur für Neubauten.

Dateien im Spielordner bleiben unverändert.

### Andere Plugins
Andere Plugins, die dieselbe Gebäudedatei ersetzen, werden nicht mit diesen Änderungen zusammengeführt. Für Materialien aus Vehicle Materials kannst du mit diesem Plugin die passende `$STORAGE_IMPORT_SPECIAL`-Zeile in den Fahrzeugfabriken ergänzen; die mitgelieferte INI enthält dafür keinen fertigen Regelsatz.

### Versionskompatibilität
- **0.4.18:** Fahrzeugfabriken, Trockendock und Raketenteile-Werke (Fertigungslinien) fasst das Nachführen der Lager nur noch an, wenn der Versuchsschalter für Fahrzeugfabriken an ist. Sie führen eigene Lager hinter denen ihrer Definition, und die sollen in Ruhe bleiben.
- **0.4.17:** Auch Ausgangslager (`$STORAGE_EXPORT_SPECIAL`) und alle Güterklassen – zum Beispiel Trockengut und Flüssigkeiten – folgen jetzt ihrer Zeile: neu, größer, kleiner oder entfernt. Nennt eine Zeile eine Klasse, in der die Ressource nichts fasst (etwa Industriesalz in einem Schüttgut-Lager), behält ein schon gebautes Gebäude sein Lager.
- **0.4.16:** Lager folgen ihrer Zeile: Änderst du in einer Lagerzeile die Größe, übernehmen schon gebaute Gebäude sie beim nächsten Laden – größer sofort, kleiner, sobald der Bestand hineinpasst. Änderst du die Klasse (gedeckt, offen, Schüttgut), wird das Lager getauscht, sobald es leer ist.
- **0.4.15:** Entfernst du eine Lagerzeile aus einem Regelsatz oder schaltest den Regelsatz aus, verschwindet das Lager beim nächsten Laden wieder aus den Gebäuden, sobald es leer ist. Ein Lager mit Bestand bleibt, bis es leer ist.
- **0.4.14:** Fehler behoben: Bekam ein Gebäude, das schon stand, beim Laden ein neues Lager aus einem Regelsatz, zeigte es danach „kein Strom“, und das Spiel konnte später abstürzen. Die vorhandenen Lager des Gebäudes bleiben jetzt unangetastet, das neue Lager kommt ans Ende.
- **0.4.13:** Das ausführliche Protokoll listet beim Laden eines Spielstands die Lager der Gebäude auf, denen ein Regelsatz ein Lager gibt – zur Fehlersuche. Am Spiel ändert das nichts.
- **0.4.12:** Ein nachgerüstetes Lager kommt jetzt an dieselbe Stelle wie bei einem Neubau (dort, wo es in der Gebäudedefinition steht) statt ans Ende. Neuer Versuchsschalter `retrofit_vehicle_factories` (Vorgabe AUS) für Fahrzeugfabriken – nur zum Testen an einer Kopie des Spielstands.
- **0.4.11:** Gebäude mit einem Fahrzeuglager (Fahrzeugfabriken, Trockendock) lässt die Nachrüstung aus. Ein Versuch, sie mitzunehmen, hat das Spiel beim Laden abstürzen lassen. Diese Gebäude musst du neu bauen, damit sie ein neues Lager bekommen.
- **0.4.9:** Schon gebaute Gebäude bekommen die Lager deiner Regelsätze nachträglich, wenn du einen Spielstand lädst (neuer Schalter `retrofit_storages`, Vorgabe EIN). Vorher musste man das Gebäude neu bauen. Dafür ist der Loader aus dem Republic Mod Manager ab b0.4.5-rmm.3 nötig.
- **0.4.8:** Mehrere aktive Regelsätze dürfen sich eine Zieldatei teilen, wenn höchstens einer davon mehr tut, als Zeilen mit `add` anzuhängen. So können zum Beispiel mehrere Inhaltspakete derselben Fahrzeugfabrik je ein Lager geben. Zwei Regelsätze, die dieselbe Datei ändern, werden weiter beide abgewiesen.
- **0.4.7:** Texte für WRSR 1.2.0.9; das Plugin nutzt keine Spieladresse und ist sonst unverändert.
- **0.4.6:** Für WRSR 1.2.0.8 und TesmioLoader b0.4.5 neu gebaut; sonst unverändert.
- **0.4.5:** Aus dem Workshop geladen, liest das Plugin jetzt auch die fertige INI, die Republic Mod Manager nach `tesmioloader\plugins\vanilla_buildings.ini` schreibt; vorher blieb es dann bei der INI im Paket
- **0.4.4:** neuer Schlüssel `name` je Regelsatz: ersetzt die vorhandene `$NAME`- oder `$NAME_STR`-Zeile jeder Zieldatei, ohne dass du die alte kennen musst; mit Punkten darin ein Übersetzungsschlüssel über das Plugin Localization
- **0.4.3:** einpunktige Anschlüsse wie `$CONNECTION_ROAD_DEAD` lassen sich mit `add_connection`, `replace_connection` und `remove_connection` bearbeiten (Token | Punkt)
- **0.4.2:** `replace_connection` nimmt hinter dem neuen Token wahlweise zwei neue Punkte und verschiebt den Anschluss dabei
- **0.4.1:** `insert` nimmt als Anker auch Zeilen, die frühere Befehle desselben Regelsatzes erzeugen
- **0.4.0:** erste veröffentlichte Fassung

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Nichts passiert | alle Regelsätze `enabled = 0` (Auslieferung) oder Plugin aus | Regelsatz einschalten, Spiel neu starten |
| `expected buildings_types...` | Zielpfad hat keine unterstützte Form | Pfad wie oben schreiben |
| `source line has 0 matches` | gesuchte Zeile fehlt im Original | vollständige Zeile aus der Originaldatei kopieren |
| `... matches instead of exactly one` | Zeile oder Anschluss nicht eindeutig | Zeile prüfen, bei Anschlüssen die Koordinaten |
| `duplicate target` | Ziel in zwei aktiven Regelsätzen, die es beide ändern | Änderungen in einen Regelsatz zusammenführen; Regelsätze, die nur mit `add` Zeilen anhängen, dürfen sich ein Ziel teilen |
| `could not read building source file` | DLC oder Workshop-Objekt nicht installiert | Pfad und Installation prüfen |
| `[utf8-bom]`, `[duplicate-key]`, `[boolean]` | INI-Format | UTF-8 ohne BOM, Schlüssel einmal, Schalter 0 oder 1 |

### Logging

Alle Meldungen stehen in `tesmioloader.log` und im Detail-Log `logs\tesmioloader.vanilla_buildings.log`, mit Stufe (INFO, DEBUG, WARN, ERROR, FATAL) und Prüfungsname. Jedes abgewiesene Ziel bekommt auch ohne Debug eine `WARN [target-rejected]`-Meldung mit Abschnitt, Ziel, Ursache und bei Befehlsfehlern der INI-Zeile.
- In **Republic Mod Manager** öffnet das Protokoll-Symbol in der Seitenleiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `Configuration file` → welche INI die DLL gewählt hat
- `target-rejected` → abgewiesene Ziele mit Ursache
- `overlay-opened` → das Spiel hat eine geänderte Datei tatsächlich gelesen
- `configuration` → die ganze INI wurde abgelehnt

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo)
```
<Workshop-Objekt>\
├── plugins\
│   ├── vanilla_buildings.dll       (Plugin)
│   └── vanilla_buildings.ini       (Original-INI, Beispiele ausgeschaltet)
├── config\                         (Editor-Schema für Republic Mod Manager)
│   ├── vanilla_buildings.launcher.ini
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

**Loader-Ordner** (nur bei Methode 2 oder mit Republic Mod Manager)
```
<Spiel>\tesmioloader\
├── plugins\
│   ├── vanilla_buildings.dll       (nur bei Methode 2)
│   └── vanilla_buildings.ini       (wirksame INI)
└── user_config\
    └── vanilla_buildings.editor.ini (persönliche Regelsätze aus Republic Mod Manager)
```

Aus dem Workshop geladen, bleibt die DLL im Paket; in `tesmioloader\plugins` liegt dann nur die wirksame INI, sobald du im Republic Mod Manager gespeichert hast.

Die temporären Kopien liegen unter `%TEMP%\TesmioLoader\vanilla_buildings\<Prozess-ID>-<Startkennung>-<Versuch>\`; jeder Start legt einen neuen Ordner an, alte können gefahrlos gelöscht werden.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/vanilla_buildings.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Werden meine Spieldateien verändert?**
A: Nein. Es entstehen nur temporäre Kopien; ohne das Plugin ist alles wie vorher.

**F: Bekommen bestehende Gebäude die neuen Lager?**
A: Ja, beim Laden eines Spielstands, solange `retrofit_storages` auf EIN steht und du den Loader aus dem Republic Mod Manager benutzt. Einzelheiten und Grenzen stehen unter [Spielstände](#spielstände).

**F: Kann ich ein Workshop-Gebäude ändern?**
A: Ja, mit der Workshop-ID als Pfadbeginn, etwa `2496571917\utrzymanie1\building.ini`. Das Objekt muss abonniert und installiert sein.

**F: Was passiert nach einem Spielupdate?**
A: Regeln, deren Originalzeilen sich geändert haben, werden für das betroffene Ziel abgewiesen und im Log gemeldet; alles andere läuft weiter.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Republic Mod Manager zeigt die Regelsätze als Liste mit Zielen und Befehlen, je Zeile ein Eintrag, und legt neue Regelsätze mit dem Plus-Knopf an.

---

**Letzte Aktualisierung:** Vanilla Buildings 0.4.18  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
