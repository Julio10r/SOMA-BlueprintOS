/* ══════════════════════════════════════════════════════════════════
   Dashboard Oficial do Projeto — SOMA BlueprintOS / +Compras
   dashboard.js

   RESPONSABILIDADE: exclusivamente visual.
   - NÃO interpreta documentação.
   - NÃO calcula indicadores (nenhuma média, soma, percentual é
     derivado aqui — apenas extraídos literalmente quando já vêm
     prontos no texto-fonte, ex.: "100%" -> 100, ou o primeiro
     número de "56 Work Orders" -> 56, para uso em barras/tiles).
   - NÃO possui regras de negócio.
   Única fonte de dados: .ai/dashboard/DASHBOARD_STATE.md (lido via
   fetch relativo "./DASHBOARD_STATE.md", ou via seleção manual de
   arquivo quando o navegador bloqueia fetch em file://).
   ══════════════════════════════════════════════════════════════════ */

(function () {
  "use strict";

  var STATE_URL = "./DASHBOARD_STATE.md";

  /* ── Parser de markdown (genérico, sem interpretação de negócio) ── */

  function splitRow(line) {
    var s = line.trim();
    if (s.startsWith("|")) s = s.slice(1);
    if (s.endsWith("|")) s = s.slice(0, -1);
    return s.split("|").map(function (c) { return c.trim(); });
  }

  function isTableLine(line) {
    return /^\s*\|/.test(line);
  }

  function isSeparatorRow(cells) {
    return cells.every(function (c) { return /^:?-{2,}:?$/.test(c) || c === ""; });
  }

  function parseTableBlock(blockLines) {
    var rowsRaw = blockLines.map(splitRow);
    var header = rowsRaw[0];
    var body = rowsRaw.slice(1).filter(function (cells) { return !isSeparatorRow(cells); });
    var rows = body.map(function (cells) {
      var o = {};
      header.forEach(function (h, idx) { o[h] = cells[idx] !== undefined ? cells[idx] : ""; });
      return o;
    });
    var kv = null;
    if (header.length === 2 && /campo/i.test(header[0]) && /valor/i.test(header[1])) {
      kv = {};
      rows.forEach(function (r) { kv[r[header[0]]] = r[header[1]]; });
    }
    return { header: header, rows: rows, kv: kv };
  }

  function processSectionLines(lines) {
    var bullets = [];
    var paragraphs = [];
    var table = null;
    var i = 0;
    while (i < lines.length) {
      var line = lines[i];
      if (isTableLine(line)) {
        var block = [];
        while (i < lines.length && isTableLine(lines[i])) { block.push(lines[i]); i++; }
        table = parseTableBlock(block);
        continue;
      }
      if (/^\s*-\s+/.test(line)) {
        bullets.push(line.replace(/^\s*-\s+/, "").trim());
        i++;
        continue;
      }
      if (/^>\s?/.test(line)) { i++; continue; }
      if (line.trim() === "") { i++; continue; }
      paragraphs.push(line.trim());
      i++;
    }
    return { table: table, bullets: bullets, paragraphs: paragraphs };
  }

  function parseDashboardState(md) {
    var lines = md.replace(/\r\n/g, "\n").split("\n");
    var i = 0;
    var metaLines = [];
    while (i < lines.length && !/^##\s+/.test(lines[i])) {
      if (/^>\s?/.test(lines[i])) metaLines.push(lines[i].replace(/^>\s?/, ""));
      i++;
    }
    var metaRaw = metaLines.join(" ");
    var lastUpdateMatch = metaRaw.match(/Última atualização:\*\*\s*([^]+?)(?:\s{2,}|$)/);
    var meta = {
      raw: metaRaw,
      lastUpdate: lastUpdateMatch ? lastUpdateMatch[1].trim() : null
    };

    var sectionsRaw = {};
    var currentTitle = null;
    for (; i < lines.length; i++) {
      var line = lines[i];
      if (/^##\s+/.test(line)) {
        currentTitle = line.replace(/^##\s+/, "").trim();
        sectionsRaw[currentTitle] = [];
        continue;
      }
      if (/^#\s+/.test(line)) { currentTitle = null; continue; }
      if (currentTitle) sectionsRaw[currentTitle].push(line);
    }

    var sections = {};
    Object.keys(sectionsRaw).forEach(function (title) {
      sections[title] = processSectionLines(sectionsRaw[title]);
    });

    return { meta: meta, sections: sections };
  }

  /* ── Helpers de leitura segura (nunca inventam dado) ─────────────── */

  var NA_TEXT = "Não disponível nesta versão do DASHBOARD_STATE";

  function sec(state, title) { return state.sections[title] || null; }

  function kv(state, title, field) {
    var s = sec(state, title);
    if (!s || !s.table || !s.table.kv) return null;
    var v = s.table.kv[field];
    return (v === undefined || v === null || v === "") ? null : v;
  }

  function rows(state, title) {
    var s = sec(state, title);
    if (!s || !s.table) return [];
    return s.table.rows || [];
  }

  function bullets(state, title) {
    var s = sec(state, title);
    return (s && s.bullets) || [];
  }

  function paragraphs(state, title) {
    var s = sec(state, title);
    return (s && s.paragraphs) || [];
  }

  function firstNumber(str) {
    if (!str) return null;
    var m = String(str).match(/\d+/);
    return m ? parseInt(m[0], 10) : null;
  }

  function firstPercent(str) {
    if (!str) return null;
    var m = String(str).match(/(\d{1,3})\s*%/);
    return m ? parseInt(m[1], 10) : null;
  }

  function escapeHtml(str) {
    return String(str).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }

  function naSpan(text) {
    return '<span class="b-neutro badge">' + escapeHtml(text || NA_TEXT) + "</span>";
  }

  /* ── Classificação visual de status (vocabulário oficial + textos livres) ── */

  function statusClass(text) {
    var t = (text || "").toLowerCase();
    if (/cancelad/.test(t)) return "cancelado";
    if (/bloquead/.test(t)) return "bloqueado";
    if (/em andamento/.test(t)) return "andamento";
    if (/concluíd|concluid|implementad/.test(t)) return "concluido";
    if (/planejad/.test(t)) return "planejado";
    if (/pendente|não aplic|nao aplic|aguardando|não iniciad|nao iniciad/.test(t)) return "pendente";
    return "neutro";
  }

  var SHORT_LABEL = {
    planejado: "Planejado",
    andamento: "Em andamento",
    bloqueado: "Bloqueado",
    concluido: "Concluído",
    cancelado: "Cancelado",
    pendente: "Pendente",
    neutro: null
  };

  function statusBadge(text, opts) {
    if (!text) return naSpan();
    opts = opts || {};
    var cls = statusClass(text);
    var label = opts.short ? (SHORT_LABEL[cls] || text) : text;
    return '<span class="badge b-' + cls + '"><span class="dot"></span>' + escapeHtml(label) + "</span>";
  }

  /* ── Construtores de UI genéricos ─────────────────────────────────── */

  function el(html) {
    var t = document.createElement("template");
    t.innerHTML = html.trim();
    return t.content.firstChild;
  }

  function statTile(value, label, color, opts) {
    opts = opts || {};
    var valHtml = value === null || value === undefined || value === ""
      ? '<span class="na">—</span>'
      : escapeHtml(String(value));
    return (
      '<div class="stat ' + (color || "") + '">' +
      '<div class="val' + (opts.small ? " na" : "") + '">' + valHtml + "</div>" +
      '<div class="lbl">' + escapeHtml(label) + "</div>" +
      (opts.caption ? '<div class="lbl" style="margin-top:6px;color:var(--text-secondary)">' + escapeHtml(opts.caption) + "</div>" : "") +
      "</div>"
    );
  }

  function card(title, bodyHtml, opts) {
    opts = opts || {};
    var leftCls = opts.left ? " card-left " + opts.left : "";
    return (
      '<div class="card' + leftCls + '">' +
      (title ? '<div class="card-title">' + escapeHtml(title) + "</div>" : "") +
      '<div class="card-body">' + bodyHtml + "</div>" +
      "</div>"
    );
  }

  function noticeBox(kind, html) {
    return '<div class="notice ' + kind + '"><span>' + html + "</span></div>";
  }

  function sectionTitle(text) {
    return '<div class="section-title">' + escapeHtml(text) + "</div>";
  }

  /* ── Render por aba ───────────────────────────────────────────────── */

  function renderTopbar(state) {
    document.getElementById("proj-name").textContent = kv(state, "Projeto", "Nome") || "SOMA BlueprintOS";
    document.getElementById("proj-version").textContent = kv(state, "Projeto", "Versão") || "";
    document.getElementById("proj-status-wrap").innerHTML = statusBadge(kv(state, "Projeto", "Status"));

    var mvpPctRaw = kv(state, "Roadmap", "Percentual do MVP 1.0");
    var pct = firstPercent(mvpPctRaw);
    var fill = document.getElementById("mvp-fill");
    var pctEl = document.getElementById("mvp-pct");
    if (pct !== null) {
      fill.style.width = pct + "%";
      fill.classList.remove("pending");
      pctEl.textContent = pct + "%";
      pctEl.classList.remove("pending");
    } else {
      fill.classList.add("pending");
      pctEl.textContent = "N/D";
      pctEl.classList.add("pending");
      pctEl.title = mvpPctRaw || "Percentual do MVP não calculável nesta versão do DASHBOARD_STATE";
    }

    document.getElementById("footer-loaded-at").textContent = state.meta.lastUpdate || "não informado no documento-fonte";
  }

  function renderExecutive(state) {
    var resumo = paragraphs(state, "Resumo Executivo").join(" ");
    var foundationStatus = kv(state, "Foundation", "Status");
    var foundationPct = kv(state, "Foundation", "Percentual");
    var ondaAtual = kv(state, "Roadmap", "Onda Atual");
    var proximaOnda = kv(state, "Roadmap", "Próxima Onda");
    var proximoMarcoGate = kv(state, "Gates", "Próximo");
    var statusGeral = kv(state, "Projeto", "Status");
    var proximosObjetivos = bullets(state, "Próximos Marcos");
    var ultimasEntregas = bullets(state, "Últimas Entregas");

    var cronoRows = rows(state, "Cronograma");
    var focusRow = null;
    if (proximaOnda) {
      focusRow = cronoRows.find(function (r) {
        return proximaOnda.indexOf(r["Onda"]) !== -1 || (r["Onda"] || "").indexOf(extractWaveNumber(proximaOnda)) !== -1;
      });
    }
    if (!focusRow) focusRow = cronoRows[0] || null;

    var html = "";

    html += '<div class="block">' + sectionTitle("Resumo Executivo") +
      '<div class="exec-summary">' + escapeHtml(resumo || NA_TEXT) + "</div></div>";

    html += '<div class="block grid grid-4">';
    html += card("Foundation", (foundationStatus ? escapeHtml(foundationStatus) : NA_TEXT) +
      (foundationPct ? '<br><span style="font-family:var(--mono);font-weight:600;color:var(--text-primary)">' + escapeHtml(foundationPct) + "</span>" : ""),
      { left: foundationStatus && /conclu/i.test(foundationStatus) ? "ok" : "pend" });
    html += card("Onda Atual", ondaAtual ? escapeHtml(ondaAtual) : NA_TEXT,
      { left: ondaAtual && /nenhuma/i.test(ondaAtual) ? "pend" : "info" });
    html += card("Próximo Marco (Gate)", proximoMarcoGate ? escapeHtml(proximoMarcoGate) : NA_TEXT, { left: "warn" });
    html += card("Status Geral", statusGeral ? escapeHtml(statusGeral) : NA_TEXT, { left: "info" });
    html += "</div>";

    html += '<div class="block grid grid-4">';
    html += card("Data Planejada", focusRow ? escapeHtml(focusRow["Data Planejada"] || "—") : NA_TEXT);
    html += card("Data Real", focusRow ? escapeHtml(focusRow["Data Real"] || "—") : NA_TEXT);
    html += card("Data Replanejada", focusRow ? escapeHtml(focusRow["Data Replanejada"] || "—") : NA_TEXT);
    html += card("Risco Geral", NA_TEXT + " — campo não presente em DASHBOARD_STATE.md nesta versão.", { left: "pend" });
    html += "</div>";

    if (focusRow) {
      html += '<div class="block card" style="font-size:var(--t-body-sm);color:var(--text-secondary)">' +
        "Datas relativas à onda em foco (<strong>" + escapeHtml(focusRow["Onda"]) + "</strong>) — status " +
        statusBadge(focusRow["Status"]) + " · gate: " + escapeHtml(focusRow["Gate"] || "—") + "</div>";
    }

    html += '<div class="block grid grid-2">';
    html += '<div>' + sectionTitle("Últimas Entregas") + renderBulletList(ultimasEntregas) + "</div>";
    html += '<div>' + sectionTitle("Próximos Objetivos") + renderBulletList(proximosObjetivos) + "</div>";
    html += "</div>";

    document.getElementById("panel-executive").innerHTML = html;
  }

  function extractWaveNumber(text) {
    var m = (text || "").match(/Onda\s*(\d+)/i);
    return m ? "Onda " + m[1] : "";
  }

  function renderBulletList(items) {
    if (!items || !items.length) return '<div class="card-body">' + NA_TEXT + "</div>";
    return '<ul class="bullet-list">' + items.map(function (i) { return "<li>" + escapeHtml(i) + "</li>"; }).join("") + "</ul>";
  }

  function renderRoadmap(state) {
    var html = "";
    var foundationStatus = kv(state, "Foundation", "Status");
    var foundationPct = firstPercent(kv(state, "Foundation", "Percentual"));

    html += sectionTitle("Foundation");
    html += waveCard({
      name: "Foundation",
      status: foundationStatus,
      pct: foundationPct,
      planned: "—", real: "—", replanned: "—", gate: "—"
    });

    html += sectionTitle("Ondas do MVP 1.0");
    var cronoRows = rows(state, "Cronograma");
    var proximaOnda = kv(state, "Roadmap", "Próxima Onda") || "";
    var pctOnda = kv(state, "Roadmap", "Percentual da Onda");

    html += '<div class="grid" style="grid-template-columns:1fr">';
    if (!cronoRows.length) {
      html += noticeBox("pend", NA_TEXT);
    } else {
      cronoRows.forEach(function (r) {
        var isFocus = proximaOnda.indexOf(r["Onda"]) !== -1;
        html += waveCard({
          name: r["Onda"],
          status: r["Status"],
          pct: isFocus ? firstPercent(pctOnda) : null,
          pctNote: isFocus ? pctOnda : null,
          planned: r["Data Planejada"],
          real: r["Data Real"],
          replanned: r["Data Replanejada"],
          gate: r["Gate"]
        });
      });
    }
    html += "</div>";

    document.getElementById("panel-roadmap").innerHTML = html;
  }

  function waveCard(w) {
    var pctKnown = w.pct !== null && w.pct !== undefined;
    var fillHtml = pctKnown
      ? '<div class="fill" style="width:' + w.pct + '%"></div>'
      : '<div class="fill pending" style="width:0%"></div>';
    return (
      '<div class="wave-card">' +
      '<div class="wave-head"><span class="wave-name">' + escapeHtml(w.name || "—") + "</span>" +
      statusBadge(w.status) +
      '<span class="wave-pct">' + (pctKnown ? w.pct + "%" : (w.pctNote ? escapeHtml(w.pctNote) : "Sem dado")) + "</span>" +
      "</div>" +
      '<div class="wave-progress">' + fillHtml + "</div>" +
      '<div class="wave-dates">' +
      waveDate("Data Planejada", w.planned) +
      waveDate("Data Real", w.real) +
      waveDate("Data Replanejada", w.replanned) +
      "</div>" +
      '<div class="wave-gate">Gate: ' + escapeHtml(w.gate || "—") + "</div>" +
      "</div>"
    );
  }

  function waveDate(label, value) {
    return '<div><div class="dl">' + escapeHtml(label) + '</div><div class="dv">' + escapeHtml(value || "—") + "</div></div>";
  }

  function renderBacklog(state) {
    var b = sec(state, "Backlog");
    var kvMap = (b && b.table && b.table.kv) || {};
    var html = "";

    html += sectionTitle("Visão consolidada do Backlog");
    html += '<div class="block grid grid-4">';
    var order = ["Total", "Concluídas", "Em andamento", "Planejadas", "Parciais", "Não comprovadas", "Bloqueadas"];
    var colorFor = { "Concluídas": "green", "Em andamento": "orange", "Bloqueadas": "red", "Planejadas": "blue", "Parciais": "orange", "Não comprovadas": "red", "Total": "grey" };
    Object.keys(kvMap).forEach(function (fieldRaw) {
      var field = fieldRaw.replace(/\s*\(.*\)$/, "");
      var num = firstNumber(kvMap[fieldRaw]);
      var colorKey = order.find(function (o) { return fieldRaw.indexOf(o) === 0; }) || field;
      html += statTile(num, fieldRaw, colorFor[colorKey] || "grey", { caption: kvMap[fieldRaw] });
    });
    html += "</div>";

    // Work Orders citados nominalmente no texto (extração literal, sem inferência)
    var cited = [];
    ["Concluídas", "Parciais", "Não comprovadas", "Bloqueadas"].forEach(function (label) {
      var key = Object.keys(kvMap).find(function (k) { return k.indexOf(label) === 0; });
      if (!key) return;
      var m = kvMap[key].match(/\(([^)]+)\)/g);
      if (!m) return;
      m.forEach(function (group) {
        group.replace(/[()]/g, "").split(",").forEach(function (code) {
          code = code.trim();
          if (code && /^[A-Z0-9.]+$/.test(code)) cited.push({ code: code, status: label });
        });
      });
    });

    html += sectionTitle("Filtros");
    var filterLabels = ["MVP 1.0", "MVP 1.1", "Planejadas", "Em andamento", "Concluídas", "Bloqueadas"];
    html += '<div class="filters" id="backlog-filters">';
    filterLabels.forEach(function (f) {
      html += '<button type="button" class="fpill" data-filter="' + escapeHtml(f) + '" aria-pressed="false">' + escapeHtml(f) + "</button>";
    });
    html += "</div>";
    html += '<div id="backlog-filter-note"></div>';

    html += sectionTitle("Work Orders citados nesta versão do DASHBOARD_STATE");
    if (cited.length) {
      html += '<div class="table-wrap"><table class="compact" id="backlog-wo-table"><thead><tr><th>Work Order</th><th>Situação</th></tr></thead><tbody>';
      cited.forEach(function (c) {
        html += '<tr data-status="' + escapeHtml(c.status) + '"><td class="mono">' + escapeHtml(c.code) + "</td><td>" + statusBadge(c.status === "Concluídas" ? "Concluído" : c.status) + "</td></tr>";
      });
      html += "</tbody></table></div>";
    } else {
      html += noticeBox("pend", NA_TEXT);
    }
    html += noticeBox("info", "Work Orders \"Planejadas\" (sem código citado nominalmente) e a reclassificação por MVP 1.0/1.1 por item não estão detalhadas nesta versão do <code>DASHBOARD_STATE.md</code> — ver <code>.ai/BACKLOG.md</code> para a lista completa do catálogo.");

    document.getElementById("panel-backlog").innerHTML = html;

    document.getElementById("backlog-filters").addEventListener("click", function (e) {
      var btn = e.target.closest(".fpill");
      if (!btn) return;
      var pressed = btn.getAttribute("aria-pressed") === "true";
      document.querySelectorAll("#backlog-filters .fpill").forEach(function (b) { b.setAttribute("aria-pressed", "false"); });
      var note = document.getElementById("backlog-filter-note");
      var table = document.getElementById("backlog-wo-table");
      if (pressed) {
        note.innerHTML = "";
        if (table) table.querySelectorAll("tbody tr").forEach(function (tr) { tr.style.display = ""; });
        return;
      }
      btn.setAttribute("aria-pressed", "true");
      var f = btn.getAttribute("data-filter");
      if (f === "MVP 1.0" || f === "MVP 1.1") {
        note.innerHTML = noticeBox("pend", "A reclassificação de Work Orders por " + escapeHtml(f) + " não está disponível por item nesta versão do DASHBOARD_STATE.");
        if (table) table.querySelectorAll("tbody tr").forEach(function (tr) { tr.style.display = ""; });
        return;
      }
      note.innerHTML = "";
      if (table) {
        var visibleCount = 0;
        table.querySelectorAll("tbody tr").forEach(function (tr) {
          var st = tr.getAttribute("data-status");
          var match = (f === "Concluídas" && st === "Concluídas") || (f === "Em andamento" && st === "Em andamento") || (f === "Bloqueadas" && st === "Bloqueadas") || (f === "Planejadas" && st === "Planejadas");
          tr.style.display = match ? "" : "none";
          if (match) visibleCount++;
        });
        if (visibleCount === 0) {
          note.innerHTML = noticeBox("pend", "Nenhum Work Order citado nominalmente nesta situação nesta versão do DASHBOARD_STATE.");
        }
      }
    });
  }

  function renderFrontend(state) {
    var previstas = kv(state, "Frontend", "Telas previstas");
    var concluidas = kv(state, "Frontend", "Telas concluídas");
    var andamento = kv(state, "Frontend", "Telas em andamento");
    var html = "";
    html += sectionTitle("Evolução das telas (agregado)");
    html += '<div class="block grid grid-3">';
    html += statTile(firstNumber(previstas), "Telas previstas", "blue", { caption: previstas });
    html += statTile(firstNumber(concluidas), "Telas concluídas", "green", { caption: concluidas });
    html += statTile(firstNumber(andamento), "Telas em andamento", "orange", { caption: andamento });
    html += "</div>";
    html += noticeBox("pend", "Indicadores por tela (Mock, +Compras Funcional, +Compras UX, Blueprint Banco, API, Integração, Implementação, Testes, Homologação) não estão detalhados por tela nesta versão do <code>DASHBOARD_STATE.md</code> — dependem de granularidade adicional a partir de <code>docs/product/ComprasFuncional.md</code> e <code>docs/product/ComprasUX.md</code>.");
    document.getElementById("panel-frontend").innerHTML = html;
  }

  function renderBanco(state) {
    var blueprint = kv(state, "Banco", "Blueprint");
    var scripts = kv(state, "Banco", "Scripts");
    var entidades = kv(state, "Banco", "Entidades");
    var html = "";
    html += '<div class="grid grid-3">';
    html += card("Blueprint", blueprint ? escapeHtml(blueprint) : NA_TEXT, { left: statusClass(blueprint) === "concluido" ? "ok" : "pend" });
    html += card("Scripts", scripts ? escapeHtml(scripts) : NA_TEXT, { left: "pend" });
    html += card("Entidades", entidades ? escapeHtml(entidades) : NA_TEXT, { left: "pend" });
    html += "</div>";
    html += '<div class="block"></div>';
    html += noticeBox("info", "Integração ERP do Banco é reportada de forma consolidada na aba Integrações.");
    document.getElementById("panel-banco").innerHTML = html;
  }

  function renderIntegracoes(state) {
    var r = rows(state, "Integrações");
    var html = "";
    html += sectionTitle("Integrações");
    if (!r.length) {
      html += noticeBox("pend", NA_TEXT);
    } else {
      html += '<div class="grid grid-2">';
      r.forEach(function (row) {
        html += card(row["Integração"],
          statusBadge(row["Status"], { short: true }) +
          '<div style="margin-top:8px">' + (row["Status"] ? escapeHtml(row["Status"]) : NA_TEXT) + "</div>",
          { left: statusClass(row["Status"]) === "concluido" ? "ok" : (statusClass(row["Status"]) === "pendente" ? "pend" : "info") });
      });
      html += "</div>";
    }
    document.getElementById("panel-integracoes").innerHTML = html;
  }

  function renderIA(state) {
    var agentes = kv(state, "IA", "Agentes");
    var prompts = kv(state, "IA", "Prompts");
    var ferramentas = kv(state, "IA", "Ferramentas");
    var html = "";
    html += '<div class="grid grid-4">';
    html += card("Agentes", agentes ? escapeHtml(agentes) : NA_TEXT);
    html += card("Prompts", prompts ? escapeHtml(prompts) : NA_TEXT);
    html += card("Ferramentas", ferramentas ? escapeHtml(ferramentas) : NA_TEXT);
    html += card("Estado Geral", NA_TEXT + " — campo não presente nesta versão do DASHBOARD_STATE.", { left: "pend" });
    html += "</div>";
    document.getElementById("panel-ia").innerHTML = html;
  }

  function renderQualidade(state) {
    var build = kv(state, "Qualidade", "Build");
    var testes = kv(state, "Qualidade", "Testes");
    var warnings = kv(state, "Qualidade", "Warnings");
    var health = kv(state, "Qualidade", "Health");
    var html = "";
    html += '<div class="grid grid-4">';
    html += statTile(null, "Build", "green", { caption: build, small: true });
    html += statTile(firstNumber(testes), "Testes (unitários)", "blue", { caption: testes });
    html += statTile(firstNumber(warnings), "Warnings", warnings === "0" || firstNumber(warnings) === 0 ? "green" : "orange");
    html += card("Health / Links quebrados", health ? escapeHtml(health) : NA_TEXT, { left: "pend" });
    html += "</div>";
    document.getElementById("panel-qualidade").innerHTML = html;
  }

  function renderDocumentacao(state) {
    var saude = kv(state, "Documentação", "Saúde");
    var ultima = kv(state, "Documentação", "Última atualização");
    var links = kv(state, "Documentação", "Links inválidos");
    var html = "";
    html += '<div class="grid grid-2">';
    html += card("Saúde documental", saude ? escapeHtml(saude) : NA_TEXT, { left: "ok" });
    html += card("Última atualização", ultima ? escapeHtml(ultima) : NA_TEXT);
    html += card("Links inválidos", links ? escapeHtml(links) : NA_TEXT, { left: "pend" });
    html += card("Quantidade de documentos / Estado da publicação", NA_TEXT + " como campos próprios — ver texto de \"Saúde documental\" acima.", { left: "pend" });
    html += "</div>";
    document.getElementById("panel-documentacao").innerHTML = html;
  }

  function renderMetricas(state) {
    var telas = firstNumber(kv(state, "Frontend", "Telas previstas"));
    var woTotalKey = Object.keys((sec(state, "Backlog") || {}).table && sec(state, "Backlog").table.kv || {}).find(function (k) { return k.indexOf("Total") === 0; });
    var woTotal = woTotalKey ? firstNumber(sec(state, "Backlog").table.kv[woTotalKey]) : null;
    var integracoesCount = rows(state, "Integrações").length;
    var testes = kv(state, "Qualidade", "Testes");
    var testesNum = firstNumber(testes);

    var html = "";
    html += '<div class="grid grid-4">';
    html += statTile(telas, "Telas (previstas)", "blue");
    html += statTile(null, "APIs", "grey", { small: true, caption: NA_TEXT });
    html += statTile(null, "Entidades", "grey", { small: true, caption: NA_TEXT });
    html += statTile(integracoesCount || null, "Integrações mapeadas", "blue");
    html += statTile(null, "Agentes", "grey", { small: true, caption: kv(state, "IA", "Agentes") });
    html += statTile(woTotal, "Work Orders (catálogo)", "blue");
    html += statTile(null, "Documentos", "grey", { small: true, caption: kv(state, "Documentação", "Saúde") });
    html += statTile(testesNum, "Testes (unitários)", "green", { caption: testes });
    html += "</div>";
    document.getElementById("panel-metricas").innerHTML = html;
  }

  function renderAll(state) {
    renderTopbar(state);
    renderExecutive(state);
    renderRoadmap(state);
    renderBacklog(state);
    renderFrontend(state);
    renderBanco(state);
    renderIntegracoes(state);
    renderIA(state);
    renderQualidade(state);
    renderDocumentacao(state);
    renderMetricas(state);
  }

  /* ── Tabs (troca de aba, sem processamento adicional) ─────────────── */

  function initTabs() {
    var tabs = document.querySelectorAll(".tab");
    tabs.forEach(function (btn) {
      btn.addEventListener("click", function () {
        var target = btn.getAttribute("data-tab");
        tabs.forEach(function (b) { b.setAttribute("aria-selected", b === btn ? "true" : "false"); });
        document.querySelectorAll(".tabpanel").forEach(function (p) {
          p.hidden = p.id !== "panel-" + target;
        });
      });
    });
  }

  /* ── Carregamento (fetch com fallback de seleção manual de arquivo) ── */

  function boot(markdownText) {
    var app = document.getElementById("app");
    var state = parseDashboardState(markdownText);
    renderAll(state);
    initTabs();
    app.setAttribute("data-state", "ready");
    document.getElementById("load-state").hidden = true;
    document.getElementById("error-state").hidden = true;
  }

  function showError(detail) {
    document.getElementById("app").setAttribute("data-state", "error");
    document.getElementById("load-state").hidden = true;
    document.getElementById("error-state").hidden = false;
    if (detail) document.getElementById("error-detail").textContent = detail;
  }

  function wireFilePicker() {
    var input = document.getElementById("file-input");
    var btn = document.getElementById("file-pick-btn");
    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      if (!file) return;
      var reader = new FileReader();
      reader.onload = function () { boot(String(reader.result)); };
      reader.onerror = function () { showError("Não foi possível ler o arquivo selecionado."); };
      reader.readAsText(file, "utf-8");
    });
  }

  function init() {
    wireFilePicker();
    fetch(STATE_URL, { cache: "no-store" })
      .then(function (resp) {
        if (!resp.ok) throw new Error("HTTP " + resp.status);
        return resp.text();
      })
      .then(boot)
      .catch(function (err) {
        showError("Falha ao buscar " + STATE_URL + " (" + err.message + "). Provável bloqueio de CORS em file://.");
      });
  }

  document.addEventListener("DOMContentLoaded", init);
})();
