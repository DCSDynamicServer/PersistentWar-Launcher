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
- DCS-Logs und Events einlesen.
- Verluste und Objective-Erfolge automatisch in BattleReports umwandeln.
- Automatischer 6h-Restart mit naechster Mission.

## Phase 5: 24/7 Deployment

- Windows-Service Installation.
- Log-Rotation und Healthchecks.
- Backup des Campaign-State.
- Admin-Login, Token-Rotation und IP-Allowlist.

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
