# 🚚 Vehicle Materials 0.4.14

**TesmioLoader-Plugin für zusätzliche Fahrzeugmaterialien**

Frei konfigurierbare Zusatzmaterialien für die Fahrzeugproduktion in *Workers & Resources: Soviet Republic* 1.2.0.9: Glas, Kabel, Kupfer oder jede andere Ressource, die Resources Plus oder Resources registriert, getrennt einstellbar für Straße, Schiene, Schiff und Flugzeug.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Produktionsgebäude vorbereiten](#-produktionsgebäude-vorbereiten)
- [Konfiguration](#-konfiguration)
- [Wertebereiche](#-wertebereiche)
- [Fahrzeugtypen zuordnen](#-fahrzeugtypen-zuordnen)
- [Verhältnis zu Resources](#-verhältnis-zu-resources)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.2.0.9
- TesmioLoader ab b0.4.5 (API 4)
- Pflicht: Resources Plus oder Resources (Resources Plus ersetzt Resources) mit allen Ressourcen, die du als Fahrzeugmaterial verwenden willst. Ohne eines der beiden startet Vehicle Materials nicht, der Grund steht im Log. Wo diese Anleitung `resources.ini` nennt, ist mit Resources Plus die `resources_plus.ini` gemeint.

### In drei Schritten
1. **Ressourcen registrieren:** Jedes Material muss in der `resources.ini` stehen, zum Beispiel `glass` oder `cable`. In Republic Mod Manager geht das über den Eintrag Resources Plus oder Resources.
2. **Abonnieren:** Der TesmioLoader findet das Plugin beim nächsten Spielstart von selbst im Workshop-Ordner. Dann die Materialien mit ihren Koeffizienten eintragen und das Plugin einschalten (`enabled = 1`), am bequemsten über Republic Mod Manager.
3. **Spiel starten:** Das Lager für das neue Material bekommen die Fahrzeugfabriken vom Plugin selbst (siehe [Produktionsgebäude vorbereiten](#-produktionsgebäude-vorbereiten)).

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Zusätzliche Materialien für den Bau von Fahrzeugen, ohne die Spieldateien zu verändern
- ✅ Die ursprünglichen Materialanforderungen des Spiels bleiben erhalten
- ✅ Bis zu 32 Materialien, jedes muss durch Resources Plus oder Resources registriert sein
- ✅ Die Konfiguration wird beim Start komplett geprüft; bei einem Fehler bleibt das Plugin aus und das Spiel läuft wie gewohnt

### 🆕 Einstellmöglichkeiten

#### 1️⃣ **Getrennte Koeffizienten je Fahrzeugklasse** (`[road]`, `[rail]`, `[ship]`, `[airplane]`)
- Ein Wert je Material und Klasse, `0` oder fehlend schaltet das Material für die Klasse ab
- Der Koeffizient wird mit dem internen Produktionswert des Fahrzeugs multipliziert: größere Fahrzeuge brauchen mehr

#### 2️⃣ **Persönliches Overlay** (`user_config\vehicle_materials.ini`)
- Die DLL liest zuerst ihre normale INI, dann `tesmioloader\user_config\vehicle_materials.ini` Schlüssel für Schlüssel darüber
- Republic Mod Manager schreibt nur das Overlay; die INI im Workshop-Paket bleibt unangetastet, deine Werte überstehen so jedes Steam-Update

#### 3️⃣ **Zuordnung der Fahrzeugtypen** (`[mapping]`)
- Automatisch: Typ 1 Straße, Typ 6 Schiff, Typ 7 Flugzeug, alle anderen Schiene
- Bei Bedarf je internem Typ 0 bis 15 von Hand übersteuerbar

---

## 💾 Installation

Wähle **eine** der zwei Methoden. Dieselbe DLL darf nie zweimal geladen werden. Resources Plus oder Resources muss in jedem Fall vorhanden und eingeschaltet sein.

---

### Methode 1️⃣: Steam Workshop (empfohlen)

```
1. Workshop-Objekt abonnieren
2. Der TesmioLoader (ab b0.4.5) findet die DLL beim nächsten Spielstart
   von selbst im Workshop-Ordner – nichts wird kopiert
3. Prüfe, dass Resources Plus oder Resources eingeschaltet ist
   und die Materialien in resources.ini stehen
4. Steam-Updates gelten ab dem nächsten Spielstart
```

Ein neu gefundenes Plugin ist im Loader eingeschaltet. Aus- und einschalten kannst du es im TesmioLauncher oder mit „Plugin aktiv“ im Republic Mod Manager. Vehicle Materials wird aber ausgeschaltet und ohne Materialien ausgeliefert (`enabled = 0`, `count = 0`): Es tut erst etwas, wenn du Materialien eingetragen und `enabled = 1` gesetzt hast.

---

### Methode 2️⃣: Von Hand in den Loader-Ordner

```
1. Kopiere vehicle_materials.dll und vehicle_materials.ini aus plugins\
   → <Spiel>\tesmioloader\plugins\

2. Prüfe, dass Resources Plus oder Resources eingeschaltet ist
   und die Materialien in resources.ini stehen
```

Für alle, die das Plugin ohne Steam-Abo benutzen wollen. Eine DLL in `tesmioloader\plugins` hat Vorrang vor der gleichnamigen aus dem Workshop; Updates kopierst du dann selbst nach.

---

## 🧰 Republic Mod Manager

Das Paket enthält im Ordner `config` ein Launcher-Schema. Republic Mod Manager (ab Version 0.6.3) zeigt Vehicle Materials damit in vier Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, der Knopf für diese Anleitung, die Karte „Fahrzeugfabriken“ (Lager anlegen EIN oder AUS, Größe der Lager) und die Fehlersuche (ausführliches Protokoll)
- **Ressourcen:** die Materialliste; der Plus-Knopf bietet nur Ressourcen an, die Resources Plus oder Resources registriert hat, und legt für jede Fahrzeugklasse den Koeffizienten an; der Papierkorb entfernt Material und Koeffizienten wieder
- **Fahrzeugklassen:** die Koeffizienten als Tabelle Material mal Klasse
- **Erweiterte Zuordnung:** die Fahrzeugtypen 0 bis 15

Der Schalter „Plugin aktiv“ im Kopf setzt beim Einschalten auch `enabled = 1`. Persönliche Werte liegen in `tesmioloader\user_config\vehicle_materials.ini`; die ausgelieferte INI bleibt unverändert. Vor einem Start prüft RMM, ob Resources Plus oder Resources eingeschaltet ist; fehlt beides, bietet RMM an, Vehicle Materials für diesen Start auszuschalten. Wer die INI lieber von Hand bearbeitet, findet alles Weitere unten.

---

## 🏗️ Produktionsgebäude vorbereiten

Eine Fabrik kann ein zusätzliches Material nur verwenden, wenn zwei Dinge stimmen: Sie hat ein Lager dafür, und das Material steht bei ihr als Verbrauch. Fehlt der Verbrauch, zeigt die Fabrik das Lager zwar an, aber die Lkw laden nicht ab und es wird nichts gebaut. **Beides gibt das Plugin den Fabriken selbst. Du musst kein Gebäude anpassen.**

- Das gilt für die Fabriken des Spiels, der DLCs und aus dem Workshop, für Straße, Schiene, Schiff und Flugzeug.
- Neue Fabriken haben die Lager von Anfang an. Fabriken, die schon stehen, bekommen sie leer dazu, wenn du den Spielstand lädst (dafür brauchst du den TesmioLoader aus dem Republic Mod Manager).
- Jedes Lager fasst 50 Tonnen. Die Größe stellst du mit `factory_storage_capacity` ein.
- Nimmst du ein Material wieder aus der Liste (oder stellst den Fahrzeugbau eines Pakets auf AUS), nimmt das Plugin sein Lager beim nächsten Laden des Spielstands aus den Fabriken heraus. Das geht nur, wenn das Lager leer ist; ein Lager mit Bestand bleibt, bis es leer ist.
- Hat eine Fabrik schon ein eigenes Lager für das Material, aus ihrer `building.ini` oder aus einem Regelsatz von Vanilla Buildings, bleibt es, wie es ist.
- An den Dateien des Spiels ändert sich nichts. Das Plugin trägt alles nur im Speicher ein.

**Lager lieber selbst anlegen?** Mit `factory_storages = 0` legt das Plugin keine Lager an. Dann braucht jede Fabrik für jedes Material eine eigene Zeile in ihrer `building.ini`:

```ini
$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_OPEN 100 glass
```

- `100` ist die Lagerkapazität
- `glass` ist die genaue Ressourcen-ID aus resources.ini
- der Transporttyp muss zur Transportklasse der Ressource passen; im Beispiel ist glass als OPEN registriert

Den Eintrag als Verbrauch macht das Plugin auch dann selbst.

---

## ⚙️ Konfiguration

### Hauptdatei: `vehicle_materials.ini`

Die DLL liest in dieser Reihenfolge:
- **Basis:** `tesmioloader\plugins\vehicle_materials.ini`, falls vorhanden, sonst die INI neben der DLL (im Workshop-Paket `plugins\vehicle_materials.ini`)
- **Overlay:** `tesmioloader\user_config\vehicle_materials.ini`, Schlüssel für Schlüssel darüber; hierhin schreibt Republic Mod Manager

⚠️ **Kommentare nur in eigenen Zeilen mit `;`.** Ein Kommentar hinter dem Wert wird als Teil des Wertes gelesen und lehnt die ganze Konfiguration ab. Die Datei muss UTF-8 ohne BOM sein; alle sieben Abschnitte müssen vorhanden sein, auch leere; unbekannte oder doppelte Abschnitte und Schlüssel, leere Werte und ungültige Zahlen lehnen die ganze Konfiguration ab. Kein Neuladen zur Laufzeit: INI ändern, Spiel neu starten.

### Abschnitt `[general]`

```ini
[general]
; 1 aktiviert das Plugin, 0 schaltet es ab; nur genau 0 oder 1
enabled = 0
; 1 schreibt Materialien und Fahrzeugbauten ins Detail-Log tesmioloader.vehicle_materials.log
debug = 0
; Obergrenze für wiederholte Warnungen und Diagnosemeldungen je Sitzung, 0 unterdrückt sie
debug_limit = 80
```

### Abschnitt `[resources]` und die vier Fahrzeugklassen

Beispiel mit zwei Materialien, so wie Republic Mod Manager es anlegt:

```ini
[resources]
; Anzahl der Einträge resource0, resource1, ...
count = 2
; genaue Ressourcen-ID aus resources.ini, jede nur einmal
resource0 = glass
resource1 = cable

[road]
; Koeffizient je Material; 0 oder fehlend = kein Bedarf in dieser Klasse
glass = 0.020
cable = 0.010

[rail]
glass = 0.030
cable = 0.015

[ship]
glass = 0.010
cable = 0.005

[airplane]
glass = 0.015
cable = 0.020

[mapping]
; -1 = automatisch, 0 = Straße, 1 = Schiene, 2 = Schiff, 3 = Flugzeug
type0 = -1
type1 = -1
type2 = -1
type3 = -1
type4 = -1
type5 = -1
type6 = -1
type7 = -1
type8 = -1
type9 = -1
type10 = -1
type11 = -1
type12 = -1
type13 = -1
type14 = -1
type15 = -1
```

Dabei gilt:
- die Nummerierung beginnt bei `resource0`, `count` muss zur Anzahl passen, Einträge ab `count` werden abgewiesen
- Namen sind ohne Groß- und Kleinschreibung eindeutig und dürfen keine Leerzeichen, Pfadzeichen oder Sonderzeichen enthalten
- ein Schlüssel in einer Fahrzeugklasse muss unter `[resources]` stehen
- ein Material ohne positiven Wert in mindestens einer Klasse wird ignoriert; mit `enabled = 1` verlangt Republic Mod Manager mindestens ein solches Material
- ein Material, das ein Fahrzeug bereits verlangt, wird nicht ein zweites Mal hinzugefügt

Beginne mit kleinen Werten und kontrolliere die Mengen im Spiel.

---

## 📏 Wertebereiche

Die DLL prüft diese Grenzen beim Start. Ein Wert außerhalb lehnt die ganze Konfiguration ab; das Plugin installiert dann keinen Hook, das Spiel läuft mit seinen ursprünglichen Materialien.

| Schlüssel | Bereich | Standard |
|---|---|---|
| `enabled`, `debug` | genau 0 oder 1 | 0 / 0 |
| `debug_limit` | 0 bis 10000 | 80 |
| `count` | 0 bis 32 | 0 |
| `resource0` … `resource31` | ID aus resources.ini, höchstens 63 Zeichen | keine |
| Koeffizienten in `[road]`, `[rail]`, `[ship]`, `[airplane]` | endliche Zahl 0 bis 1000000 | keine |
| `type0` … `type15` | -1 bis 3 | -1 |
| Datei | höchstens 1 MiB, Schlüssel und Werte höchstens 63 Zeichen | |

---

## 🔀 Fahrzeugtypen zuordnen

Das Spiel führt Fahrzeuge unter internen Produktionstypen. Das Plugin ordnet sie so zu:

| Interner Typ | Klasse |
|---|---|
| 1 | Straße |
| 6 | Schiff |
| 7 | Flugzeug |
| alle anderen | Schiene |

`[mapping]` übersteuert das je Typ 0 bis 15:

| Wert | Klasse |
|---|---|
| -1 | automatisch |
| 0 | Straße |
| 1 | Schiene |
| 2 | Schiff |
| 3 | Flugzeug |

Normalerweise bleibt der Abschnitt unverändert. Mit `debug = 1` steht im Detail-Log, welcher Typ welcher Klasse zugeordnet wurde.

---

## 🔗 Verhältnis zu Resources

Vehicle Materials legt keine Ressource an. Es fragt beim Start den Registrierungsdienst von Resources Plus oder Resources und verwendet nur Namen, die dort veröffentlicht sind:

- fehlen beide oder sind sie ausgeschaltet, startet Vehicle Materials nicht, der Grund steht im Log
- ein Material, das in resources.ini fehlt, lehnt die Konfiguration ab
- der Name muss in beiden INIs exakt gleich geschrieben sein

Entfernst du ein Material im Republic Mod Manager aus der Ressourcenliste, bereinigt er nach Rückfrage beim Speichern auch die Einträge in Vehicle Materials.

---

## 💾 Kompatibilität

### Spielstände

Das Plugin verändert keine Spieldateien und speichert nichts im Spielstand. Materialien, die Fahrzeuge verlangen, sind Teil der Ressourcenliste des Spielstands: Eine Ressource, die bereits in einem Spielstand verwendet wird, darf in resources.ini nicht mehr entfernt werden.

### Versionskompatibilität

- **0.4.14:** Im Republic Mod Manager haben die Raketenteile auf der Seite Fahrzeugklassen ein eigenes Raketen-Symbol statt des Flugzeugs (ab RMM 0.8.12, ein älterer zeigt dort ein Zahnrad); am Plugin ändert sich nichts.
- **0.4.13:** Flugzeuge rechnen wieder mit den Flugzeug-Werten – seit dem Raketen-DLC hat das Spiel die Fahrzeugarten neu nummeriert, und Flugzeuge liefen mit den Schienen-Werten. Neue fünfte Klasse **Raketenteile** mit eigenen Werten (Reiter „Fahrzeugklassen“); ein Material ohne Raketen-Wert verlangt von Raketenteilen nichts. Kutschen und andere Fahrzeuge mit Pferden brauchen kein Zusatzmaterial mehr, und ihre Werkstätten (Kutschenbau, Gestüt) bekommen kein Lager dafür – was sie schon hatten, verschwindet, sobald es leer ist.
- **0.4.12:** Intern: neu gebaut mit der gemeinsamen Lager-Routine; am Verhalten ändert sich nichts.
- **0.4.11:** Intern: neu gebaut mit der gemeinsamen Lager-Routine; am Verhalten ändert sich nichts.
- **0.4.10:** Änderst du die Größe der Lager (`factory_storage_capacity`), übernehmen schon gebaute Fabriken sie beim nächsten Laden – größer sofort, kleiner, sobald der Bestand hineinpasst.
- **0.4.9:** Intern aufgeräumt; am Verhalten ändert sich nichts.
- **0.4.8:** Ein leeres Lager verschwindet jetzt auch dann, wenn dahinter noch das Lager eines anderen Materials steht.
- **0.4.7:** Das Lager eines Materials, das nicht mehr in der Liste steht, verschwindet wieder aus den Fabriken, sobald es leer ist. Materialien aus Inhaltspaketen stehen jetzt als gewöhnliche Einträge in der Liste (Republic Mod Manager ab 0.8.4); die eigene Paketdatei von 0.4.4 gibt es nicht mehr.
- **0.4.6:** Fabriken, die schon stehen, bekommen ihr neues Lager jetzt so, dass sie ihren Strom behalten und sofort weiterbauen.
- **0.4.5:** Die Fahrzeugfabriken bekommen das Lager und den Eintrag als Verbrauch für jedes Material vom Plugin selbst, auch Fabriken aus dem Workshop und Fabriken, die schon stehen. Vorher brauchte jede Fabrik dafür eigene Zeilen in ihrer Gebäudedatei. Neue Einstellungen `factory_storages` und `factory_storage_capacity`.
- **0.4.4:** Inhaltspakete können ein eigenes Material mitbringen. Schaltest du das im Republic Mod Manager auf der Seite des Pakets ein (Karte „Fahrzeugbau“), liest das Plugin die Datei des Pakets aus dem Ordner `vehicle_materials.d` und hängt das Material hinter deine eigene Liste. Mit einer solchen Datei läuft das Plugin auch dann, wenn du es nie selbst eingeschaltet hast; hast du „Plugin aktiv“ selbst auf AUS gestellt, bleibt es aus.
- **0.4.3:** Für WRSR 1.2.0.9 (Hotfix zum Raketen-DLC); eine andere Spielversion wird abgelehnt. Sonst unverändert.
- **0.4.2:** Für WRSR 1.2.0.8 mit dem Raketen-DLC; eine andere Spielversion wird abgelehnt. Sonst unverändert.
- **0.4.1:** Für WRSR 1.1.1.13; eine andere Spielversion wird abgelehnt. Das Plugin prüft vor dem Start zwei weitere Stellen im Spiel; passt eine nicht, bleibt es aus und das Spiel läuft wie gewohnt
- **0.4.0:** erste veröffentlichte Fassung; die Konfiguration wird als Basis plus persönliches Overlay gelesen

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Plugin startet nicht | Resources Plus und Resources fehlen oder sind ausgeschaltet | eines der beiden abonnieren und im TesmioLauncher oder Republic Mod Manager einschalten |
| Konfiguration abgelehnt | Material nicht in resources.ini, Kommentar hinter einem Wert, Wert außerhalb des Bereichs | Log lesen, Namen und Werte korrigieren |
| Fahrzeug verlangt das Material nicht | `enabled = 0` oder Koeffizient 0 in dieser Klasse | „Plugin aktiv“ einschalten, Koeffizient setzen |
| Fabrik hat kein Lager für das Material | `factory_storages = 0`, oder die Fabrik stand schon und der Spielstand wurde ohne den TesmioLoader aus dem Republic Mod Manager geladen | `factory_storages = 1` setzen; den Loader aus dem Republic Mod Manager benutzen |
| Falsche Fahrzeugklasse | interner Typ anders als erwartet | `debug = 1`, Zuordnung in `[mapping]` setzen |

### Logging

Alle Meldungen stehen in `tesmioloader.log` und im Detail-Log `logs\tesmioloader.vehicle_materials.log`; mit `debug = 1` kommen in beiden die Materialien und Fahrzeugbauten dazu.
- In **Republic Mod Manager** öffnet das Protokoll-Symbol in der Seitenleiste die Protokollansicht mit Filter und Absender.

Erwartet bei erfolgreichem Start:
```
vehicle_materials  INFO: - [status] Hook active at SOVIET64.exe+0x3F8C50
vehicle_materials  INFO: - [status] Automatic mapping: type1/2=road, type6=ship, type8/10=airplane, type7/9=rocket, other=rail; horse-drawn vehicles take no extra material
vehicle_materials  INFO: - [status] v0.4.1 ready
```

Suche nach:
- `vehicle_materials` → alle Meldungen des Plugins
- `FATAL` → Ablehnungen mit Ursache und Handlungsempfehlung
- `Personal overlay applied` → das Overlay aus user_config wurde gelesen

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo)
```
<Workshop-Objekt>\
├── plugins\
│   ├── vehicle_materials.dll       (Plugin)
│   └── vehicle_materials.ini       (Original-INI, ohne Materialien, enabled = 0)
├── config\                         (Launcher-Schema für Republic Mod Manager)
│   ├── vehicle_materials.launcher.ini
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
│   ├── vehicle_materials.dll       (nur bei Methode 2)
│   └── vehicle_materials.ini       (nur bei Methode 2, Original-INI)
└── user_config\
    └── vehicle_materials.ini       (persönliche Werte aus Republic Mod Manager)
```

Aus dem Workshop geladen, bleiben DLL und Original-INI im Paket. Die persönlichen Werte liegen in jedem Fall in `user_config`.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/WRSR-MODS/tree/main/plugins/vehicle_materials.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Kann ich Materialien verwenden, die nicht in resources.ini stehen?**
A: Nein. Vehicle Materials verwendet nur Ressourcen, die Resources Plus oder Resources registriert hat.

**F: Warum liefert niemand das Material an die Fabrik?**
A: Dann fehlt der Fabrik das Lager für das Material. Das Plugin legt es selbst an, solange `factory_storages = 1` steht; das ist die Vorgabe. Im Protokoll steht dazu eine Zeile, die mit „Factories“ beginnt.

**F: Werden die ursprünglichen Materialien ersetzt?**
A: Nein. Stahl, mechanische Bauteile und alles andere bleiben; die neuen Materialien kommen dazu.

**F: Was passiert bei einem Fehler in der INI?**
A: Das ganze Plugin wird abgelehnt und installiert keinen Hook. Das Spiel baut Fahrzeuge wie ohne Plugin, der Grund steht im Log.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Republic Mod Manager bietet die Materialien aus resources.ini mit + an, legt die Koeffizienten je Klasse an und prüft die Wertebereiche.

---

**Letzte Aktualisierung:** Vehicle Materials 0.4.14  
**Für:** WRSR 1.2.0.9 | TesmioLoader API 4
