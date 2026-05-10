# Mission Result Guide

Diese Datei beschreibt das Ergebnisformat, das der Launcher aus `Data/Results`
einliest, um daraus automatisch einen `BattleReport` fuer den naechsten Turn zu
erzeugen.

## Ablage

- Ergebnisdateien liegen in `src/DcsWarLauncher/Data/Results`.
- Der Launcher liest aktuell die neueste `.json` oder `.log` Datei aus diesem Ordner.
- Lokale Ergebnisdateien werden nicht nach Git committed.

## Direktes BattleReport-Format

Das einfachste Testformat ist ein kompletter BattleReport:

```json
{
  "battleReport": {
    "blueMissionSuccess": 12,
    "redMissionSuccess": 4,
    "blueLosses": 2,
    "redLosses": 7,
    "airSuperiority": 6
  }
}
```

## Eventlisten

Alternativ kann die Datei Events enthalten. Das ist das Ziel fuer spaetere
DCS-Lua-Exporte:

```json
{
  "events": [
    { "type": "objective-captured", "coalition": "blue", "value": 10 },
    { "type": "kill", "targetCoalition": "red" },
    { "type": "air-superiority", "coalition": "blue", "value": 5 }
  ]
}
```

Ein reines JSON-Array ist ebenfalls gueltig:

```json
[
  { "type": "mission-success", "coalition": "blue", "value": 6 },
  { "type": "loss", "targetCoalition": "blue" }
]
```

## JSONL / Log-Format

Fuer einfache DCS-Log-Ausgaben darf jede Event-Zeile ein JSON-Objekt sein.
Andere Zeilen werden ignoriert:

```text
WL_EVENT_EXPORT_BEGIN
{ "type": "mission-success", "coalition": "blue", "value": 6 }
{ "type": "loss", "targetCoalition": "blue" }
{ "type": "objective-captured", "coalition": "red", "value": 8 }
WL_EVENT_EXPORT_END
```

## Unterstuetzte Eventfelder

- `type`, `event`, `eventType` oder `name`
- `coalition`, `initiatorCoalition`, `side` oder `winner`
- `targetCoalition`, `victimCoalition` oder `lostCoalition`
- `value`, `score` oder `points`

Coalitions koennen als `blue`, `red`, `allies`, `axis`, `2` oder `1`
geschrieben werden.

## Aktuelle Eventwertung

- `kill`, `dead`, `loss`, `crash`, `eject`: Verlust fuer `targetCoalition`.
- `package-success`, `mission-success`, `task-complete`: Erfolg fuer `coalition`.
- `objective`, `capture`, `destroyed-target`: Objective-Erfolg fuer `coalition`.
- `air-superiority`, `air-superiority-shift`, `airpower`: Luftueberlegenheit.

Alle Werte werden auf die bestehenden Turn-Grenzen geklemmt:

- Erfolg: `-25` bis `25`
- Verluste: `0` bis `50`
- Luftueberlegenheit: `-25` bis `25`
