const els = {
  serverPill: document.querySelector("#serverPill"),
  token: document.querySelector("#token"),
  missionPath: document.querySelector("#missionPath"),
  startBtn: document.querySelector("#startBtn"),
  stopBtn: document.querySelector("#stopBtn"),
  actionMessage: document.querySelector("#actionMessage"),
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
  templateWarnings: document.querySelector("#templateWarnings"),
  templateGroups: document.querySelector("#templateGroups"),
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

  els.templateWarnings.innerHTML = "";
  for (const warning of template.warnings || []) {
    const item = document.createElement("p");
    item.textContent = warning;
    els.templateWarnings.appendChild(item);
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

async function advanceTurn() {
  els.actionMessage.textContent = "AI wertet Turn aus...";
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
    els.actionMessage.textContent = "Turn konnte nicht abgeschlossen werden.";
    return;
  }

  currentState = await response.json();
  els.actionMessage.textContent = `Turn ${currentState.turn} erstellt.`;
  await loadState();
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
}

els.startBtn.addEventListener("click", startServer);
els.stopBtn.addEventListener("click", stopServer);
els.advanceTurnBtn.addEventListener("click", advanceTurn);
els.saveStateBtn.addEventListener("click", saveState);
els.exportMissionPlanBtn.addEventListener("click", exportMissionPlan);
els.prepareMissionBtn.addEventListener("click", prepareMission);
els.inspectTemplateBtn.addEventListener("click", loadTemplateInspection);
els.refreshBtn.addEventListener("click", async () => {
  await loadStatus();
  await loadScheduler();
  await loadState();
  await loadTemplateInspection();
});

loadStatus();
loadScheduler();
loadState();
loadTemplateInspection();
setInterval(loadStatus, 5000);
setInterval(loadScheduler, 10000);
setInterval(updateRemaining, 30000);
