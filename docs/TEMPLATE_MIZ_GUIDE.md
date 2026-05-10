# Template MIZ Guide

Diese Datei beschreibt, worauf beim Erstellen einer Template-Mission fuer den
DCS Persistent War Launcher geachtet werden muss.

## Grundregeln

- Player-Slots bleiben im Template und werden vom Launcher nicht automatisch entfernt.
- Player-Gruppen muessen `_CLIENT_` im Gruppennamen enthalten.
- Campaign-Anker werden als Trigger Zones im Mission Editor angelegt.
- Launcher-Anker muessen mit `WL_` beginnen.
- Namen sollten nur Grossbuchstaben, Zahlen und `_` verwenden.
- Eine Template-MIZ sollte in `src/DcsWarLauncher/Data/Templates` liegen.
- Der Launcher verwendet aktuell die neueste `.miz` aus diesem Ordner.

## Player Slots

Client-Gruppen sollten nach Coalition, Rolle/Flugzeug, Base und Nummer benannt werden.

Beispiele:

```text
BLUE_CLIENT_F16_KUTAISI_01
BLUE_CLIENT_FA18_KUTAISI_01
BLUE_CLIENT_AH64_GUDAUTA_01
RED_CLIENT_SU27_KRASNODAR_01
RED_CLIENT_SU25T_KRASNODAR_01
```

Wichtig:

- Alle spielbaren Units in diesen Gruppen muessen Skill `Client` haben.
- Der Launcher zaehlt diese Slots und warnt, wenn eine `_CLIENT_` Gruppe AI-Units enthaelt.
- Player-Slots sind heilig: Sie sollen als statische Template-Basis erhalten bleiben.

## Objective-Anker

Objective-Anker markieren Angriffs-/Operationspunkte fuer Campaign-Ziele.
Pro Objective werden ein Blue- und ein Red-Anker empfohlen.

Schema:

```text
WL_OBJ_<OBJECTIVE>_BLUE
WL_OBJ_<OBJECTIVE>_RED
```

Aktuelle Beispiele:

```text
WL_OBJ_KUTAISI_BLUE
WL_OBJ_KUTAISI_RED
WL_OBJ_SENAKI_BLUE
WL_OBJ_SENAKI_RED
WL_OBJ_SUKHUMI_BLUE
WL_OBJ_SUKHUMI_RED
WL_OBJ_KRASNODAR_BLUE
WL_OBJ_KRASNODAR_RED
```

Hinweise:

- Blue-Anker leicht auf die blaue Seite des Objectives setzen.
- Red-Anker leicht auf die rote Seite des Objectives setzen.
- Nicht direkt auf Runways setzen.
- Fuer `Krasnodar Center` akzeptiert der Launcher auch `WL_OBJ_KRASNODAR_BLUE/RED`.

## Front-Anker

Front-Anker beschreiben die grobe Frontlinie. Sie werden als nummerierte Trigger Zones
entlang der Linie gesetzt.

Schema:

```text
WL_FRONT_01
WL_FRONT_02
WL_FRONT_03
WL_FRONT_04
```

Hinweise:

- Nummerierung von Sueden/Westen nach Norden/Osten konsistent halten.
- Der Launcher sortiert nach der Nummer.
- Fuer breite Frontabschnitte koennen groessere oder polygonale Trigger Zones genutzt werden.
- Aktuell liest der Launcher Name, X/Y und Radius.

## Airbase-Anker

Airbase-Anker sind fuer spaetere Startlogik, Ownership, Fallbacks und AI-Spawns vorgesehen.
Sie sind noch nicht zwingend fuer Phase 4, aber sollten bei neuen Templates mitgedacht werden.

Schema:

```text
WL_AIRBASE_<AIRBASE>
```

Beispiele:

```text
WL_AIRBASE_KUTAISI
WL_AIRBASE_SENAKI
WL_AIRBASE_SUKHUMI
WL_AIRBASE_GUDAUTA
WL_AIRBASE_KRASNODAR
WL_AIRBASE_KRYMSK
WL_AIRBASE_MAYKOP
```

Hinweise:

- Nicht direkt auf die Runway setzen.
- Am besten mittig im sicheren Operationsbereich der Base.
- Fuer `Krasnodar Center` akzeptiert der Launcher auch `WL_AIRBASE_KRASNODAR`.

## Heli- und FARP-Anker

Diese Anker sind fuer spaetere Heli-Startlogik und Forward Operations vorgesehen.

Schema:

```text
WL_HELI_BASE_<AIRBASE>_BLUE
WL_HELI_BASE_<AIRBASE>_RED
WL_FARP_<LOCATION>_BLUE
WL_FARP_<LOCATION>_RED
```

Beispiele:

```text
WL_HELI_BASE_GUDAUTA_BLUE
WL_HELI_BASE_GUDAUTA_RED
WL_HELI_BASE_KUTAISI_BLUE
WL_FARP_GUDAUTA_BLUE
WL_FARP_SENAKI_BLUE
WL_FARP_SUKHUMI_RED
```

Geplante Logik:

- Wenn eine Forward Base gehalten wird, koennen Helis dort starten.
- Wenn sie verloren oder contested ist, fallen Helis auf eine sichere Base zurueck.
- Fallback-Regeln werden spaeter in der Campaign-Logik umgesetzt.

## Supply-, Depot- und Spawn-Anker

Diese Anker sind fuer spaetere AI- und Logistik-Generierung vorgesehen.

Empfohlene Namen:

```text
WL_DEPOT_<LOCATION>_01
WL_SPAWN_<COALITION>_<LOCATION>_01
WL_RESERVE_<COALITION>_<LOCATION>_01
```

Beispiele:

```text
WL_DEPOT_KUTAISI_01
WL_DEPOT_KRASNODAR_01
WL_SPAWN_BLUE_KUTAISI_01
WL_SPAWN_RED_KRASNODAR_01
WL_RESERVE_BLUE_SENAKI_01
WL_RESERVE_RED_KRYMSK_01
```

## Warehouse/Fuel/Ammo

Der Launcher schreibt in vorbereitete Turn-MIZ-Dateien aktuell einen sicheren
`warLauncher`-Block in die `warehouses` Datei. Dieser Block spiegelt Fuel, Ammo
und Aircraft-Verfuegbarkeit aus dem Campaign-State, ohne unbekannte DCS-Warehouse-
IDs aggressiv zu ueberschreiben.

Aktueller Modus:

```text
campaign-supply-shadow + bekannte Caucasus-Airport-Warehouse-IDs
```

Wichtig:

- Die Template-MIZ sollte eine normale `warehouses` Datei enthalten.
- Wenn die Datei fehlt, legt der Launcher sie in der generierten Turn-MIZ an.
- Fuer bekannte Caucasus-Airbases patcht der Launcher echte `warehouses["airports"][id]` Eintraege.
- Aktuell werden `unlimitedFuel`, `OperatingLevel_Fuel`, `OperatingLevel_Eqp`, `OperatingLevel_Air` und Fuel-Init-Werte gesetzt.
- Munitions- und Aircraft-Bestaende bleiben noch vorsichtig, bis echte DCS-Weapon-/Aircraft-Inventare sauber gemappt sind.
- Der Shadow-Block bleibt als lesbare Diagnose in der MIZ.

Bekannte Caucasus-IDs:

```text
13 Krasnodar Center
20 Sukhumi
21 Gudauta
23 Senaki
25 Kutaisi
```

## Air Defense und Sensoren

SAM/EWR/Radar-Anker sind fuer spaetere Luftverteidigung und Detection-Logik vorgesehen.

Empfohlene Namen:

```text
WL_SAM_<LOCATION>_01
WL_EWR_<LOCATION>_01
WL_RADAR_<LOCATION>_01
```

Beispiele:

```text
WL_SAM_KUTAISI_01
WL_SAM_KRASNODAR_01
WL_EWR_SUKHUMI_01
WL_RADAR_SENAKI_01
```

## Aktueller Testumfang

Der Launcher erkennt aktuell:

- `_CLIENT_` Gruppen
- `WL_` Trigger Zones
- Objective-Anker `WL_OBJ_...`
- Front-Anker `WL_FRONT_...`
- vorbereitete Airbase-/Heli-/FARP-Anker im MissionPlan
- Warehouse-Shadow-Patches in generierten Turn-MIZ-Dateien

Der Launcher veraendert aktuell nicht:

- Player-Slots
- Units
- Routes
- Trigger-Logik
- Airbase-Koalitionen in der MIZ

## Checkliste vor dem Speichern

- Template-MIZ laedt in DCS ohne Fehler.
- Coalition-/Slot-Auswahl funktioniert.
- Alle Player-Gruppen enthalten `_CLIENT_`.
- Alle spielbaren Slots sind Skill `Client`.
- Wichtige Campaign-Anker beginnen mit `WL_`.
- Objective-Anker haben Blue- und Red-Seite.
- Front-Anker sind sauber nummeriert.
- Template im richtigen Ordner gespeichert.
- Im Launcher `Mission Template -> Pruefen` ausfuehren.
- Warnungen im Template Inspector pruefen.
- `Mission Plan Vorschau` pruefen.
