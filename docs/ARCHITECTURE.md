# Architektur

Das Projekt soll wie ein eigenstaendiger Persistent-War-Manager funktionieren, nicht als
direkter Fork von DCS Liberation. Bediengefuehl und Kampagnenidee duerfen aehnlich sein,
Logik und Code bleiben eigene Implementierung.

## Hauptmodule

- `DcsWarLauncher.Domain`: Kampagnenzustand, Objectives, Frontlinien, AI-Orders, Servermodelle.
- `DcsWarLauncher.Campaign`: Turn-Auswertung, Frontlinienbewegung, spaeter Economy und Ground War.
- `DcsWarLauncher.Infrastructure`: JSON/SQLite-State, DCS-Prozesssteuerung, spaeter Windows-Service.
- `wwwroot`: Web-Dashboard fuer Serversteuerung und Kampagnenbedienung.
- `TurnSchedulerService`: Hintergrunddienst fuer 6h-Turn-Automation.

## Campaign State

Der State enthaelt jetzt die Grundobjekte fuer eine Liberation-aehnliche Kampagne:

- `Objectives`: Frontnahe Ziele mit Besitzer und Kontrollstaerke.
- `Airbases`: Flugplaetze mit Besitzer, Runway-Zustand, Fuel und Kartenposition.
- `Squadrons`: Staffeln mit Flugzeugtyp, Homebase, verfuegbaren Maschinen und Pilot Readiness.
- `MissionPackages`: geplante Pakete fuer CAP, Strike, OCA und spaetere Missionserzeugung.
- `GroundUnits`: Bodenverbaende mit Staerke, Supply, Readiness und Posture.
- `SupplyDepots`: Logistikpunkte mit Stores, Status und Kartenposition.
- `Frontlines`: vereinfachte Segmente fuer die Karte.
- `AiPlan`: strategische Absicht pro Koalition.

## Turn-Regeln

Die Turn Engine verarbeitet inzwischen mehrere Ebenen:

- Air Power aus dem BattleReport beeinflusst Objectives, Airbases und Depots.
- Supply Depots fuellen sich auf, verlieren aber Stores unter gegnerischem Druck.
- Ground Units verbrauchen Supply, verlieren Staerke und reorganisieren sich bei schlechter Versorgung.
- Objectives erhalten zusaetzlichen Bodendruck durch lokale Ground Units.
- Airbases koennen durch lokale Ground Control den Besitzer wechseln.
- Squadrons reparieren Maschinen und verlieren Readiness durch Verluste.

## 24/7 Zielbild

1. Der Windows-Service laeuft dauerhaft auf dem DCS-Host.
2. Alle 6 Stunden wird der aktive Turn geschlossen.
3. DCS-Events werden ausgewertet.
4. Die Kampagnen-AI berechnet neue Frontlinien und Orders.
5. Der Mission-Generator schreibt die naechste `.miz`.
6. Der Server startet die neue Mission automatisch.

Der aktuelle Scheduler erledigt bereits Schritt 2 und legt die Orchestrierung fuer Schritt
3 bis 6 an. DCS-Eventauswertung und Mission-Generator sind die naechsten groesseren Module.

## Hetzner Empfehlung

Fuer DCS ist ein Hetzner Dedicated Server sinnvoller als eine kleine Cloud-VM. Wichtig sind
hohe Single-Core-Leistung, genug RAM, schnelle SSD und Windows Server. Die Weboberflaeche
sollte hinter HTTPS, Token-Auth und idealerweise IP-Allowlist laufen.
