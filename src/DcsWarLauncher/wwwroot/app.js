const els = {
  tabButtons: document.querySelectorAll("[data-tab-target]"),
  tabPanels: document.querySelectorAll("[data-tab-panel]"),
  serverPill: document.querySelector("#serverPill"),
  token: document.querySelector("#token"),
  missionPath: document.querySelector("#missionPath"),
  generatedMissionName: document.querySelector("#generatedMissionName"),
  generatedMissionMeta: document.querySelector("#generatedMissionMeta"),
  useGeneratedMissionBtn: document.querySelector("#useGeneratedMissionBtn"),
  missionResultName: document.querySelector("#missionResultName"),
  missionResultMeta: document.querySelector("#missionResultMeta"),
  importMissionResultBtn: document.querySelector("#importMissionResultBtn"),
  advanceFromResultBtn: document.querySelector("#advanceFromResultBtn"),
  startBtn: document.querySelector("#startBtn"),
  stopBtn: document.querySelector("#stopBtn"),
  actionMessage: document.querySelector("#actionMessage"),
  turnMessage: document.querySelector("#turnMessage"),
  schedulerEnabled: document.querySelector("#schedulerEnabled"),
  schedulerPoll: document.querySelector("#schedulerPoll"),
  schedulerChecked: document.querySelector("#schedulerChecked"),
  schedulerRun: document.querySelector("#schedulerRun"),
  schedulerMessage: document.querySelector("#schedulerMessage"),
  campaignName: document.querySelector("#campaignName"),
  campaignCreated: document.querySelector("#campaignCreated"),
  theater: document.querySelector("#theater"),
  turn: document.querySelector("#turn"),
  phase: document.querySelector("#phase"),
  turnDuration: document.querySelector("#turnDuration"),
  turnEnds: document.querySelector("#turnEnds"),
  turnRemaining: document.querySelector("#turnRemaining"),
  blueSupply: document.querySelector("#blueSupply"),
  redSupply: document.querySelector("#redSupply"),
  blueSuccess: document.querySelector("#blueSuccess"),
  redSuccess: document.querySelector("#redSuccess"),
  blueLosses: document.querySelector("#blueLosses"),
  redLosses: document.querySelector("#redLosses"),
  airSuperiority: document.querySelector("#airSuperiority"),
  advanceTurnBtn: document.querySelector("#advanceTurnBtn"),
  saveStateBtn: document.querySelector("#saveStateBtn"),
  resetCampaignBtn: document.querySelector("#resetCampaignBtn"),
  prepareSmokeStateBtn: document.querySelector("#prepareSmokeStateBtn"),
  checkReadinessBtn: document.querySelector("#checkReadinessBtn"),
  readinessStatus: document.querySelector("#readinessStatus"),
  readinessSummary: document.querySelector("#readinessSummary"),
  readinessItems: document.querySelector("#readinessItems"),
  previewMissionPlanBtn: document.querySelector("#previewMissionPlanBtn"),
  exportMissionPlanBtn: document.querySelector("#exportMissionPlanBtn"),
  prepareMissionBtn: document.querySelector("#prepareMissionBtn"),
  inspectTemplateBtn: document.querySelector("#inspectTemplateBtn"),
  refreshBtn: document.querySelector("#refreshBtn"),
  templateFile: document.querySelector("#templateFile"),
  templateTheater: document.querySelector("#templateTheater"),
  templateStatus: document.querySelector("#templateStatus"),
  templateSlots: document.querySelector("#templateSlots"),
  templateBlueSlots: document.querySelector("#templateBlueSlots"),
  templateRedSlots: document.querySelector("#templateRedSlots"),
  templateAnchorCount: document.querySelector("#templateAnchorCount"),
  templateWarnings: document.querySelector("#templateWarnings"),
  templateAnchors: document.querySelector("#templateAnchors"),
  templateGroups: document.querySelector("#templateGroups"),
  missionPlanPreview: document.querySelector("#missionPlanPreview"),
  objectives: document.querySelector("#objectives"),
  frontlineMap: document.querySelector("#frontlineMap"),
  aiPlan: document.querySelector("#aiPlan"),
  missionPackages: document.querySelector("#missionPackages"),
  turnHistory: document.querySelector("#turnHistory"),
  squadrons: document.querySelector("#squadrons"),
  groundUnits: document.querySelector("#groundUnits"),
  supplyDepots: document.querySelector("#supplyDepots"),
  factories: document.querySelector("#factories")
};

let currentState = null;
let latestGeneratedMission = null;
let latestMissionResult = null;

function activateTab(tabName) {
  for (const button of els.tabButtons) {
    button.classList.toggle("active", button.dataset.tabTarget === tabName);
  }

  for (const panel of els.tabPanels) {
    panel.classList.toggle("active", panel.dataset.tabPanel === tabName);
  }
}

function authHeaders() {
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${els.token.value}`
  };
}

async function loadStatus() {
  const response = await fetch("/api/server/status");
  const status = await response.json();
  els.serverPill.textContent = status.isRunning
    ? `Laeuft: PID ${status.processId}`
    : "Server offline";
  els.serverPill.classList.toggle("running", status.isRunning);
}

async function loadScheduler() {
  const response = await fetch("/api/scheduler/status");
  const scheduler = await response.json();
  els.schedulerEnabled.textContent = scheduler.enabled
    ? scheduler.isProcessing ? "Verarbeitet" : "Aktiv"
    : "Aus";
  els.schedulerPoll.textContent = `${scheduler.pollSeconds}s`;
  els.schedulerChecked.textContent = scheduler.lastCheckedUtc
    ? new Date(scheduler.lastCheckedUtc).toLocaleTimeString()
    : "-";
  els.schedulerRun.textContent = scheduler.lastRunUtc
    ? new Date(scheduler.lastRunUtc).toLocaleString()
    : "-";
  els.schedulerMessage.textContent = scheduler.lastMessage;
}

async function loadGeneratedMission() {
  const response = await fetch("/api/mission/generated/latest");
  latestGeneratedMission = await response.json();

  if (!latestGeneratedMission.exists) {
    els.generatedMissionName.textContent = "Keine vorbereitet";
    els.generatedMissionMeta.textContent = "-";
    els.useGeneratedMissionBtn.disabled = true;
    return;
  }

  const modified = latestGeneratedMission.lastModifiedUtc
    ? new Date(latestGeneratedMission.lastModifiedUtc).toLocaleString()
    : "-";
  const sizeKb = latestGeneratedMission.sizeBytes
    ? `${Math.round(latestGeneratedMission.sizeBytes / 1024)} KB`
    : "-";

  els.generatedMissionName.textContent = latestGeneratedMission.mizFileName;
  els.generatedMissionMeta.textContent = `${sizeKb} - ${modified}`;
  els.useGeneratedMissionBtn.disabled = false;
}

async function loadMissionResultStatus() {
  const response = await fetch("/api/mission/results/latest");
  latestMissionResult = await response.json();

  if (!latestMissionResult.exists) {
    els.missionResultName.textContent = "Keine Ergebnisdatei";
    els.missionResultMeta.textContent = "Data/Results ist leer.";
    els.importMissionResultBtn.disabled = true;
    els.advanceFromResultBtn.disabled = true;
    return;
  }

  const modified = latestMissionResult.lastModifiedUtc
    ? new Date(latestMissionResult.lastModifiedUtc).toLocaleString()
    : "-";
  const sizeKb = latestMissionResult.sizeBytes
    ? `${Math.round(latestMissionResult.sizeBytes / 1024)} KB`
    : "-";

  els.missionResultName.textContent = latestMissionResult.fileName;
  els.missionResultMeta.textContent = `${sizeKb} - ${modified}`;
  els.importMissionResultBtn.disabled = false;
  els.advanceFromResultBtn.disabled = false;
}

async function loadReadiness() {
  const response = await fetch("/api/readiness/v008");
  const report = await response.json().catch(() => null);
  if (!response.ok || !report) {
    els.readinessStatus.textContent = "Fehler";
    els.readinessStatus.className = "bad-text";
    els.readinessSummary.textContent = "Readiness konnte nicht geladen werden.";
    els.readinessItems.innerHTML = "";
    return;
  }

  els.readinessStatus.textContent = report.isReady ? "Bereit" : "Blockiert";
  els.readinessStatus.className = report.isReady ? "ok-text" : "bad-text";
  els.readinessSummary.textContent = report.summary;
  renderReadinessItems(report.items || []);
}

async function prepareSmokeState() {
  els.readinessSummary.textContent = "Bereite v0.08 Smoke-State vor...";
  const response = await fetch("/api/readiness/v008/prepare-smoke-state", {
    method: "POST",
    headers: authHeaders()
  });

  const report = await response.json().catch(() => null);
  if (!response.ok || !report) {
    els.readinessStatus.textContent = "Fehler";
    els.readinessStatus.className = "bad-text";
    els.readinessSummary.textContent = response.status === 401
      ? "Host Token fehlt oder ist ungueltig. Bitte im Server-Tab eintragen."
      : "Smoke-State konnte nicht vorbereitet werden.";
    return;
  }

  els.readinessStatus.textContent = report.isReady ? "Bereit" : "Blockiert";
  els.readinessStatus.className = report.isReady ? "ok-text" : "bad-text";
  els.readinessSummary.textContent = "Smoke-State vorbereitet. Vorheriger State wurde als Backup gesichert.";
  renderReadinessItems(report.items || []);
  await loadState();
}

function renderReadinessItems(items) {
  els.readinessItems.innerHTML = "";
  for (const item of items) {
    const card = document.createElement("article");
    card.className = `readiness-item ${item.status}`;
    card.innerHTML = `
      <header>
        <strong>${item.name}</strong>
        <span>${item.status}</span>
      </header>
      <small>${item.message}</small>
    `;
    els.readinessItems.appendChild(card);
  }
}

function useGeneratedMission() {
  if (!latestGeneratedMission?.exists || !latestGeneratedMission.mizFilePath) {
    els.actionMessage.textContent = "Keine vorbereitete Turn-MIZ gefunden.";
    return;
  }

  els.missionPath.value = latestGeneratedMission.mizFilePath;
  els.actionMessage.textContent = "Letzte Turn-MIZ als Startmission uebernommen.";
}

async function loadState() {
  const response = await fetch("/api/war/state");
  currentState = await response.json();
  els.campaignName.textContent = currentState.campaignName || "Campaign";
  els.campaignCreated.textContent = currentState.createdUtc
    ? new Date(currentState.createdUtc).toLocaleDateString()
    : "-";
  els.theater.textContent = currentState.theater;
  els.turn.textContent = currentState.turn;
  els.phase.textContent = currentState.phase;
  els.turnDuration.textContent = `${currentState.turnDurationHours}h`;
  els.turnEnds.textContent = new Date(currentState.currentTurnEndsUtc).toLocaleString();
  els.blueSupply.value = currentState.blueSupply;
  els.redSupply.value = currentState.redSupply;
  renderObjectives();
  renderFrontlines();
  renderAiPlan();
  renderMissionPackages();
  renderTurnHistory();
  renderSquadrons();
  renderGroundUnits();
  renderSupplyDepots();
  renderFactories();
  updateRemaining();
}

async function loadTemplateInspection() {
  const response = await fetch("/api/mission/template/inspect");
  const template = await response.json();
  renderTemplateInspection(template);
}

function renderTemplateInspection(template) {
  const groups = template.clientGroups || [];
  const anchors = template.anchors || [];
  const blueSlots = groups
    .filter(group => group.coalition === "blue")
    .reduce((sum, group) => sum + group.clientUnits, 0);
  const redSlots = groups
    .filter(group => group.coalition === "red")
    .reduce((sum, group) => sum + group.clientUnits, 0);

  els.templateFile.textContent = template.fileName || "Keine .miz";
  els.templateTheater.textContent = template.theater || "-";
  els.templateStatus.textContent = template.isReadable ? "Lesbar" : "Fehler";
  els.templateStatus.className = template.isReadable ? "ok-text" : "bad-text";
  els.templateSlots.textContent = template.clientSlotCount ?? 0;
  els.templateBlueSlots.textContent = blueSlots;
  els.templateRedSlots.textContent = redSlots;
  els.templateAnchorCount.textContent = anchors.length;

  els.templateWarnings.innerHTML = "";
  for (const warning of template.warnings || []) {
    const item = document.createElement("p");
    item.textContent = warning;
    els.templateWarnings.appendChild(item);
  }

  els.templateAnchors.innerHTML = "";
  for (const anchor of anchors) {
    const item = document.createElement("article");
    item.className = "template-anchor";
    const hasCoordinates = Number.isFinite(anchor.x) && Number.isFinite(anchor.y);
    item.innerHTML = `
      <strong>${anchor.name}</strong>
      <span>${anchor.kind}</span>
      <small>${hasCoordinates ? `x ${Math.round(anchor.x)} / y ${Math.round(anchor.y)}` : "keine Koordinaten erkannt"}</small>
    `;
    els.templateAnchors.appendChild(item);
  }

  els.templateGroups.innerHTML = "";
  for (const group of groups) {
    const item = document.createElement("article");
    item.className = `template-group ${group.coalition}`;
    item.innerHTML = `
      <strong>${group.name}</strong>
      <span>${group.aircraft}</span>
      <small>${group.clientUnits} Client / ${group.aiUnits} AI</small>
    `;
    els.templateGroups.appendChild(item);
  }
}

function renderObjectives() {
  els.objectives.innerHTML = "";
  for (const objective of currentState.objectives || []) {
    const card = document.createElement("article");
    card.className = "objective";
    const ownerClass = objective.owner === "blue" || objective.owner === "red" ? objective.owner : "";
    card.innerHTML = `
      <header>
        <strong>${objective.name}</strong>
        <span class="owner">${objective.owner}</span>
      </header>
      <div class="bar"><span class="${ownerClass}" style="width:${objective.strength}%"></span></div>
    `;
    els.objectives.appendChild(card);
  }
}

function renderFrontlines() {
  els.frontlineMap.innerHTML = "";
  for (const segment of currentState.frontlines || []) {
    const line = document.createElement("div");
    line.className = `frontline ${segment.momentum}`;
    line.style.left = `${segment.startX}%`;
    line.style.top = `${segment.startY}%`;
    line.style.width = `${Math.max(8, Math.abs(segment.endX - segment.startX))}%`;
    line.style.transform = `rotate(${segment.endY - segment.startY}deg)`;
    line.title = `${segment.name}: ${segment.momentum}`;
    els.frontlineMap.appendChild(line);
  }

  for (const airbase of currentState.airbases || []) {
    const marker = document.createElement("button");
    marker.className = `airbase-marker ${airbase.owner}`;
    marker.style.left = `${airbase.x}%`;
    marker.style.top = `${airbase.y}%`;
    marker.title = `${airbase.name} - runway ${airbase.runwayHealth}% - fuel ${airbase.fuel}%`;
    marker.innerHTML = `
      <span class="airbase-name">${airbase.name}</span>
      <span class="airbase-status">${airbase.status}</span>
    `;
    els.frontlineMap.appendChild(marker);
  }

  for (const depot of currentState.supplyDepots || []) {
    const marker = document.createElement("div");
    marker.className = `depot-marker ${depot.coalition}`;
    marker.style.left = `${depot.x}%`;
    marker.style.top = `${depot.y}%`;
    marker.title = `${depot.name} - stores ${depot.stores}%`;
    marker.textContent = depot.name.replace(" Depot", "");
    els.frontlineMap.appendChild(marker);
  }

  for (const objective of currentState.objectives || []) {
    const marker = document.createElement("div");
    marker.className = `map-objective ${objective.owner}`;
    marker.textContent = objective.name;
    marker.style.left = `${18 + currentState.objectives.indexOf(objective) * 20}%`;
    marker.style.top = `${78 - objective.strength / 2}%`;
    els.frontlineMap.appendChild(marker);
  }
}

function renderGroundUnits() {
  els.groundUnits.innerHTML = "";
  for (const unit of currentState.groundUnits || []) {
    const card = document.createElement("article");
    card.className = `ground-card ${unit.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${unit.name}</strong>
        <span>${unit.posture}</span>
      </header>
      <div class="squadron-meta">
        <span>${unit.type}</span>
        <span>${unit.location}</span>
      </div>
      ${meter("Strength", unit.strength, unit.coalition)}
      ${meter("Supply", unit.supply, unit.coalition)}
      ${meter("Readiness", unit.readiness, unit.coalition)}
    `;
    els.groundUnits.appendChild(card);
  }
}

function renderSupplyDepots() {
  els.supplyDepots.innerHTML = "";
  for (const depot of currentState.supplyDepots || []) {
    const card = document.createElement("article");
    card.className = `depot-card ${depot.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${depot.name}</strong>
        <span>${depot.status}</span>
      </header>
      <small>${depot.location}</small>
      ${meter("Stores", depot.stores, depot.coalition)}
    `;
    els.supplyDepots.appendChild(card);
  }
}

function renderFactories() {
  els.factories.innerHTML = "";
  for (const factory of currentState.factories || []) {
    const card = document.createElement("article");
    card.className = `factory-card ${factory.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${factory.name}</strong>
        <span>${factory.status}</span>
      </header>
      <div class="squadron-meta">
        <span>${factory.location}</span>
        <span>${factory.outputType}</span>
      </div>
      ${meter("Health", factory.health, factory.coalition)}
      <div class="meter-row">
        <span>Production</span>
        <strong>${factory.production}</strong>
      </div>
    `;
    els.factories.appendChild(card);
  }
}

function meter(label, value, coalition) {
  return `
    <div class="meter-row">
      <span>${label}</span>
      <strong>${value}%</strong>
    </div>
    <div class="bar"><span class="${coalition}" style="width:${value}%"></span></div>
  `;
}

function renderAiPlan() {
  els.aiPlan.innerHTML = "";
  for (const order of currentState.aiPlan || []) {
    const card = document.createElement("article");
    card.className = `ai-order ${order.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${order.coalition}</strong>
        <span>${order.confidence}%</span>
      </header>
      <p>${order.task}</p>
      <small>${order.target}</small>
    `;
    els.aiPlan.appendChild(card);
  }
}

function renderMissionPackages() {
  els.missionPackages.innerHTML = "";
  for (const pack of currentState.missionPackages || []) {
    const card = document.createElement("article");
    card.className = `package-card ${pack.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${pack.id}</strong>
        <span>${pack.status}</span>
      </header>
      <div class="package-task">${pack.task}</div>
      <small>${pack.aircraftCount} ship - ${pack.squadron}</small>
      <small>${pack.target}</small>
    `;
    els.missionPackages.appendChild(card);
  }
}

function renderTurnHistory() {
  els.turnHistory.innerHTML = "";
  const history = [...(currentState.turnHistory || [])]
    .sort((left, right) => right.turn - left.turn)
    .slice(0, 5);

  if (history.length === 0) {
    const empty = document.createElement("p");
    empty.className = "empty-state";
    empty.textContent = "Noch keine abgeschlossenen Turns.";
    els.turnHistory.appendChild(empty);
    return;
  }

  for (const entry of history) {
    const report = entry.battleReport || {};
    const item = document.createElement("article");
    item.className = "history-row";
    item.innerHTML = `
      <div>
        <strong>Turn ${entry.turn}</strong>
        <span>${entry.summary}</span>
      </div>
      <dl>
        <div><dt>Blue</dt><dd>${report.blueMissionSuccess ?? 0}</dd></div>
        <div><dt>Red</dt><dd>${report.redMissionSuccess ?? 0}</dd></div>
        <div><dt>B Loss</dt><dd>${report.blueLosses ?? 0}</dd></div>
        <div><dt>R Loss</dt><dd>${report.redLosses ?? 0}</dd></div>
        <div><dt>Air</dt><dd>${report.airSuperiority ?? 0}</dd></div>
      </dl>
    `;
    els.turnHistory.appendChild(item);
  }
}

function renderSquadrons() {
  els.squadrons.innerHTML = "";
  for (const squadron of currentState.squadrons || []) {
    const readiness = squadron.aircraftTotal === 0
      ? 0
      : Math.round((squadron.aircraftReady / squadron.aircraftTotal) * 100);
    const card = document.createElement("article");
    card.className = `squadron-card ${squadron.coalition}`;
    card.innerHTML = `
      <header>
        <strong>${squadron.name}</strong>
        <span>${squadron.aircraft}</span>
      </header>
      <div class="squadron-meta">
        <span>${squadron.homeBase}</span>
        <span>${squadron.aircraftReady}/${squadron.aircraftTotal} ready</span>
      </div>
      <div class="bar"><span class="${squadron.coalition}" style="width:${readiness}%"></span></div>
      <small>Pilot readiness ${squadron.pilotReadiness}%</small>
    `;
    els.squadrons.appendChild(card);
  }
}

function updateRemaining() {
  if (!currentState) return;
  const remainingMs = new Date(currentState.currentTurnEndsUtc).getTime() - Date.now();
  if (remainingMs <= 0) {
    els.turnRemaining.textContent = "bereit";
    return;
  }

  const hours = Math.floor(remainingMs / 3600000);
  const minutes = Math.floor((remainingMs % 3600000) / 60000);
  els.turnRemaining.textContent = `${hours}h ${minutes}m`;
}

async function startServer() {
  els.actionMessage.textContent = "Starte...";
  const response = await fetch("/api/server/start", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify({ missionPath: els.missionPath.value || null })
  });
  const result = await response.json().catch(() => ({ message: "Unauthorized oder ungueltige Antwort." }));
  els.actionMessage.textContent = result.message;
  await loadStatus();
}

async function stopServer() {
  els.actionMessage.textContent = "Stoppe...";
  const response = await fetch("/api/server/stop", {
    method: "POST",
    headers: authHeaders()
  });
  const result = await response.json().catch(() => ({ message: "Unauthorized oder ungueltige Antwort." }));
  els.actionMessage.textContent = result.message;
  await loadStatus();
}

async function saveState() {
  currentState.blueSupply = Number(els.blueSupply.value);
  currentState.redSupply = Number(els.redSupply.value);
  const response = await fetch("/api/war/state", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(currentState)
  });
  if (!response.ok) {
    els.actionMessage.textContent = "State konnte nicht gespeichert werden.";
    return;
  }

  currentState = await response.json();
  els.actionMessage.textContent = "State gespeichert.";
  await loadState();
}

async function resetCampaign() {
  const confirmed = window.confirm("Campaign wirklich auf Turn 1 zuruecksetzen? Der aktuelle State wird vorher als Backup gesichert.");
  if (!confirmed) {
    return;
  }

  els.actionMessage.textContent = "Setze Campaign auf Turn 1 zurueck...";
  const response = await fetch("/api/war/reset-default", {
    method: "POST",
    headers: authHeaders()
  });

  const state = await response.json().catch(() => null);
  if (!response.ok || !state) {
    els.actionMessage.textContent = response.status === 401
      ? "Host Token fehlt oder ist ungueltig. Bitte im Server-Tab eintragen."
      : "Campaign konnte nicht zurueckgesetzt werden.";
    return;
  }

  currentState = state;
  els.actionMessage.textContent = "Campaign wurde auf Turn 1 zurueckgesetzt. Vorheriger State wurde gesichert.";
  await loadState();
  await loadReadiness();
}

async function advanceTurn() {
  els.turnMessage.textContent = "AI wertet Turn aus...";
  const report = {
    blueMissionSuccess: Number(els.blueSuccess.value),
    redMissionSuccess: Number(els.redSuccess.value),
    blueLosses: Number(els.blueLosses.value),
    redLosses: Number(els.redLosses.value),
    airSuperiority: Number(els.airSuperiority.value)
  };

  const response = await fetch("/api/war/advance-turn", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(report)
  });

  if (!response.ok) {
    els.turnMessage.textContent = "Turn konnte nicht abgeschlossen werden.";
    return;
  }

  currentState = await response.json();
  els.turnMessage.textContent = `Turn ${currentState.turn} erstellt.`;
  await loadState();
}

async function importMissionResult() {
  els.turnMessage.textContent = "Lade Mission Result...";
  const response = await fetch("/api/mission/results/import", {
    method: "POST",
    headers: authHeaders()
  });

  const result = await response.json().catch(() => null);
  if (!response.ok || !result?.battleReport) {
    els.turnMessage.textContent = response.status === 401
      ? "Host Token fehlt oder ist ungueltig. Bitte im Server-Tab eintragen."
      : result?.error || "Mission Result konnte nicht geladen werden.";
    return;
  }

  applyBattleReport(result.battleReport);
  els.turnMessage.textContent = `BattleReport aus ${result.fileName} geladen.`;
  await loadMissionResultStatus();
}

async function advanceTurnFromResult() {
  els.turnMessage.textContent = "Schliesse Turn aus Mission Result ab...";
  const response = await fetch("/api/war/advance-turn/from-result", {
    method: "POST",
    headers: authHeaders()
  });

  const result = await response.json().catch(() => null);
  if (!response.ok || !result?.state) {
    els.turnMessage.textContent = response.status === 401
      ? "Host Token fehlt oder ist ungueltig. Bitte im Server-Tab eintragen."
      : result?.error || "Turn konnte nicht aus Mission Result abgeschlossen werden.";
    return;
  }

  applyBattleReport(result.battleReport);
  currentState = result.state;
  els.turnMessage.textContent = `Turn ${currentState.turn} aus ${result.fileName} erstellt.`;
  await loadState();
  await loadMissionResultStatus();
}

function applyBattleReport(report) {
  els.blueSuccess.value = report.blueMissionSuccess ?? 0;
  els.redSuccess.value = report.redMissionSuccess ?? 0;
  els.blueLosses.value = report.blueLosses ?? 0;
  els.redLosses.value = report.redLosses ?? 0;
  els.airSuperiority.value = report.airSuperiority ?? 0;
}

async function previewMissionPlan() {
  els.actionMessage.textContent = "Lade Mission Plan Vorschau...";
  const response = await fetch("/api/mission/preview-plan", {
    headers: authHeaders()
  });

  const plan = await response.json().catch(() => null);
  if (!response.ok || !plan) {
    els.actionMessage.textContent = "Mission Plan Vorschau konnte nicht geladen werden.";
    return;
  }

  renderMissionPlanPreview(plan);
  els.actionMessage.textContent = `Mission Plan Vorschau: Turn ${plan.turn}`;
}

function renderMissionPlanPreview(plan) {
  const bindings = plan.templateBindings || {};
  const objectiveAnchors = bindings.objectiveAnchors || [];
  const airbaseAnchors = bindings.airbaseAnchors || [];
  const frontAnchors = bindings.frontAnchors || [];
  const missingObjectiveAnchors = bindings.missingObjectiveAnchors || [];
  const missingAirbaseAnchors = bindings.missingAirbaseAnchors || [];
  const missingAnchors = [...missingObjectiveAnchors, ...missingAirbaseAnchors];

  els.missionPlanPreview.innerHTML = `
    <div class="preview-summary">
      <span>Objective Bindings <strong>${objectiveAnchors.length}</strong></span>
      <span>Airbase Bindings <strong>${airbaseAnchors.length}</strong></span>
      <span>Front Anchors <strong>${frontAnchors.length}</strong></span>
      <span>Fehlende Anker <strong>${missingAnchors.length}</strong></span>
      <span>Packages <strong>${(plan.flightGroups || []).length}</strong></span>
    </div>
    <div class="preview-grid">
      <section>
        <h3>Objectives</h3>
        <div class="preview-list" id="previewObjectiveAnchors"></div>
      </section>
      <section>
        <h3>Front</h3>
        <div class="preview-list" id="previewFrontAnchors"></div>
      </section>
      <section>
        <h3>Airbases</h3>
        <div class="preview-list" id="previewAirbaseAnchors"></div>
      </section>
      <section>
        <h3>Fehlende Anker</h3>
        <div class="preview-list" id="previewMissingAnchors"></div>
      </section>
    </div>
  `;

  const objectiveList = els.missionPlanPreview.querySelector("#previewObjectiveAnchors");
  for (const anchor of objectiveAnchors) {
    const item = document.createElement("article");
    item.className = `preview-item ${anchor.coalition}`;
    item.innerHTML = `
      <strong>${anchor.objective}</strong>
      <span>${anchor.coalition} - ${anchor.anchorName}</span>
      <small>x ${Math.round(anchor.x)} / y ${Math.round(anchor.y)}</small>
    `;
    objectiveList.appendChild(item);
  }

  const frontList = els.missionPlanPreview.querySelector("#previewFrontAnchors");
  for (const anchor of frontAnchors) {
    const item = document.createElement("article");
    item.className = "preview-item front";
    item.innerHTML = `
      <strong>${anchor.anchorName}</strong>
      <span>Sequence ${anchor.sequence}</span>
      <small>x ${Math.round(anchor.x)} / y ${Math.round(anchor.y)}</small>
    `;
    frontList.appendChild(item);
  }

  const airbaseList = els.missionPlanPreview.querySelector("#previewAirbaseAnchors");
  for (const anchor of airbaseAnchors) {
    const item = document.createElement("article");
    item.className = "preview-item airbase";
    item.innerHTML = `
      <strong>${anchor.airbase}</strong>
      <span>${anchor.anchorType} - ${anchor.anchorName}</span>
      <small>x ${Math.round(anchor.x)} / y ${Math.round(anchor.y)}</small>
    `;
    airbaseList.appendChild(item);
  }

  const missingList = els.missionPlanPreview.querySelector("#previewMissingAnchors");
  for (const missing of missingObjectiveAnchors) {
    const item = document.createElement("article");
    item.className = `preview-item missing ${missing.coalition}`;
    item.innerHTML = `
      <strong>${missing.objective}</strong>
      <span>${missing.coalition}</span>
      <small>${(missing.expectedAnchorNames || []).join(", ")}</small>
    `;
    missingList.appendChild(item);
  }

  for (const missing of missingAirbaseAnchors) {
    const item = document.createElement("article");
    item.className = "preview-item missing airbase";
    item.innerHTML = `
      <strong>${missing.airbase}</strong>
      <span>${missing.anchorType}</span>
      <small>${(missing.expectedAnchorNames || []).join(", ")}</small>
    `;
    missingList.appendChild(item);
  }
}

async function exportMissionPlan() {
  els.actionMessage.textContent = "Exportiere Mission Plan...";
  const response = await fetch("/api/mission/export-plan", {
    method: "POST",
    headers: authHeaders()
  });

  const result = await response.json().catch(() => ({ fileName: null }));
  if (!response.ok || !result.fileName) {
    els.actionMessage.textContent = "Mission Plan konnte nicht exportiert werden.";
    return;
  }

  els.actionMessage.textContent = `Mission Plan exportiert: ${result.fileName}`;
}

async function prepareMission() {
  els.actionMessage.textContent = "Bereite Turn-MIZ vor...";
  const response = await fetch("/api/mission/prepare", {
    method: "POST",
    headers: authHeaders()
  });

  const result = await response.json().catch(() => ({ mizFileName: null, error: null }));
  if (!response.ok || !result.mizFileName) {
    els.actionMessage.textContent = result.error || "Turn-MIZ konnte nicht vorbereitet werden.";
    return;
  }

  els.actionMessage.textContent = `Turn-MIZ vorbereitet: ${result.mizFileName}`;
  await loadGeneratedMission();
}

els.startBtn.addEventListener("click", startServer);
els.stopBtn.addEventListener("click", stopServer);
els.useGeneratedMissionBtn.addEventListener("click", useGeneratedMission);
els.importMissionResultBtn.addEventListener("click", importMissionResult);
els.advanceFromResultBtn.addEventListener("click", advanceTurnFromResult);
els.advanceTurnBtn.addEventListener("click", advanceTurn);
els.saveStateBtn.addEventListener("click", saveState);
els.resetCampaignBtn.addEventListener("click", resetCampaign);
els.prepareSmokeStateBtn.addEventListener("click", prepareSmokeState);
els.checkReadinessBtn.addEventListener("click", loadReadiness);
els.previewMissionPlanBtn.addEventListener("click", previewMissionPlan);
els.exportMissionPlanBtn.addEventListener("click", exportMissionPlan);
els.prepareMissionBtn.addEventListener("click", prepareMission);
els.inspectTemplateBtn.addEventListener("click", loadTemplateInspection);
for (const button of els.tabButtons) {
  button.addEventListener("click", () => activateTab(button.dataset.tabTarget));
}
els.refreshBtn.addEventListener("click", async () => {
  await loadStatus();
  await loadScheduler();
  await loadGeneratedMission();
  await loadMissionResultStatus();
  await loadReadiness();
  await loadState();
  await loadTemplateInspection();
});

loadStatus();
loadScheduler();
loadGeneratedMission();
loadMissionResultStatus();
loadReadiness();
loadState();
loadTemplateInspection();
setInterval(loadStatus, 5000);
setInterval(loadScheduler, 10000);
setInterval(updateRemaining, 30000);
