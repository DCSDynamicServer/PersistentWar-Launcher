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
