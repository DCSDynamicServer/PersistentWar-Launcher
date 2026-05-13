# Roadmap

## Status

Der Launcher hat den ersten spielbaren Kern:

- ASP.NET Core Backend mit Web UI.
- Persistenter Campaign-State mit 6h-Turns.
- Campaign-, Forces-, Mission-, Server- und History-Tabs.
- Template-MIZ-Erkennung mit WL-Ankern.
- Turn-MIZ-Vorbereitung aus Template.
- AI-Flights werden in die vorbereitete MIZ geschrieben.
- Player-Slots bleiben aus dem Template erhalten.
- Mission Plan Export inklusive Warehouse-Plan.
- DCS Dedicated Server kann per Launcher gestartet und gestoppt werden.
- Letzte Turn-MIZ wird in einen festen DCS-Missionspfad deployed.
- `serverSettings.lua` wird gepatched, damit DCS die deployed MIZ startet.
- Scheduler kann abgelaufene Turns auswerten, neue MIZ erzeugen, deployen und DCS neu starten.
- DCS `dcs.log` kann als Mission Result eingelesen werden.
- Leere `debrief.log` Dateien werden ignoriert.
- Automation Log im Server-Tab.

## Aktueller Live-Test

Ziel: Der Server soll bis zum naechsten Abend durchlaufen und mindestens einen automatischen Turn-Wechsel schaffen.

Pruefen:

- Launcher bleibt aktiv.
- `Scheduler:Enabled`, `AutoStopServer`, `AutoStartServer` und `AdvanceWhenTurnExpired` sind aktiv.
- `MissionResultDirectory` zeigt auf den DCS `Logs` Ordner.
- Mission-Tab erkennt `dcs.log` als Mission Result.
- DCS wird zum Turn-Ende gestoppt.
- Neuer Campaign-State wird berechnet.
- Neue Turn-MIZ wird vorbereitet und deployed.
- DCS startet wieder mit der naechsten Turn-MIZ.
- Automation Log und Turn History zeigen den Ablauf.

Bekannte offene Punkte:

- Das eingebettete MIZ-Runtime-Script laeuft noch nicht zuverlaessig. Fuer den Live-Test wird deshalb `dcs.log` als Result-Quelle verwendet.
- Die Mission selbst beendet sich noch nicht sicher aus der MIZ heraus; der Launcher stoppt DCS extern.

## v0.10: AI Air Logic

Ziel: AI-Fluege sollen nachvollziehbar und passend zur Kampagnenlage geplant werden.

- AI-Package-Rollen sauber trennen: CAP, Intercept, CAS, Strike, SEAD, Escort, Transport.
- Package-Auswahl aus Frontlage, Objectives, Airbase-Status, Supply und Factory-Zielen ableiten.
- AI-Fluege an sinnvolle WL-Anker binden: Objectives, Front, Airbases, Depots, FARPs.
- Sichere Departure-Basen und Fallback-Basen pro Coalition waehlen.
- Heli-AI von FARPs oder Heli-Basen starten lassen.
- Spieleranzahl pro Coalition spaeter als AI-Support-Budget beruecksichtigen.
- Keine AI-Packages erzeugen, wenn keine passenden Flugzeuge, Basen oder Supply vorhanden sind.

## v0.11: Ground War

Ziel: Bodenkrieg soll Frontlinien glaubhaft verschieben.

- Ground Units erhalten Ziele: verteidigen, reorganisieren, angreifen, rueckfallen.
- Bodenverbaende versuchen naechste Objectives, Basen oder Depots zu erreichen.
- Eroberung basiert auf Staerke, Readiness, Supply, Luftueberlegenheit und gegnerischer Verteidigung.
- Bodenverbaende koennen neue Frontabschnitte erzeugen oder alte aufgeben.
- Einheiten verlieren Staerke und Readiness durch Kampf, schlechte Supply und Luftangriffe.
- Neue AI-Bodeneinheiten duerfen nur entstehen, wenn Factories, Depots und Supply-Routen passen.

## v0.12: Warehouse und Supply

Ziel: Supply soll der Motor der Kampagne werden.

- Internes Warehouse-Modell fuer Fuel, Ammo, Ersatzteile und Ground Stores.
- Airbases, Depots, FARPs und Factories erhalten Supply-Werte.
- Supply beeinflusst AI-Fluege, Reparatur, Reinforcement und Ground-Spawns.
- Warehouse/Fuel/Ammo-Patching fuer DCS schrittweise aktivieren.
- Bekannte Caucasus-Airport-Warehouse-IDs pflegen und spaeter fuer andere Maps erweiterbar machen.
- `.miz` Warehouse-Patching erst aktivieren, wenn DCS-Ladestabilitaet bestaetigt ist.

## v0.13: Transport und Supply Flights

Ziel: C-130, Chinook und spaeter Trucks verbinden die Logistik mit der Front.

- C-130 Supply-Fluege zwischen Hauptbasen, Airbases und Depots planen.
- Chinook/Transportheli-Fluege zu FARPs und Frontdepots planen.
- Erfolgreiche Supply-Fluege erhoehen Stores/Fuel/Ammo am Ziel.
- Verlorene oder abgefangene Supply-Fluege reduzieren Nachschub.
- Supply-Escort und Intercept-Missionen aus diesen Transporten ableiten.
- Ground-Spawns und Heli-Spawns an vorhandene Supply koppeln.

## v0.14: FARPs und Heli Operations

Ziel: FARPs sollen automatisch aus der Kampagnenlage entstehen und verschwinden.

- FARP als Campaign-Objekt: Coalition, Position, Status, Fuel, Ammo, Stores, verbundenes Objective.
- Automatische FARP-Erzeugung nahe stabiler oder neu eroberter Frontabschnitte.
- FARP-Aktivierung nur bei ausreichender Supply.
- AI-Helis koennen von aktiven FARPs nachspawnen.
- FARPs koennen durch Frontverlust, Supply-Mangel oder Angriffe deaktiviert werden.
- Template-Anker `WL_FARP_*` und spaeter echte FARP-MIZ-Erzeugung unterstuetzen.

## v0.15: Player Tasking und Kneeboard

Ziel: Spieler sollen pro Turn klare Aufgaben bekommen, ohne externe Flugplanung vorauszusetzen.

- Pro Coalition automatisch Mission Cards erzeugen.
- Objectives, Frontlinien, Zielkoordinaten, Bedrohung und Prioritaeten im Briefing darstellen.
- F10 Marker fuer Front, Ziele, Depots, FARPs und Strike-Ziele erzeugen.
- Kneeboard-Seiten pro Coalition generieren und in die MIZ einbetten.
- Optional spaeter Package-spezifische Kneeboards fuer CAS, CAP, Strike und Transport.
- Optional Export fuer externe Flugplanung/MDC-kompatible Tools.

## v0.16: Multiplayer Balance

Ziel: Ungleich verteilte Spielerzahlen sollen den Krieg nicht kaputtmachen.

- Spieleranzahl und Aktivitaet pro Coalition aus Logs oder Serverstatus erfassen.
- Unterbesetzte Coalition erhaelt begrenztes AI-Support-Budget.
- AI-Support in CAP, Intercept, CAS, Strike oder Supply-Defense uebersetzen.
- Kompensation begrenzen, damit Spielerleistung weiter wichtig bleibt.
- Player-Imbalance und AI-Kompensation in Turn History anzeigen.

## v0.17: Deployment Polish

Ziel: Der Serverbetrieb soll robust und wartbar werden.

- Windows-Service Installation.
- Log-Rotation und Healthchecks.
- Campaign-State Backups pruefbar machen.
- Admin-Login, Token-Rotation und IP-Allowlist.
- Server-Tab weiter vereinfachen: normaler Betrieb mit so wenig Buttons wie moeglich.

## Nach v0.1: Campaign Setup Wizards

Ziel: Neue Campaigns ohne Handarbeit in JSON-Dateien erstellen.

- Neue Campaign ueber UI erstellen.
- Theater, Hauptbasen und Turn-Dauer auswaehlen.
- Player-Slots pro Hauptbasis konfigurieren, ohne sie spaeter automatisch zu entfernen.
- Squadrons, Ground Units, Supply Depots, Factories und FARPs ueber Formulare anlegen.
- Template-Mission auswaehlen und mit Campaign-Daten verknuepfen.
- Balancing-Presets fuer Attrition, Repair, Supply und Reinforcement einstellen.
- Server- und Remote-Settings gefuehrt einrichten.

## v0.2: Real Weather und Turn Atmosphaere

Ziel: Jeder 6h-Turn soll sich wetter- und zeitmaessig lebendig anfuehlen.

- Reales METAR-Wetter fuer das Campaign-Gebiet einlesen.
- Alle 6h Turn-Wechsel neue Wetterdaten anwenden.
- Luftdruck, Temperatur, Wind, Wolken, Sicht und Niederschlag in Mission-Daten abbilden.
- Uhrzeit/Datum pro Turn fortschreiben oder optional realer Serverzeit folgen.
- Wetter-Fallbacks fuer Offline-Betrieb und nicht erreichbare METAR-Quellen.
- UI-Anzeige fuer aktuelles und naechstes Turn-Wetter.
