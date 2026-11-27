# Republic Mod Manager – Anleitung

[English](README_EN.md) | **Deutsch**

Republic Mod Manager (kurz RMM) ist ein Fenster für alle deine TesmioLoader-Plugins. Du siehst, welche Plugins du hast, schaltest sie ein oder aus, stellst ihre Werte mit Beschreibung und Bereichsprüfung ein und startest das Spiel. RMM selbst schreibt nur Einstellungsdateien; die korrigierte `tesmioloader.dll` setzt der Installer des Workshop-Pakets ein.

---

## 📋 Inhaltsverzeichnis

1. [Voraussetzungen](#-voraussetzungen)
2. [Das Fenster](#-das-fenster)
3. [Der Schalter „Plugin aktiv“](#-der-schalter-plugin-aktiv)
4. [Speichern und Starten](#-speichern-und-starten)
5. [Die Karte „Hinweise“](#-die-karte-hinweise)
6. [Wie ein Plugin ins Spiel kommt](#-wie-ein-plugin-ins-spiel-kommt)
7. [Ersatz-Plugins und Abhängigkeiten](#-ersatz-plugins-und-abhängigkeiten)
8. [Inhaltspakete](#-inhaltspakete-ressourcen-vorkommen-gebäude)
9. [Vor dem Spielstart](#-vor-dem-spielstart)
10. [Spielstände](#-spielstände)
11. [Das Protokollfenster](#-das-protokollfenster)
12. [Das Einstellungsfenster](#-das-einstellungsfenster)
13. [Profile und Wiederherstellung](#-profile-und-wiederherstellung)
14. [Listen-Editoren: Resources, Needs, Deposits und Paket-Editoren](#-listen-editoren)
15. [Erzeugte Gebäude](#️-erzeugte-gebäude)
16. [Spielversion und rmm.ini](#-spielversion-und-rmmini)
17. [Wo deine Dateien liegen](#-wo-deine-dateien-liegen)
18. [Wenn etwas nicht klappt](#-wenn-etwas-nicht-klappt)
19. [Für Plugin-Autoren](#-für-plugin-autoren)

---

## 🚀 Voraussetzungen

- *Workers & Resources: Soviet Republic* 1.2.0.9
- TesmioLoader von MaxLegend ab b0.4.6 im Spielordner (Ordner `tesmioloader` mit `tesmioloader.dll`, `tesmiolauncher.exe` und `tesmioloader.ini`). RMM bringt eine korrigierte Fassung der `tesmioloader.dll` mit (b0.4.6-rmm.1, gleiche API). Mit ihr gelten deine persönlichen Werte auch bei fremden Plugins, und nur mit ihr bekommen schon gebaute Gebäude neue Lager von Vehicle Materials, Vanilla Buildings und Buildings Plus; mit einer anderen Fassung warnt RMM dich
- Windows 10 oder 11

Andere Lader wie Soviet Mod Loader oder die Workshop Bridge brauchst du nicht; RMM arbeitet nur mit dem TesmioLoader.

RMM liegt als `rmm.exe` im Ordner `tesmioloader`, neben der `tesmioloader.dll`. Starte es von dort oder über die Desktop-Verknüpfung. Es findet den Loader, das Spiel und den Steam-Workshop-Ordner von selbst. Nur ein RMM-Fenster ist gleichzeitig offen; ein zweiter Start holt das vorhandene nach vorn.

---

## 🖥️ Das Fenster

**Links die Liste.** Jede Zeile ist ein Plugin, das der TesmioLoader beim Spielstart findet: jede Plugin-DLL in `tesmioloader\plugins` und jede in einem abonnierten Workshop-Paket. Dazu kommen Inhaltspakete ohne DLL. Das Symbol zeigt die Herkunft: Steam-Symbol für Workshop-Pakete, TesmioLauncher-Symbol für Plugins im plugins-Ordner, Zahnrad für alles andere. Ein grüner Punkt heißt: Dieses Plugin läuft beim nächsten Spielstart. Ein oranger Punkt heißt: Sein Ersatz-Plugin ist auch eingeschaltet, dieses hier wird beim nächsten Start ausgeschaltet (siehe [Ersatz-Plugins und Abhängigkeiten](#-ersatz-plugins-und-abhängigkeiten)). Ein gelbes „Update“ heißt: Das Workshop-Paket hat sich geändert, seit du zuletzt gespeichert hast. Ein bernsteinfarbener Punkt heißt: Hier hast du etwas geändert und noch nicht gespeichert.

**Unter dem Suchfeld vier Filter:** „Alle“, „Aktiv“, „Probleme“ und „Updates“. Sie zeigen genau die Einträge, auf die das zutrifft — steht ein Plugin nicht unter „Probleme“, hat es auch keins. Nur ein Eintrag mit ungespeicherten Änderungen bleibt in jedem Filter stehen; der bernsteinfarbene Punkt sagt dir, warum. Die offene Seite bleibt geöffnet, auch wenn ihr Eintrag gerade herausgefiltert ist; „Alle“ holt ihn zurück.

Rechts neben dem Suchfeld sitzt der Aktualisieren-Knopf: Er liest die Paketordner neu, wenn du zwischendurch etwas abonniert oder kopiert hast.

**Oben der Kopf.** Name, Version und Beschreibung des Plugins, rechts der Schalter „Plugin aktiv“ und die Sprache der Oberfläche. RMM spricht Deutsch und Englisch; „Automatisch“ nimmt Deutsch, wenn Windows auf Deutsch läuft, sonst Englisch. Die Seiten der Plugins, die ihre Einstellungen vom RMM mitbekommen (etwa die Plugins von MaxLegend, Buildings Plus, Resources Plus und Needs Plus), gibt es zusätzlich auf Bulgarisch, Tschechisch, Spanisch, Französisch, Ungarisch, Italienisch, Japanisch, Koreanisch, Polnisch, Portugiesisch (Brasilien), Rumänisch, Russisch, Slowakisch, Serbisch, Türkisch, Ukrainisch sowie Chinesisch (vereinfacht und traditionell). Du siehst sie, wenn Windows in einer dieser Sprachen läuft und „Automatisch“ eingestellt ist; der Rest des Fensters bleibt dann englisch.

**Darunter die Reiter.** Die Reiter kommen vom Plugin. Ein Plugin ohne eigene Einstellungsseite bekommt einen Reiter „Allgemein“, den RMM aus den Kommentaren der INI-Datei baut.

**Die Statuszeile** unter den Reitern sagt, ob der TesmioLoader das Plugin lädt (Ladeweg), wo seine DLL liegt (Workshop oder plugins\) und wann der Loader es zuletzt geladen hat. Fahre mit der Maus über einen Punkt, dann erklärt ein Hinweis, was er bedeutet. Ist zu einem Plugin auch sein Ersatz-Plugin eingeschaltet, leuchtet statt des Ladewegs ein oranger Punkt „Wird ersetzt“.

**Die Karten** tragen die Einstellungen. Jede Zeile hat einen Titel, einen kurzen Text, was die Einstellung im Spiel verändert, und rechts das Feld:

- Schalter für EIN und AUS.
- Zahlenfelder mit Plus und Minus. Fahre mit der Maus über das Feld, dann siehst du den erlaubten Bereich. Ein Dezimalkomma darfst du tippen, gespeichert wird mit Punkt.
- Auswahllisten, Textfelder und mehrzeilige Felder mit Zähler.
- Ein Zurücksetzen-Knopf erscheint nur, wenn dein Wert vom Standard des Plugins abweicht.

Rechtsklick auf einen Text kopiert ihn in die Zwischenablage, praktisch für Fehlerberichte.

**Die Fußzeile** zeigt den Zustand: grün „Gespeichert“, bernstein „Ungespeicherte Änderungen“, rot „Konfiguration ungültig“ mit dem Grund. Daneben die Knöpfe Zurücksetzen, Speichern und Speichern + Starten. Speichern ist nur anklickbar, wenn es etwas Gültiges zu speichern gibt. Hast du mehrere Plugins geändert, nennt die Fußzeile sie alle.

Das Fenster merkt sich Größe, Sprache, gewähltes Plugin und Reiter. Es braucht mindestens 1560 mal 760 Punkte; bei schmalen Fenstern rutschen die Beschriftungen über die Felder.

---

## 🔛 Der Schalter „Plugin aktiv“

Der Schalter im Kopf entscheidet, ob das Plugin beim nächsten Spielstart läuft. Er ist das Kästchen des TesmioLaunchers, also der Eintrag `[plugins] <name>` in `tesmioloader.ini`; `<name>` ist der Dateiname der DLL ohne `.dll`.

- **Kein Eintrag heißt EIN.** Der Loader lädt alles, was er findet und was nicht ausgeschaltet ist. Ein frisch abonniertes Plugin läuft deshalb beim nächsten Start gleich mit.
- **Beim Einschalten** setzt RMM zusätzlich das Feld `enabled` in den Werten des Plugins auf 1, falls es eines gibt, damit die DLL nicht sofort wieder ablehnt.
- **Gleiche DLL an mehreren Orten:** Der Loader lädt nur eine davon, und der Schalter gilt für diese.
- **Ersatz-Plugins:** Schaltest du ein Plugin ein, das ein anderes ersetzt (oder umgekehrt), schaltet RMM das andere beim Speichern aus.

Der Schalter zeigt „an“ nur, wenn alles zusammenpasst, genau wie der grüne Punkt in der Liste. Inhaltspakete ohne DLL (Ressourcen, Vorkommen, Gebäude) haben statt dessen den Schalter „Im Spiel bereitstellen“.

---

## 💾 Speichern und Starten

**Speichern** schreibt deine Änderungen:

- Bei einem Plugin aus dem Workshop landen deine persönlichen Werte in `tesmioloader\user_config\<name>.ini`. Die INI im Workshop-Ordner bleibt, wie der Autor sie liefert; dort schreibt RMM nie etwas. Ein Update des Pakets überschreibt deine Werte deshalb nie, und das Spiel lädt die neue Fassung von selbst. Manche Plugins lesen `user_config` nicht selbst; verlangt ihr Paket es, schreibt RMM zusätzlich die fertige INI nach `tesmioloader\plugins\<name>.ini`, wo das Plugin sie liest.
- Bei Plugins, die als DLL im plugins-Ordner liegen, sichert RMM beim ersten Speichern die vorgefundene INI als Original unter `user_config\.autoload\<name>.upstream.ini` und schreibt dann die fertige INI mit deinen Werten nach `plugins\`. „Original wiederherstellen“ in der Fußzeile holt die gesicherte Datei byteweise zurück.
- Der Schalter „Plugin aktiv“ landet in `tesmioloader.ini`. RMM ändert dort nur die Zeile des Plugins, alles andere bleibt stehen.
- Vor jedem Speichern legt RMM einen Wiederherstellungspunkt an (siehe [Profile und Wiederherstellung](#-profile-und-wiederherstellung)).
- Eine DLL kopiert oder verändert RMM nie.

Speichern geht nur, wenn Spiel und TesmioLauncher beendet sind und alle Werte im erlaubten Bereich liegen. Das Öffnen, Suchen und Wechseln von Plugins oder Reitern ändert keine deiner Einstellungen. Eine Ausnahme gibt es: Fehlt bei einem Listen-Plugin (etwa Resources Plus) die Listen-Datei in `tesmioloader\plugins`, legt RMM sie an, damit es etwas zu bearbeiten gibt; ein blauer Hinweis nennt die Quelle.

**Mehrere Plugins auf einmal:** Du musst nicht nach jedem Plugin speichern. Wechsle einfach zum nächsten Eintrag, deine Änderungen bleiben im Speicher, und der Eintrag bekommt einen bernsteinfarbenen Punkt in der Liste. Die Fußzeile zählt alle ungespeicherten Plugins auf. Speichern oder Speichern + Starten schreibt sie dann alle auf einmal, eins nach dem anderen. Stimmt in einem Plugin ein Wert nicht, bleibt Speichern gesperrt, und die Fußzeile sagt, in welchem. Zurücksetzen gilt immer nur für das Plugin, das gerade offen ist. Beim Schließen von RMM fragt es noch einmal nach: alle speichern, alle verwerfen oder abbrechen.

**Speichern + Starten** speichert genauso und startet dann das Spiel über `tesmiolauncher.exe` ohne dessen Fenster. RMM schließt sich dabei, wenn du es nicht ausdrücklich offen lässt (siehe unten). Willst du das Launcher-Fenster sehen, setze in `rmm.ini` unter `[settings]` den Wert `tesmiolauncher_window = 1`.

Nach dem Start schaut RMM dem Spiel noch etwa 15 Sekunden zu und schließt sich erst dann. Beendet sich das Spiel **sofort** wieder — in den ersten fünf Sekunden, bevor es überhaupt geladen hat —, sagt RMM dir das und nennt den häufigsten Grund: einen Steam-Client, der zwar läuft, gerade aber kein Spiel annimmt, das ausserhalb von Steam startet; dann hilft nur, Steam ganz zu beenden und neu zu starten. Machst du das Spiel selbst wieder zu, und sei es gleich nach dem Ladebildschirm, bleibt RMM still und schließt sich einfach. Willst du das alte Verhalten (RMM schließt sofort), setze in `rmm.ini` unter `[settings]` den Wert `launch_watch_seconds = 0`.

**Soll RMM gar nicht zugehen,** setze im Einstellungsfenster den Schalter „Fenster offen lassen“ (oder `keep_open = 1` in der `rmm.ini`). Dann startet das Spiel wie immer, und RMM bleibt stehen, bis du es selbst schliesst — praktisch, wenn du beim Laden noch etwas nachsehen willst. Die Fußzeile sagt dir dann, dass das Spiel läuft. Die Warnung bei einem sofortigen Abgang kommt weiterhin.

Vor dem Start prüft RMM außerdem drei Dinge:

- **Ressourcen:** Gibt es alle Ressourcen, auf die eingeschaltete Plugins verweisen, in Resources Plus oder Resources? Fehlt eine, nennt die Meldung das Plugin, und der Start wartet.
- **Ersatz-Paare:** Sind ein Plugin und sein Ersatz beide eingeschaltet, schaltet RMM das Original aus und sagt es dir.
- **Abhängigkeiten:** Fehlt einem eingeschalteten Plugin etwas, das es braucht, fragt ein Fenster nach. „Ausschalten und starten“ schaltet das betroffene Plugin aus (einschalten kannst du es später wieder), „Trotzdem starten“ lässt alles, wie es ist.

---

## 💡 Die Karte „Hinweise“

Auf dem ersten Reiter jedes Plugins steht oben eine Karte „Hinweise“. Dort erscheint alles, was du wissen solltest, bevor du speicherst:

- **Blau:** Informationen, etwa ein Hinweis des Plugin-Autors, ein Update des Pakets mit alter und neuer Version, dass eine Abhängigkeit aktiv ist oder wo deine persönlichen Werte liegen.
- **Gelb:** Warnungen, etwa eine Abhängigkeit, die zwar da, aber ausgeschaltet ist, ein Ersatz-Plugin, das ebenfalls eingeschaltet ist, oder eine DLL, die es mehrfach gibt (mit der Angabe, welche der Loader lädt).
- **Rot:** Fehler, etwa eine fehlende Abhängigkeit oder ein Workshop-Ordner, der bei RMM und beim Loader nicht derselbe ist.

Speichern kannst du auch mit einem gelben oder roten Feld. Fehlt einem Plugin etwas, das es braucht, fragt RMM vor „Speichern + Starten“ nach.

---

## 🛤️ Wie ein Plugin ins Spiel kommt

Der TesmioLoader sucht seine Plugins bei jedem Spielstart selbst, an zwei Orten:

1. **Im Ordner `tesmioloader\plugins`:** jede Plugin-DLL, die direkt dort liegt.
2. **Im Steam-Workshop-Ordner des Spiels:** jede Plugin-DLL in einem abonnierten Paket, egal in welchem Unterordner.

RMM zeigt genau das, was der Loader findet, und kopiert nie eine DLL. Abonnieren reicht also: Steam hält die Dateien aktuell, und beim nächsten Spielstart ist das Plugin dabei. Abbestellen nimmt es wieder heraus.

**Gleiche DLL an mehreren Orten.** Von jedem Dateinamen lädt der Loader nur eine DLL. Eine DLL in `plugins\` gewinnt gegen eine gleichnamige aus dem Workshop; unter mehreren Kopien im Workshop gewinnt die mit dem kleinsten Pfad. RMM zeigt dafür nur einen Eintrag und nennt in der Karte „Hinweise“, welche Datei geladen und welche übergangen wird.

**Deine Werte bleiben vom Paket getrennt.** Die INI neben einer Workshop-DLL ist das Original des Autors und wird nie geändert. Deine persönlichen Werte liegen in `tesmioloader\user_config\<name>.ini`, und beim Spielstart gelten sie vor dem Original. Dafür braucht es die `tesmioloader.dll`, die RMM mitbringt; mit einer anderen Fassung warnt RMM, dass deine Werte bei fremden Plugins nicht ankommen.

**Listen wie Ressourcen, Bedürfnisse und Vorkommen** liegen immer in `tesmioloader\plugins\<name>.ini`, auch wenn die DLL aus dem Workshop kommt – dort lesen diese Plugins ihre Liste. Gibt es die Datei noch nicht, legt RMM sie an: bei einem Ersatz-Plugin aus der Liste seines Originals, sonst aus der INI des Pakets. Die Quelle bleibt unverändert.

**Reihenfolge.** Geladen wird nach dem Namen der DLL von A bis Z, egal wo sie liegt. Die fertige Liste zeigt die Seite [Vor dem Spielstart](#-vor-dem-spielstart).

**Ein anderer Workshop-Ordner.** Normalerweise suchen Loader und RMM im Steam-Workshop-Ordner deines Spiels. Stellst du in `rmm.ini` einen anderen Ordner ein, muss der Loader denselben kennen (`workshop_path` unter `[tesmioloader]` in `tesmioloader.ini`). Sonst zeigt RMM Plugins, die das Spiel nie lädt; ein rotes Feld sagt dir das.

---

## 🔁 Ersatz-Plugins und Abhängigkeiten

**Ersatz-Plugins.** Manche Plugins ersetzen ein anderes: Sie tun dasselbe, beheben Fehler oder können mehr. Beide zusammen dürfen nicht laufen. RMM kennt diese Paare:

| Ersatz | ersetzt |
|---|---|
| Resources Plus | Resources |
| Needs Plus | Needs |
| Deposits Plus | Deposits |

- Schaltest du eines der beiden ein, schaltet RMM das andere beim Speichern aus. Beide Einträge bleiben in der Liste, du kannst jederzeit zurückwechseln.
- Sind beide eingeschaltet, etwa gleich nach dem Abonnieren, leuchtet das Original orange („Wird ersetzt“). Beim nächsten Start über RMM gewinnt das Ersatz-Plugin, das Original wird ausgeschaltet. Willst du lieber das Original, schalte vorher das Ersatz-Plugin aus.
- Startest du das Spiel ohne RMM, passen diese Ersatz-Plugins selbst auf: Solange ihr Original geladen ist, bleiben sie untätig.

Ein Paket meldet sein Original mit `replaces` in seinem Manifest. Für Plugins ohne diese Angabe führt RMM die Liste `settings_schemas\replacements.ini`.

**Abhängigkeiten.** Ein Paket kann sagen, dass es ein anderes braucht (Research Expansion braucht zum Beispiel Localization). Die Karte „Hinweise“ zeigt, ob das Gebrauchte da und eingeschaltet ist. Ein Ersatz-Plugin zählt dabei auch für sein Original: Wer Resources braucht, ist mit Resources Plus zufrieden. Speichern geht immer; fehlt beim Start etwas, bietet RMM an, das betroffene Plugin auszuschalten.

---

## 📦 Inhaltspakete (Ressourcen, Vorkommen, Gebäude)

Manche Workshop-Pakete bringen keine DLL mit, sondern nur Inhalte: neue Ressourcen, Vorkommen, Bedürfnisse oder Gebäude als INI-Abschnitte, dazu Dateien wie Symbole und Modelle. Solche Pakete erkennst du in der Liste daran, dass ihre Seite keine Einstellungen hat, sondern nur einen Schalter „Im Spiel bereitstellen“.

Einschalten und Speichern macht drei Dinge:

- Die Einträge wandern in die INIs der zuständigen Plugins: Ressourcen zu **Resources Plus**, Vorkommen zu **Deposits Plus**, Bedürfnisse zu **Needs Plus**, Gebäude zu **Buildings Plus**. Läuft bei dir statt eines Plus-Plugins das Original (Resources, Deposits, Needs), nimmt RMM dessen INI.
- Die mitgelieferten Dateien landen unter `tesmioloader\vfs`, wo das Spiel sie statt seiner eigenen liest.
- Bei Vorkommen vergibt RMM die Typnummer selbst, damit sie sich nie mit einem anderen Vorkommen beisst. Eine einmal vergebene Nummer bleibt.

In den Editoren erscheinen die Einträge als Originale mit Schloss: du kannst ihre Werte für dich überschreiben, aber den Eintrag selbst nicht löschen, nur ausblenden.

**Ausschalten.** Schaltest du ein Paket wieder aus, nimmt RMM beim Speichern seine Einträge und Dateien heraus. Bringt das Paket Ressourcen mit, fragt RMM vorher nach: Ein Spielstand, der mit diesen Ressourcen gespeichert wurde, lässt sich danach nicht mehr laden – auch wenn er sie gar nicht nutzt. Schaltest du das Paket wieder ein, lädt er wieder. Gebäude, die Buildings Plus aus dem Paket erzeugt hat, räumt RMM beim Speichern aus `media_soviet\workshop_wip` weg und hebt sie im Zustandsordner des Pakets auf. Sonst würde das Spiel sie weiter laden und beim Start abstürzen, wenn ihre Ressource fehlt. Nach dem Wiedereinschalten schreibt Buildings Plus sie beim nächsten Spielstart neu, unter derselben Nummer.

**Namen in deiner Sprache.** Ein Paket kann für seine Ressourcen Namen je Spielsprache mitbringen; RMM trägt sie mit ein. Im deutschen Spiel steht dann zum Beispiel „Kabel“ statt „Cable“. Anzeigen kann das Resources Plus ab Version 0.1.4; mit einer älteren Fassung oder dem Original Resources bleibt es beim englischen Namen.

**Kollisionen gewinnst du.** Bringt ein Paket eine Kennung mit, die es in deinem Spiel schon gibt — deine eigene Ressource, ein Eintrag eines anderen Pakets oder einer aus der ausgelieferten INI des Plugins —, dann wird das Paket **gar nicht** bereitgestellt: keine Einträge, keine Dateien im vfs. Auf der Paketseite steht eine rote Meldung mit der Kennung, die im Weg ist. Nimm sie erst weg — einen eigenen Eintrag im RMM, einen Originaleintrag direkt in `plugins\<plugin>.ini` — und schalte das Paket danach wieder ein. Dein Bestand wird nie stillschweigend ersetzt, und eine Datei, die vor dem Paket im vfs lag, wird ohnehin weder überschrieben noch später gelöscht.

Ausnahme: Gebäude, die zwei Pakete ausdrücklich teilen (etwa das Gaskraftwerk in Natural Gas und Base Resources Buildings). Dann liefert eines das Gebäude, und ist es aus, springt das andere ein – gleiche Nummer, deine Spielstände laden weiter. Die Paketseite sagt, wer es gerade liefert.

Ausschalten und Speichern nimmt Einträge und Dateien wieder heraus. Deine eigenen Einträge und deine Überschreibungen bleiben. Ändert sich das Paket im Workshop, erscheint in der Liste die gelbe Marke „Update“; einmal speichern übernimmt den neuen Stand.

Findet der Loader für eine Inhaltsart kein passendes Plugin, sagt die Paketseite das (zum Beispiel „Für Gebäude ist kein passendes Plugin eingerichtet“) und überspringt diesen Teil.

**Regelsätze für Spielgebäude.** Ein Paket kann auch Regelsätze für **Vanilla Buildings** mitbringen, zum Beispiel ein zusätzliches Lager für seine Ressource in einem Gebäude des Spiels. Sie kommen wie die Gebäude an: als Originale auf der Seite von Vanilla Buildings, wo du jeden einzeln ein- und ausschalten kannst.

**Fahrzeugbau.** Manche Pakete können ihre Ressource zu einem Material für den Fahrzeugbau machen. Dann hat die Paketseite eine Karte „Fahrzeugbau“ mit zwei Möglichkeiten:

- **EIN:** Jede Fahrzeugfabrik braucht zusätzlich diese Ressource, sonst baut sie nichts mehr (außer Kutschen und anderen Fahrzeugen mit Pferden). Dafür muss das Plugin **Vehicle Materials** installiert und eingeschaltet sein.
- **AUS:** Fahrzeuge werden gebaut wie bisher.

RMM entscheidet das nie für dich. Solange du weder EIN noch AUS gewählt und gespeichert hast, passiert nichts, und RMM fragt vor jedem Spielstart nach, damit du es nicht übersiehst. Dasselbe gilt, wenn EIN gewählt ist, aber Vehicle Materials fehlt oder ausgeschaltet ist: Dann steht auf der Karte und in „Vor dem Spielstart“, woran es liegt.

Mit EIN trägt RMM das Material mit den Werten des Pakets in die Liste auf der Seite von **Vehicle Materials** ein und schaltet das Plugin ein, falls es aus war. Dort ist es ein Eintrag wie jeder andere: Du kannst die Werte für Straße, Schiene, Schiff, Flugzeug und Raketenteile ändern, und deine Werte bleiben auch nach einem Update des Pakets. Bringt ein Update einen Wert für eine Klasse, die dein Eintrag noch nicht hat (etwa die Raketenteile), trägt RMM nur diesen nach. AUS nimmt den Eintrag wieder heraus, genau wie der Papierkorb auf der Seite von Vehicle Materials. Löschst du den Eintrag dort selbst, zeigt die Karte „Fahrzeugbau“ des Pakets danach AUS. Hattest du das Material schon selbst eingetragen, steht die Karte von Anfang an auf EIN, und deine Werte bleiben. Dein eigener Eintrag gehört dir: Weder AUS noch das Ausschalten des Pakets nimmt ihn heraus, die Karte sagt das in einem Hinweis. Schaltest du das Paket ganz aus, nimmt RMM nur den Eintrag mit heraus, den das Paket selbst angelegt hat.

**Das Lager in den Fahrzeugfabriken.** Eine Fahrzeugfabrik kann ein zusätzliches Material nur annehmen, wenn sie ein Lager dafür hat. Das gibt ihr **Vehicle Materials** selbst, auch Fabriken aus dem Workshop: Neue Fabriken haben es von Anfang an, schon gebaute bekommen es leer, wenn du den Spielstand lädst (dafür brauchst du die `tesmioloader.dll` aus dem RMM-Paket). Nimmst du ein Material wieder heraus, verschwindet sein Lager, sobald es leer ist.

---

## ✅ Vor dem Spielstart

Das Klemmbrett-Symbol in der Seitenleiste öffnet eine Seite, die alles zusammenfasst, was der nächste Spielstart tun wird:

- **Überblick:** wie viele Plugins geladen werden, wie viele ausgeschaltet sind, wie viele Pakete ein ungespeichertes Update haben, wann das Spiel zuletzt lief und wie viele Spielstände gefunden wurden.
- **Was im Weg steht:** fehlende oder ausgeschaltete Abhängigkeiten, abgewiesene Pakete, wartende Updates, ein Plugin und sein Ersatz, die beide eingeschaltet sind, DLLs, die es mehrfach gibt — und Abhängigkeiten, die **zu spät** geladen werden. Ein Klick auf eine Zeile schliesst das Fenster und zeigt den betroffenen Eintrag.
- **Gebäudezeilen ohne Ressource:** eine `$STORAGE`-, `$PRODUCTION`- oder `$CONSUMPTION`-Zeile in Vanilla Buildings oder Buildings Plus, die eine Ressource nennt, die kein Plugin liefert. Geprüft werden auch die Gebäude-Ordner in `media_soviet\workshop_wip`, weil das Spiel jeden davon lädt. Damit stürzt das Spiel beim Einlesen der Gebäude ab; RMM nennt Plugin, Abschnitt und Ressource hier und fragt bei „Speichern + Starten" noch einmal nach.
- **Regelsätze, die nicht überall greifen:** ein eingeschalteter Regelsatz von Vanilla Buildings, bei dem ein Befehl nicht in jede Zieldatei passt. Bei diesen Gebäuden ändert er nichts; das Spiel startet trotzdem. RMM nennt den Regelsatz, die Zahl der Gebäude und die erste Datei, bei der es nicht passt.
- **Ladereihenfolge:** die Liste in der Reihenfolge, in der der Loader arbeitet: nach dem Namen der DLL, egal ob sie in `plugins\` oder im Workshop-Ordner liegt. Bei jedem Eintrag steht, woher er kommt. Das ist wichtig, wenn ein Plugin einen Dienst eines anderen braucht: Den gibt es erst, wenn der Anbieter geladen ist.

---

## 💾 Spielstände

RMM liest die Datei `tesmioloader.save.ini`, die der Loader neben jeden Spielstand legt. Darin steht, welche Plugins geladen waren und welche Ressourcen und Vorkommen die Welt kennt. Geschrieben wird dort nie etwas.

Daraus werden zwei Dinge:

- Auf der Seite eines Plugins oder Inhaltspakets steht eine Zeile **„Wird von … Spielständen benutzt"** mit den Namen.
- Beim **Ausschalten** fragt RMM nach und nennt genau diese Spielstände. „Nein" lässt den Schalter, wo er war.

---

## 📜 Das Protokollfenster

Das Protokoll-Symbol in der Seitenleiste öffnet ein Fenster mit drei Arten von Quellen: dem Journal dieser RMM-Sitzung, `tesmioloader.log` aus dem Loader-Ordner und jedem Plugin-Protokoll `tesmioloader.<plugin>.log`, egal ob es im Unterordner `logs\` oder direkt im Loader-Ordner liegt. Die Dateien lassen sich auch lesen, während das Spiel läuft; „Aktualisieren“ liest neu, „Ordner öffnen“ zeigt den Loader-Ordner im Explorer.

„Suchen“ filtert nach Text, „Absender“ nach dem ersten Wort einer Zeile (`plugin`, `hook`, ein Plugin-Name), „Nur Probleme und Warnungen“ blendet alles Unauffällige aus. Fehlerzeilen stehen rot, Warnungen gelb. Die Fußzeile nennt Zeilen, Treffer und die letzte Abschlusszeile des Loaders.

---


## ⚙️ Das Einstellungsfenster

Das Schieberegler-Symbol in der Seitenleiste öffnet die Einstellungen von RMM selbst — nicht die des gewählten Plugins:

- **Fenster des TesmioLaunchers zeigen:** AUS startet das Spiel sofort, EIN zeigt erst den Launcher.
- **Nach dem Start zusehen:** wie viele Sekunden RMM prüft, ob das Spiel oben bleibt. 0 schliesst sofort.
- **Fenster offen lassen:** EIN lässt RMM nach dem Start stehen; du schliesst es selbst.
- **Sprache** der Oberfläche, dasselbe wie der Knopf `DE` daneben.
- **Vor unbekannter Spielversion warnen:** EIN meldet, wenn dein Spiel nicht die Version ist, für die die Plugins gebaut wurden.
- **Meldungen des Spiels:** ein Schalter mit vier Stellungen, wie viel das Spiel in `tesmioloader.log` schreiben darf. Von links nach rechts: aus, nur Fehler, Fehler und Warnungen, alles. Unter dem Schalter steht, was die gewählte Stellung bedeutet.
- **Wiederholungen zeigen:** wie oft dieselbe Meldung ausgeschrieben wird, bevor sie nur noch gezählt wird. Die Summe steht am Ende des Protokolls. 0 schreibt jede Wiederholung aus.
- **Angaben für einen Fehlerbericht:** legt Versionen, Ordner, gefundene Plugins und den Zustand von Steam in die Zwischenablage.

Die zwei Werte der Karte **Protokoll** gehören dem Loader, nicht RMM: sie landen in `[tesmioloader]` der `tesmioloader.ini` und gelten ab dem nächsten Spielstart. RMM ändert dort nur diese beiden Zeilen, Kommentare und alle anderen Schlüssel bleiben stehen. Die Karte erscheint nur, wenn die Datei da ist.

Die übrigen Werte gelten sofort und werden beim Schliessen gespeichert — und zwar dort, wo RMM sich alles merkt, nicht in der `rmm.ini`. Weicht dein Wert von dem ab, was in der `rmm.ini` steht, sagt dir eine kleine Zeile unter dem Feld, was dort vorgegeben ist.

**Neu anfangen.** Ganz unten im Einstellungsfenster stehen zwei Knöpfe, die sauber getrennt sind:

- **RMM-Daten löschen** (gelb) räumt nur weg, was RMM sich selbst merkt: Profile, Wiederherstellungspunkte und den gemerkten Fensterzustand. Deine Plugins, ihre Einstellungen und deine Spielstände bleiben unberührt.
- **Alles zurücknehmen** (rot) nimmt zusätzlich alles zurück, was RMM je in den Loader-Ordner geschrieben hat: deine persönlichen Werte, Dateien im `vfs` und die Schalter in `tesmioloader.ini`; die gesicherten Original-INIs schreibt RMM zurück.

Vor dem roten Knopf zeigt RMM erst, was verloren geht — **einschliesslich der Namen deiner Spielstände**, die auf die betroffenen Pakete bauen. Danach musst du das Wort `LÖSCHEN` eintippen, bevor der letzte Knopf überhaupt anklickbar wird. Der Fokus liegt überall auf Abbrechen, damit die Eingabetaste nichts kaputtmacht.

Vorher legt RMM eine Sicherung aller INI-Dateien unter `tesmioloader\rmm_reset_backup\<Zeitstempel>` an. Nicht angefasst werden der Spielordner mit deinen Spielständen, deine Abos im Workshop, der TesmioLoader selbst und alles, was RMM nie geschrieben hat. Willst du auch das Programm loswerden, nimm `Uninstall-RMM.bat` aus dem Workshop-Paket.

---
## 🗂️ Profile und Wiederherstellung

Das Archiv-Symbol in der Seitenleiste öffnet das Fenster „Profile und Wiederherstellung“ mit zwei Reitern.

**Profile** sichern den kompletten Einstellungsstand unter einem Namen: `tesmioloader.ini`, alle INIs in `plugins\` und `user_config\` und die gesicherten Originale. DLLs gehören nie dazu. „Aktuellen Stand sichern…“ fragt Name und Notiz ab. Rechts siehst du je Datei, ob sie zum gespeicherten Stand passt. „Profil anwenden“ schreibt alles zurück; INIs von Plugins, die das Profil nicht kennt, bleiben stehen. Profile liegen unter `user_config\.autoload\profiles\`.

**Wiederherstellungspunkte** entstehen bei jedem Speichern: der Stand aller berührten Dateien von vor dem Speichern, je Plugin genau einer. „Auf diesen Punkt zurücksetzen“ schreibt diese Dateien zurück; der jetzige Stand wird dabei zum neuen Punkt, du kannst also wieder zurück. Punkte, deren Dateien fehlen, werden nur angezeigt.

Beide Aktionen brauchen ein geschlossenes Spiel und einen geschlossenen TesmioLauncher.

---

## 📝 Listen-Editoren

Einige Plugins verwalten Listen statt einzelner Werte. RMM zeigt sie als Liste links und Felder rechts. Das gilt für Resources, Needs und Deposits von MaxLegend, für ihre Ersatz-Plugins, für Buildings Plus und für Pakete, die so eine Seite mitbringen (etwa Research Expansion, Technical Service Storage, Vanilla Buildings, UI Layout Fixes).

**Gemeinsam für alle:**

- Einträge aus dem Plugin oder Paket tragen ein Schloss. Du kannst sie ändern, aber nicht löschen; über den roten Knopf lassen sie sich ausblenden und unter „Ausgeblendete Originaleinträge“ wieder anzeigen.
- Eigene Einträge legst du mit „+“ an. Der Dialog fragt nur das Nötige ab und schlägt vor, was er kann; alles Weitere stellst du danach rechts ein. Eigene Einträge lassen sich löschen.
- Kennungen, die etwas im Spiel benennen müssen (eine Ressource, eine Forschung, eine Gebäudedatei, eine Text-ID), prüft RMM vor dem Speichern. Stimmt eine nicht, wird die Fußzeile rot und nennt Eintrag und Feld.
- Eine gelbe Warnung erinnert dich, wenn eine Änderung den Spielstand betrifft, etwa neue Ressourcen oder Vorkommen, die ein laufendes Spiel dann braucht.

**Resources:** Liste aller Ressourcen des Plugins. **Resources Plus** hat dieselbe Seite für `plugins\resources_plus.ini`, die Datei des fehlerbereinigten Forks. Die beiden sind ein Ersatz-Paar: Es läuft immer nur eines, beide Einträge bleiben in der Liste. Vorbildressource, Transportklasse und Materialfamilie sind Auswahllisten mit den Namen aus dem Spiel. In der INI gibt es keinen Aus-Schalter; aus schaltest du das Plugin mit „Plugin aktiv“. Bei Resources Plus stehen ganz unten in den Eigenschaften einer Ressource die Felder „Namen je Spielsprache“: Was du dort für Deutsch einträgst, zeigt das deutsche Spiel; ein leeres Feld lässt den Anzeigenamen gelten. Das geht auch bei Originalen und bei Ressourcen aus Inhaltspaketen. Eine Original-Ressource, die du gar nicht haben willst, blendest du mit dem roten Papierkorb neben ihrem Namen aus: Sie fehlt dann in der Liste, die das Spiel liest, bleibt aber in der Originaldatei stehen und lässt sich unter der Liste bei „Ausgeblendete Originaleinträge“ wieder anzeigen. Achtung: Ein Spielstand merkt sich jede Ressource beim Namen. Ein Spielstand, der mit dieser Ressource gespeichert wurde, lässt sich danach nicht mehr laden – das Spiel stürzt beim Laden ab, auch wenn der Spielstand die Ressource gar nicht nutzt. Zeigst du sie wieder an, lädt er wieder. Dasselbe gilt, wenn du eine eigene Ressource entfernst oder ein Inhaltspaket mit Ressourcen ausschaltest. Einstellungen anderer Plugins, die diese Ressource nennen, bereinigt RMM beim selben Speichern und sagt dir vorher, welche. Nennt ein Gebäude die Ressource noch, lässt RMM das Ausblenden oder Entfernen nicht zu und zeigt dir, welches: einen Abschnitt von Buildings Plus oder Vanilla Buildings oder einen Ordner in `media_soviet\workshop_wip`. Ohne die Ressource würde das Spiel beim Start abstürzen, sobald es dieses Gebäude lädt. Nimm zuerst das Gebäude heraus, dann die Ressource. **Needs Plus** ist der gleiche Fork für needs: dieselbe Seite wie Needs, für `plugins\needs_plus.ini`.

**Needs:** Liste der Bedürfnisse mit Spender, Faktor, Ladenkategorie, Wahrscheinlichkeit und Unzufriedenheit. Höchstens acht Einträge, bei Needs Plus 16. Der Reiter „Allgemein“ trägt die Schalter des Plugins.

**Deposits:** Reiter „Allgemein“ mit den Plugin-Schaltern und Reiter „Vorkommen“ mit einem Eintrag je Vorkommen: Kennung, Typnummer, Karte, Platz auf der Karte, Symbol und mehr. Die Typnummer schlägt der Dialog als nächste freie vor; eine doppelte wird abgewiesen. **Deposits Plus** übernimmt einmalig die Vorkommen, die du der `deposits.ini` des Originals selbst hinzugefügt hast: Sie stehen dann als deine eigenen Vorkommen in der Liste, mit derselben Typnummer und demselben Token, und ein Hinweis nennt sie. Ist eine Nummer oder ein Token schon vergeben, bleibt das Vorkommen draußen und wird genannt.

**Einträge, die nicht von RMM stammen:** Bei Plugins, deren Original die INI aus dem Paket ist (Deposits Plus, Research Expansion, Technical Service Storage, UI Layout Fixes, Vanilla Buildings), schreibt RMM die Datei im Loader-Ordner bei jedem Speichern neu. Steht dort ein Eintrag, den weder das Paket noch RMM kennt, etwa von Hand ergänzt oder vom Plugin selbst angelegt, übernimmt RMM ihn als deinen eigenen Eintrag, statt ihn zu überschreiben. Das gilt für ganze Einträge; einen von Hand geänderten Wert eines Originaleintrags übernimmt RMM nicht.

**Buildings Plus:** Reiter „Allgemein“ mit den Plugin-Schaltern, Reiter „Gebäude“ mit deinen eigenen Erklärungen — und Reiter „Erzeugte Gebäude“, siehe unten.

Der Reiter „Gebäude“ ist eine Liste mit einer Zeile je Gebäude: Schalter, Name, Nummer, Objektordner und Spender, rechts der Zustand und die Knöpfe **Ändern…**, **Öffnen** und der Papierkorb. Der Zustand sagt, was beim nächsten Spielstart passiert — ob das Gebäude im Spiel ist, erst noch geschrieben wird, neu gebaut wird, oder ob etwas im Weg steht: ein Spender, den es auf deinem Rechner nicht gibt, oder eine Nummer, die schon einem fremden Ordner gehört. Beides bricht sonst erst beim Spielstart ab und steht nur im Protokoll.

Hinter „Ändern…“ liegen die Angaben (Nummer, Spender, Objektname, Name im Spiel, Beschreibung, Lebensdauer) und ein zweiter Reiter für die Zeilen. Dort steht links die `building.ini` des Spenders und rechts die Liste deiner **Befehle**. Ein Befehl fasst genau eine Stelle an: Du klickst links eine Zeile an, schreibst unten die neue Zeile und wählst **Anhängen** (ans Ende der Datei), **Ersetzen**, **Davor** oder **Danach**; **Zeile entfernen** neben der ausgewählten Zeile nimmt sie heraus. In der Liste steht der Befehl dann zum Beispiel als `Einfügen 1 | $STORAGE_FUEL … | $STORAGE_IMPORT_SPECIAL …`. Alles, was du nicht anfasst, bleibt wie beim Spender – ein neues Lager wirft die anderen nicht weg. Zeilen, die zusammengehören (ein Anschluss mit seinen Punkten, eine Warenanzeige mit ihrer Lage), wählst und schreibst du als Block. Die Ansicht lässt sich auf **Ergebnis** umschalten — dann siehst du die Datei, die beim nächsten Start wirklich entsteht, mit der Nummer jedes Lagers für `$RESOURCE_VISUALIZATION` und einer Warnung, wenn Wasser oder Abwasser vor einem angezeigten Lager steht. Solange ein Befehl rot ist, schreibt das Plugin das Gebäude nicht. Ein Gebäude, das noch in der älteren Schreibweise mit `line`-Zeilen vorliegt, öffnet das frühere Fenster: Dort stehen deine Zeilen neben denen des Spenders, und durchgestrichen ist, was sie beim Spender wegwerfen.

Unter den beiden Listen steht, woraus ein Befehl entsteht. **Ausgewählte Zeile** zeigt die Zeile, die du oben angeklickt hast – nur zum Lesen; sie ist der Anker. In **Neue Zeile / Zeilen** schreibst du, was dort stehen soll. Du kannst mehrere Zeilen untereinander schreiben (mit Enter trennen): Jede wird ein eigener Befehl. **Anhängen** hängt sie der Reihe nach ans Ende; **Davor**, **Danach** und **Ersetzen** setzen die erste an die ausgewählte Stelle und die übrigen direkt darunter. Bei Buildings Plus gehören Zeilen ohne `$` am Anfang (Punkte, Position) zur Zeile darüber. Ein Doppelklick kopiert die angeklickte Zeile nach unten, wenn du nur einen Wert ändern willst. Einen Befehl, den es schon gibt, änderst du so: Klick ihn rechts in der Liste **Befehle** an – seine Zeile steht dann unten –, ändere sie und klick auf **Ändern**. Der Befehl bleibt, wo er ist, und ein Befehl, der an seiner Zeile hängt, zieht mit. Jede Ressource gehört zu einer Lagerklasse (Schüttgut, Trockengut, offen, gedeckt …). Schreibst du eine Lagerzeile mit einer Klasse, die die Ressource nicht hat – zum Beispiel Industriesalz in `RESOURCE_TRANSPORT_GRAVEL` –, warnt dich das Fenster in Orange und nennt die passende Klasse: So ein Lager würde im Spiel 0 Tonnen fassen. Darunter liegen die Knöpfe. Ein Knopf ist blass, solange ihm etwas fehlt, und die Zeile darunter sagt dir, was das ist. Das Zeilen-Fenster eines Regelsatzes von Vanilla Buildings ist genauso gebaut und hat dieselben Knöpfe; dort gibt es zusätzlich mehrere Zieldateien, und Anschlüsse haben eigene Befehle. Passt ein Befehl nicht in jede Zieldatei, steht das rot über den Listen, und die betroffenen Dateien sind in der Auswahl oben rot mit einem `x` markiert – bei ihnen ändert der Regelsatz im Spiel nichts. In der Liste der Regelsätze steht dann neben dem Namen zum Beispiel „Greift nur bei 1 von 20 Gebäuden“.

**Paket-Editoren** bringen ihre eigenen Reiter, Listen und Hilfen mit. Manche haben Knöpfe, die Anleitungen des Pakets öffnen, Auswahlfenster für Gebäude, Forschungen oder Spieltexte, Bildvorschauen oder einen Reiter für Übersetzungen.

---

## 🏗️ Erzeugte Gebäude

Ein Gebäude aus einem Inhaltspaket wird beim Spielstart als vollständiges Workshop-Objekt nach `media_soviet\workshop_wip\<Nummer>\` geschrieben — von Buildings Plus. Das sieht aus wie ein Abo, ist aber deine eigene Datei auf deiner Platte. Entfernst du ein Gebäude auf der Seite von Buildings Plus oder blendest es aus, räumt RMM seinen erzeugten Ordner beim Speichern mit weg (nach `tesmioloader\user_config\.autoload\parked_buildings`), damit das Spiel es nicht weiter lädt. Kommt der Abschnitt zurück, schreibt Buildings Plus den Ordner beim nächsten Spielstart neu, unter derselben Nummer.

Der Reiter „Erzeugte Gebäude“ von Buildings Plus zeigt je Ordner Name, Nummer, Objektordner und Herkunft. Zwei Dinge meldet er von sich aus:

- **Besitzer fehlt.** Steht in der `workshopconfig.ini` eine `$OWNER_ID 0`, meldet das Spiel bei **jedem** Laden eines Spielstands, dass Workshop-Objekte fehlen. Der Knopf „Besitzer eintragen“ setzt deine Steam-ID ein — geändert wird genau diese eine Zahl, jedes andere Byte der Datei bleibt, wie es war.
- **Verwaist.** Gibt es zu einem Ordner kein Paket mehr, das ihn erklärt, nennt RMM die Ordner. Lösche sie, sobald kein Spielstand die Gebäude mehr nutzt.

Ist Buildings Plus eingeschaltet, trägt es die fehlende Nummer beim Spielstart ohnehin selbst nach — der Knopf ist für den Fall, dass du es aus hast. Verändert wird sonst nichts, auch nicht der Stempel `tesmioloader.stamp` im Ordner.

### Ein erzeugtes Gebäude ändern

„Ändern…" öffnet die `building.ini` des Gebäudes. Links stehen die Zeilen, die der Generator schreibt, rechts **deine Änderungen**: eine Zeile ersetzen, eine Zeile entfernen, eine Zeile hinzufügen. Ein `*` markiert links jede Zeile, an der schon etwas von dir hängt.

Klickst du eine Zeile an, kommt der **ganze Block** ins Textfeld — also ein Token mit den Zeilen darunter, die ihm gehören (`$RESOURCE_VISUALIZATION` samt `position`, `rotation`, `scale`). Das ist nicht nur bequem: eine Zeile wie `rotation 0.0` steht in so einer Datei oft dreimal, und RMM ändert nie auf gut Glück die falsche. Als ganzer Block ist sie eindeutig, und damit lässt sie sich überhaupt erst ändern.

Wichtig ist, **was** gespeichert wird: nicht die geänderte Datei, sondern deine Änderungen. Das klingt nach Haarspalterei, entscheidet aber den Ernstfall — wenn das Paket ein Update bekommt, schreibt der Generator die Datei neu, und RMM spielt deine Änderungen in die **neue** Fassung ein. Was der Autor in der Zwischenzeit verbessert hat, bleibt also erhalten; eine gespeicherte Kopie hätte es weggeworfen. Passt eine Zeile nach dem Update nicht mehr (sie ist weg oder steht jetzt mehrfach da), sagt RMM das und lässt sie aus, statt still nichts zu tun.

Das Nachziehen passiert, sobald du den Reiter öffnest — also bevor du das Spiel startest. Eine blaue Zeile sagt dir, dass es passiert ist.

Hast du die Datei **von Hand** geändert, außerhalb von RMM, merkt RMM das (die Datei ist nicht mehr die, die es geschrieben hat) und fasst sie nicht an. Deine gespeicherten Änderungen liegen dann brach, bis du im Fenster auf „Übernehmen" gehst.

„Alles zurücksetzen" im Fenster wirft deine Änderungen weg und stellt die Fassung des Generators wieder her. Dasselbe macht „Alles zurücknehmen" im Einstellungsfenster für alle Gebäude auf einmal.

---

## 🎮 Spielversion und rmm.ini

Beim Start liest RMM die Kennung von `SOVIET64.exe`. Gehört sie zu keiner Spielversion, für die die Plugins gemacht sind, erscheint eine Warnung, und „Speichern + Starten“ fragt vor dem Start nach. Bekannt ist 1.2.0.9. Eine neue Kennung trägst du in `settings_schemas\game_versions.ini` unter `[supported]` ein.

`rmm.ini` neben `rmm.exe` hat wenige Schalter, alle mit Erklärung in der Datei:

| Schlüssel | Bedeutung |
|---|---|
| `[paths] workshop_root` | Ordner mit den abonnierten Paketen. Fehlt die Zeile (oder steht ein `;` davor), nimmt RMM den Steam-Workshop-Ordner deines Spiels. Ein leerer Wert ist nicht erlaubt. Der Loader muss im selben Ordner suchen. |
| `[settings] language` | `auto` (Deutsch bei deutschem Windows, sonst Englisch), `de`, `en` oder der Code einer Sprache der Einstellungsseiten (etwa `fr`, `zh-CN`; das Fenster ist dann englisch). |
| `[settings] version_check` | 0 schaltet die Warnung zur Spielversion ab. |
| `[settings] tesmiolauncher_window` | 1 zeigt das Fenster des TesmioLaunchers bei „Speichern + Starten“. |
| `[settings] launch_watch_seconds` | Wie lange RMM nach „Speichern + Starten“ zusieht, ob das Spiel oben bleibt. Standard 15, 0 schaltet es ab. |
| `[settings] keep_open` | 1 lässt das RMM-Fenster nach „Speichern + Starten“ offen stehen; du schliesst es selbst. Standard 0. |

Was du im Fenster einstellst, hat Vorrang vor der Datei.
**Ohne Fenster speichern.** Für Wartung von der Kommandozeile aus:

```bash
rmm.exe --save --build "<Spiel>\tesmioloader" --package "<Ordner des Plugins oder Pakets>"
```

Das ist derselbe Weg wie der Knopf Speichern, mit denselben Prüfungen: Spiel und TesmioLauncher müssen zu sein, alle Werte gültig. Jede Rückfrage, die das Fenster stellen würde, wird abgelehnt statt beantwortet. Die Antwort ist eine Zeile, die mit PASS oder FAIL beginnt.

Den Kopfschalter legt `--activate` um, bevor gespeichert wird:

```bash
rmm.exe --activate on --save --build "<Spiel>\tesmioloader" --package "<Ordner>"
```

`on` schaltet ein, `off` aus. Das ist derselbe Klick wie im Fenster, nur ohne Maus: bei einem Inhaltspaket heißt das "Im Spiel bereitstellen", bei einem Plugin "Plugin aktiv". Geschrieben wird erst durch `--save`; ohne `--save` weist RMM die Option ab.

Die Wahl auf der Karte „Fahrzeugbau“ eines Inhaltspakets trifft `--vehicles`, ebenfalls nur zusammen mit `--save`:

```bash
rmm.exe --vehicles on --save --build "<Spiel>\tesmioloader" --package "<Ordner>"
```

**Steam-Anmeldung prüfen.** Weil das Spiel hier direkt und nicht über Steam startet, braucht es einen angemeldeten Steam-Client. Ob einer da ist, sagt dir:

```bash
rmm.exe --steam-check
```

Die Antwort ist eine Zeile mit PASS oder FAIL und dazu das, was RMM in der Steam-Registrierung gefunden hat. Dieselbe Prüfung läuft vor „Speichern + Starten“; warnen tut sie nur, wenn Steam keinen angemeldeten Spieler einträgt oder gar kein Client läuft. Kann RMM nicht nachsehen, sagt es nichts und startet.

---

## 📦 Wo deine Dateien liegen

```
SovietRepublic\tesmioloader\
├── tesmioloader.dll, tesmiolauncher.exe   der Loader und sein Starter
├── tesmioloader.ini                    Schalter der Plugins ([plugins])
├── rmm.exe, rmm.ini                    RMM und seine Grundeinstellungen
├── settings_schemas\                   Einstellungsseiten für Plugins ohne eigene Seite
├── plugins\                            lokale Plugin-DLLs, ihre INIs und die Listen der Listen-Editoren
├── vfs\                                Dateien aus Inhaltspaketen
├── user_config\
│   ├── <name>.ini                      deine persönlichen Werte je Plugin
│   └── .autoload\
│       ├── <name>.upstream.ini         gesicherte Originale
│       ├── backups\<paket>\previous\   Wiederherstellungspunkte
│       └── profiles\                   deine Profile
└── logs\                               Protokolle der Plugins

steamapps\workshop\content\784150\      deine abonnierten Pakete; hier schreibt RMM nie
```

Fenstergröße, Sprache und Auswahl merkt sich RMM unter `%LOCALAPPDATA%\RepublicModManager`.

---

## 🛠️ Wenn etwas nicht klappt

| Was du siehst | Was dahintersteckt | Was hilft |
|---|---|---|
| Rotes Feld „Fehlt: …“ | Der Loader findet das Plugin nicht, das dieses hier braucht | Das fehlende Paket abonnieren. Vor dem Start bietet RMM an, das betroffene Plugin auszuschalten |
| Gelbes Feld „Diese DLL gibt es mehrfach“ | Dieselbe DLL liegt an mehreren Orten, der Loader lädt nur eine | Die Kopie entfernen, die du nicht willst, meist die in `plugins\` |
| Rotes Feld zum Workshop-Ordner | RMM und der Loader suchen in verschiedenen Ordnern | In `rmm.ini` die Zeile `workshop_root` entfernen oder denselben Ordner eintragen wie beim Loader |
| Oranger Punkt „Wird ersetzt“ | Ein Plugin und sein Ersatz sind beide eingeschaltet | Eines der beiden ausschalten; sonst schaltet RMM beim Start das Original aus |
| „Beim letzten Spielstart nicht geladen“ | Das Plugin war beim letzten Start aus, oder das Spiel lief seit dem Einschalten nicht | Speichern und das Spiel einmal starten |
| „Speichern + Starten“ startet das Spiel nicht, der TesmioLauncher meldet eine falsche Spielversion | Dein TesmioLauncher kennt die Spielversion noch nicht (bei 1.2.0.9 zum Beispiel ein Launcher älter als 0.4.6) | Den neuen Launcher aus dem Abo von MaxLegends TesmioLoader nehmen: `tesmiolauncher.exe` aus dessen Ordner `tesmioloader` in den Ordner `tesmioloader` im Spielordner kopieren. Bis es einen passenden Launcher gibt, hilft `version_check = 0` unter `[tesmioloader]` in `tesmioloader\tesmioloader.ini` |
| Speichern geht nicht | Spiel oder TesmioLauncher läuft, oder ein Wert liegt außerhalb des Bereichs | Programme beenden, roten Hinweis in der Fußzeile lesen |
| Ein Wert steht nach dem Speichern wieder anders | Das Plugin las eine andere INI als erwartet | Statuszeile prüfen: Wo liegen die Dateien? Protokollfenster öffnen |
| Ein Wert eines Workshop-Plugins wirkt im Spiel nicht | Die `tesmioloader.dll` ist nicht die aus dem RMM-Paket, oder das Plugin liest seine INI selbst und sieht deine persönlichen Werte nicht | Die `tesmioloader.dll` aus dem RMM-Paket nehmen. Hilft das nicht: DLL und INI des Plugins nach `tesmioloader\plugins` kopieren, dann schreibt RMM die fertige INI neben die DLL (Updates von Steam kommen bei dieser Kopie nicht mehr von selbst an) |

Für einen Fehlerbericht: Rechtsklick auf die Meldung, „Text kopieren“, dazu das Protokollfenster mit „Nur Probleme und Warnungen“.

---

## 🧩 Für Plugin-Autoren

Wie ein Workshop-Paket aufgebaut sein muss, damit RMM es zeigt, und wie eine Einstellungsseite mit Reitern, Karten, Feldern, Listen und Übersetzungen beschrieben wird, steht in `SCHEMA_DE.md` (englisch: `SCHEMA_EN.md`).

---

**Genosse, Achtung:** Dieses Programm wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

**GNU GPL v3.** Quelltext: https://github.com/Kespri/WRSR-MODS/tree/main/tools/republic_mod_manager
