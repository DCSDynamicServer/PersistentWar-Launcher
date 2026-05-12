# DCS Persistent War Launcher

Ein MVP fuer einen DCS-Liberation-aehnlichen Persistent-War-Manager mit integriertem
Remote-Starter, 6-Stunden-Turns, Frontlinienberechnung und AI-Planung.

Das Ziel ist ein 24/7-faehiges System fuer einen Windows-basierten DCS Dedicated Server,
zum Beispiel auf einem Hetzner Dedicated Server.

## Start

```powershell
dotnet run --project .\src\DcsWarLauncher\DcsWarLauncher.csproj
```

Danach im Browser oeffnen:

```text
http://localhost:5055
```

## Konfiguration

Passe `src/DcsWarLauncher/appsettings.json` an:

- `DcsExecutablePath`: Pfad zu `DCS.exe` oder DCS Dedicated Server.
- `DefaultMissionPath`: Standard-`.miz`, die gestartet wird.
- `StartArguments`: CLI-Argumente fuer DCS. `{mission}` wird durch den Mission-Pfad ersetzt.
- `RemoteToken`: Token fuer Remote-Start, Stop und State-Speichern.
- `DataRoot`: Optional, aber fuer Serverbetrieb empfohlen. Fester Pfad zum Launcher-Data-Ordner, damit Templates, Generated-MIZ, Results und `war-state.json` unabhaengig vom EXE-Startordner gefunden werden.

Scheduler fuer 24/7-Betrieb:

- `Scheduler:Enabled`: Aktiviert den Hintergrunddienst.
- `Scheduler:PollSeconds`: Wie oft der aktive Turn geprueft wird.
- `Scheduler:AdvanceWhenTurnExpired`: Schliesst abgelaufene Turns automatisch ab.
- `Scheduler:AutoStopServer`: Stoppt DCS vor der Turn-Auswertung, falls es vom Launcher gestartet wurde.
- `Scheduler:AutoStartServer`: Startet DCS nach der Turn-Auswertung wieder.

## Remote API

Start:

```powershell
Invoke-RestMethod `
  -Uri http://SERVER-IP:5055/api/server/start `
  -Method Post `
  -Headers @{ Authorization = "Bearer change-me-before-remote-use" } `
  -ContentType "application/json" `
  -Body '{"missionPath":null}'
```

Stop:

```powershell
Invoke-RestMethod `
  -Uri http://SERVER-IP:5055/api/server/stop `
  -Method Post `
  -Headers @{ Authorization = "Bearer change-me-before-remote-use" }
```

Turn auswerten und neue Frontlinien/AI-Orders erzeugen:

```powershell
Invoke-RestMethod `
  -Uri http://SERVER-IP:5055/api/war/advance-turn `
  -Method Post `
  -Headers @{ Authorization = "Bearer change-me-before-remote-use" } `
  -ContentType "application/json" `
  -Body '{
    "blueMissionSuccess": 10,
    "redMissionSuccess": 8,
    "blueLosses": 4,
    "redLosses": 6,
    "airSuperiority": 3
  }'
```

Scheduler-Status:

```powershell
Invoke-RestMethod -Uri http://SERVER-IP:5055/api/scheduler/status
```

## Kampagnenmodell

Der aktuelle Stand liegt in `src/DcsWarLauncher/Data/war-state.json`.

- Ein Turn dauert standardmaessig 6 Stunden.
- Nach jedem Turn wird ein `BattleReport` verarbeitet.
- Objectives wechseln je nach Druck, Verlusten und Luftueberlegenheit den Besitzer.
- Frontlinien werden aus benachbarten Objectives erzeugt.
- AI-Orders sind aktuell deterministisch, aber bewusst als austauschbarer Kern gebaut.
- Airbases, Squadrons und Mission Packages werden im Campaign-State mitgefuehrt.
- Ground Units und Supply Depots beeinflussen ab jetzt Objectives, Airbase-Capture und Attrition.

## 24/7 Betrieb

Im lokalen Entwicklungsmodus ist `Scheduler:Enabled` standardmaessig `false`, damit nicht
versehentlich ein lokaler DCS-Prozess gestartet oder gestoppt wird.

Auf dem DCS-Host setzt du:

```json
{
  "Scheduler": {
    "Enabled": true,
    "PollSeconds": 30,
    "AutoStopServer": true,
    "AutoStartServer": true,
    "AdvanceWhenTurnExpired": true
  }
}
```

Wenn der aktive Turn abgelaufen ist, erzeugt der Scheduler automatisch den naechsten
Campaign-State. Sobald DCS-Pfade und Mission-Export fertig sind, wird daraus der komplette
6h-Zyklus: stoppen, auswerten, neue Mission erzeugen, neu starten.

## Naechste Module

- Windows-Service-Install fuer echten 24/7-Betrieb.
- Automatischer Turn Scheduler fuer 6h-Zyklen.
- DCS-Log/Tacview/Event-Parser fuer echte Verluste und Treffer.
- Liberation-aehnliche Kampagnenlogik fuer Airbases, Squadrons, Pakete und Ground War.
- Mission-Generator fuer `.miz` Dateien.
- Template-MIZ-Regeln und Naming-Konventionen: siehe `docs/TEMPLATE_MIZ_GUIDE.md`.
- LLM/AI-Adapter fuer dynamische Strategie.
- Rollenmodell fuer Admin, Game Master und Piloten.
