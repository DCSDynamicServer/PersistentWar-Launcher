# Roadmap

## Phase 1: Solides Fundament

- Backend sauber strukturieren.
- Remote Start/Stop absichern.
- 6h-Turns persistieren.
- Frontlinien und AI-Orders sichtbar machen.
- 24/7 Scheduler fuer automatische Turn-Wechsel.

## Phase 2: Liberation-aehnliches Dashboard

- Karte mit Airbases, Frontlinien und Objectives.
- Kampagnenpanel fuer Supplies, Squadrons und Pakete.
- Serverstatus, aktiver Turn, naechster Restart.
- Airbase-, Squadron- und Package-Daten im Campaign-State.

## Phase 3: Kampagnenlogik

- Erweiterte Airbase Ownership und Capture-Regeln.
- Ground Units, Supply Depots, Factories.
- Squadron- und Aircraft-Pool mit echten Verlusten und Reparaturzyklen.
- Attrition, Repair, Reinforcement.
- Bodendruck und Supply wirken auf Objective-Kontrolle und Airbase Capture.

## Phase 4: DCS Integration

- Mission-Dateien erzeugen oder vorbereiten.
- Template-Anker aus Mission Editor erkennen und mit Campaign-Objectives verknuepfen.
- Weitere DCS-Anker-Typen fuer Airbases, Depots, Spawns, SAM/EWR und Reservepunkte vorbereiten.
- Template-MIZ-Dokumentation in `docs/TEMPLATE_MIZ_GUIDE.md` aktuell halten.
- DCS-Logs und Events einlesen.
- Verluste und Objective-Erfolge automatisch in BattleReports umwandeln.
- Automatischer 6h-Restart mit naechster Mission.

## v0.09: Operational Core

- Echte AI-Units und einfache Routen aus dem MissionPlan in die vorbereitete `.miz` schreiben.
- AI-Packages an `WL_OBJ_*`, `WL_FRONT_*`, `WL_AIRBASE_*`, `WL_HELI_BASE_*` und `WL_FARP_*` Anker binden.
- Airbase- und FARP-Startlogik fuer AI/Helis vorbereiten, inklusive Fallback auf sichere Basen.
- Player-Slots weiterhin unveraendert aus dem Template erhalten.
- DCS-Logs und Missionsergebnisse einlesen.
- Automatische BattleReports aus Kills, Verlusten, Objective-Events und Missionsausgang erzeugen.
- Mission-Result-Format fuer direkte Reports, Eventlisten und JSONL/DCS-Log-Exports dokumentieren.
- Warehouse/Fuel/Ammo-Patching fuer einfache Supply-Wirkung pro Base vorbereiten.
- Warehouse-Shadow-Block in generierte `.miz` schreiben, bis echte DCS-Warehouse-IDs sicher gemappt sind.
- Bekannte Caucasus-Airport-Warehouse-IDs fuer Fuel/Operating-Level-Patching mappen.
- Operationalen 6h-Loop herstellen: Mission vorbereiten, Server starten, Turn laufen lassen, Ergebnis importieren, naechste Mission erzeugen.
- Minimaler Admin-Testbetrieb mit mehreren Spielern auf dem Server.

## Phase 5: 24/7 Deployment

- Windows-Service Installation.
- Log-Rotation und Healthchecks.
- Backup des Campaign-State.
- Admin-Login, Token-Rotation und IP-Allowlist.

## Nach Phase 4: UI Cleanup / Navigation

- Dashboard in klare Tabs aufteilen: Campaign, Forces, Mission, Server, History.
- Oberen Statusbereich auf Serverstatus, aktuellen Turn, naechsten Restart und Warnungen reduzieren.
- Mission Template, Mission Plan Vorschau, Export und MIZ-Vorbereitung in einen eigenen Mission-Tab verschieben.
- Forces-Ansicht fuer Squadrons, Packages, Ground War, Supply und Factories zusammenfassen.
- Turn History und spaetere Battle Reports/Log-Auswertung in einen eigenen History-Tab auslagern.
- Keine neuen Features in diesem Block, nur bessere Bedienbarkeit und Uebersicht.

## Phase 5 Polish: Multiplayer Balance

- Spieleranzahl und Aktivitaet pro Coalition aus DCS-Logs oder Serverstatus erfassen.
- Unterbesetzte Coalition erhaelt ein AI-Support-Budget.
- AI-Budget in zusaetzliche CAP, Intercept, CAS oder Strike Packages uebersetzen.
- Kompensation nur teilweise anwenden, damit Spielerleistung weiterhin wichtig bleibt.
- Player-Imbalance und AI-Kompensation in Turn History speichern.
- UI-Hinweis, wenn ein Turn durch Spieler-Ungleichgewicht kompensiert wurde.

## Nach v0.1: Campaign Setup Wizards

- Neue Campaign ueber UI erstellen.
- Theater, Hauptbasen und Turn-Dauer auswaehlen.
- Player-Slots pro Hauptbasis konfigurieren, ohne sie spaeter automatisch zu entfernen.
- Squadrons, Ground Units, Supply Depots und Factories ueber Formulare anlegen.
- Template-Mission auswaehlen und mit Campaign-Daten verknuepfen.
- Balancing-Presets fuer Attrition, Repair, Supply und Reinforcement einstellen.
- Server- und Remote-Settings gefuehrt einrichten.

## v0.2: Real Weather und Turn Atmosphaere

- Reales METAR-Wetter fuer das Campaign-Gebiet einlesen.
- Alle 6h Turn-Wechsel neue Wetterdaten anwenden.
- Luftdruck, Temperatur, Wind, Wolken, Sicht und Niederschlag in Mission-Daten abbilden.
- Uhrzeit/Datum pro Turn fortschreiben oder optional realer Serverzeit folgen.
- Wetter-Fallbacks fuer Offline-Betrieb und nicht erreichbare METAR-Quellen.
- UI-Anzeige fuer aktuelles und naechstes Turn-Wetter.
