# DCS Persistent War Launcher

Webbasierter Persistent-War-Launcher fuer DCS. Der aktuelle Stand ist ein v0.08 Smoke-Test-Build fuer lokale Tests und erste Server-Vorbereitung.

## v0.08 Smoke-Test

1. Launcher starten.
2. Im Server-Tab den Host Token eintragen.
3. Im Mission-Tab das Mission Template pruefen.
4. Im Mission-Tab den v0.08 Readiness Check ausfuehren.
5. Im Mission-Tab `Turn-MIZ vorbereiten` klicken.
6. Im Server-Tab `Letzte Turn-MIZ` uebernehmen.
7. Die erzeugte `.miz` in DCS laden und Briefing, Slots und AI-Fluege testen.

## Phase 5 Automation Start

Der erste Phase-5-Baustein kapselt einen kompletten Automation-Run:

1. Pruefen, ob der aktuelle Turn abgelaufen ist.
2. Aktuelles Mission Result importieren, falls vorhanden.
3. Bei kaputtem Mission Result abbrechen.
4. Optional DCS stoppen.
5. Naechsten Campaign-State berechnen.
6. Naechste Turn-MIZ vorbereiten.
7. Turn-MIZ in den Server-Missionsordner deployen.
8. Alte War-Launcher-Turn-MIZ-Dateien im Server-Missionsordner entfernen.
9. Optional DCS mit genau dieser deployed Turn-MIZ starten.

Manueller Testlauf:

```powershell
Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5055/api/scheduler/run-once" `
  -Headers @{ Authorization = "Bearer <token>" }
```

Alternativ im Server-Tab `Automation einmal testen` klicken.

Vor dem ersten echten Server-Test zeigt der Server-Tab einen Config-Check:

- DCS exe vorhanden
- Default-Mission vorhanden
- Remote Token gesetzt
- Scheduler-Modus
- AutoStart aktiv oder aus

Wenn AutoStart noch aus ist, arbeitet die Automation im Safe Mode und startet DCS nicht automatisch.

Empfohlene Server-Mission-Strategie:

- Entweder `DefaultMissionPath` zeigt direkt auf die feste Server-MIZ, z.B. `...\Missions\persistent-war-current.miz`.
- Oder `ServerMissionDirectory` zeigt auf den DCS-Missionsordner und `DeployedMissionFileName` bleibt fest, z.B. `persistent-war-current.miz`.
- `CleanupOldTurnMissions` bleibt aktiv.
- DCS startet immer die feste deployed MIZ, nicht zufaellig eine alte Turn-MIZ.

## Wichtiger v0.08 Stand

- Player-Slots bleiben aus der Template-MIZ erhalten.
- AI-Fluege werden in die Turn-MIZ geschrieben.
- Briefing und `war-launcher/mission-plan.json` werden in die Turn-MIZ geschrieben.
- DCS `warehouses` bleiben fuer v0.08 unveraendert.
- Warehouse/Fuel/Ammo-Daten werden nur im `mission-plan.json` exportiert.

## Wichtige Ordner

- `src/DcsWarLauncher/Data/Templates`: Template-MIZ ablegen.
- `src/DcsWarLauncher/Data/Generated`: erzeugte Turn-MIZ.
- `src/DcsWarLauncher/Data/Exports`: exportierte Mission-Plaene.
- `src/DcsWarLauncher/Data/Results`: Missionsergebnisse fuer BattleReports.
- `docs/TEMPLATE_MIZ_GUIDE.md`: Regeln fuer Template-MIZ, WL-Anker und Benennung.

## Lokal starten

```powershell
dotnet run --project .\src\DcsWarLauncher\DcsWarLauncher.csproj --urls http://localhost:5055
```

Danach im Browser `http://localhost:5055` oeffnen.

## Tests

```powershell
dotnet build .\src\DcsWarLauncher\DcsWarLauncher.csproj
dotnet run --project .\tests\DcsWarLauncher.Tests\DcsWarLauncher.Tests.csproj
```
