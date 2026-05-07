# PersistentWar Launcher - Agent Rules

## Projekt
Webbasierter PersistentWar Launcher für DCS.

## Architektur
- Backend: ASP.NET Core
- Frontend: Web UI
- Ziel: dynamische persistente Kampagne

## Branch Regeln
- Niemals direkt auf main arbeiten
- Features nur auf feature/* branches
- dev ist der Hauptentwicklungsbranch

## Arbeitsweise
- Kleine testbare Schritte
- Keine grossen Refactors ohne Auftrag
- Bestehende Architektur respektieren
- Nach Änderungen erklären:
  - welche Dateien geändert wurden
  - warum
  - was getestet werden soll

## Niemals committen
- .env Dateien
- API Keys
- lokale DCS Pfade
- lokale Serverdaten
- Passwörter

## v1 Ziele
- Dynamische Kampagne
- automatischer Restart
- Missionsstatus speichern
- neue Mission generieren
- Webinterface
- DCS Server starten/stoppen

## Wichtig
Vor jeder grösseren Änderung zuerst Projekt analysieren und Plan erstellen.