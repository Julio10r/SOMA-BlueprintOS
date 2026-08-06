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

  function stripMd(s) { return String(s == null ? "" : s).replace(/\*\*/g, "").replace(/`/g, "").trim(); }
  function splitRow(line) {
    var s = line.trim();
    if (s.startsWith("|")) s = s.slice(1);
    if (s.endsWith("|")) s = s.slice(0, -1);
    return s.split("|").map(function (c) { return stripMd(c); });
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
        bullets.push(stripMd(line.replace(/^\s*-\s+/, "")));
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

  /* ── Blocos rotulados em negrito ("**Rótulo:** texto" + bullets abaixo) ──
     Usado pela seção "Resumo Executivo" no novo schema (2.0.0), onde os
     campos (Situação Atual, Últimas Entregas, Próximos Objetivos, Próximo
     Marco, Principais Riscos) são sub-blocos dentro de um único H2, não
     H2 próprios. Nenhum cálculo — apenas segmentação de texto já existente. */
  function parseLabeledBlocks(lines) {
    var map = {}; var currentKey = null;
    lines.forEach(function (line) {
      var mBold = line.match(/^\*\*([^*]+):\*\*\s*(.*)$/);
      if (mBold) {
        currentKey = mBold[1].trim();
        map[currentKey] = { text: mBold[2].trim(), items: [] };
        return;
      }
      if (/^\s*-\s+/.test(line)) {
        if (currentKey) map[currentKey].items.push(stripMd(line.replace(/^\s*-\s+/, "")));
        return;
      }
      if (/^>\s?/.test(line) || line.trim() === "") return;
      if (currentKey && map[currentKey].items.length === 0) {
        map[currentKey].text = (map[currentKey].text ? map[currentKey].text + " " : "") + line.trim();
      }
    });
    return map;
  }

  /* ── Subseções H3 dentro de um H2 (usado por "## Entregáveis", que
     contém "### Onda 1 — ...", "### Onda 2 — ...", etc.). Cada subseção
     produz apenas as linhas de tabela já existentes, sem cálculo. ── */
  function parseH3Subsections(lines) {
    var map = {}; var currentKey = null; var buffer = [];
    function flush() {
      if (currentKey) {
        var block = buffer.filter(isTableLine);
        map[currentKey] = block.length ? parseTableBlock(block).rows : [];
      }
    }
    lines.forEach(function (line) {
      var m = line.match(/^###\s+(.*)$/);
      if (m) {
        flush();
        var title = m[1].trim();
        var num = title.match(/Onda\s*(\d+)/i);
        currentKey = num ? "Onda " + num[1] : title;
        buffer = [];
        return;
      }
      buffer.push(line);
    });
    flush();
    return map;
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
    if (sections["Entregáveis"]) sections["Entregáveis"].byOnda = parseH3Subsections(sectionsRaw["Entregáveis"]);
    if (sections["Resumo Executivo"]) sections["Resumo Executivo"].labeled = parseLabeledBlocks(sectionsRaw["Resumo Executivo"]);
    if (sections["Roadmap dos Produtos"]) sections["Roadmap dos Produtos"].labeled = parseLabeledBlocks(sectionsRaw["Roadmap dos Produtos"]);

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

  /* Percentual com decimal (vírgula, formato brasileiro), ex.: "28,6%" -> 28.6.
     Usado para o Percentual Global do MVP, que exige precisão além do
     inteiro para exibir o valor exato em tooltip/detalhe acessível. */
  function decimalPercent(str) {
    if (!str) return null;
    var m = String(str).match(/(\d{1,3}(?:,\d+)?)\s*%/);
    return m ? parseFloat(m[1].replace(",", ".")) : null;
  }

  /* Converte o valor em pontos já registrado no DASHBOARD_STATE (ex.:
     "7,0 pontos") para percentual arredondado apenas para apresentação
     visual (ex.: "7%") — os pontos de Contribuição ao MVP já são, por
     definição (Peso × Progresso Técnico, ambos em % de um total de
     100%), a mesma unidade de percentual do MVP; esta função apenas
     extrai o número já pronto no texto-fonte e arredonda para exibição,
     sem recalcular nenhum indicador. */
  function pointsToDisplayPercent(str) {
    if (!str) return null;
    var m = String(str).match(/(\d+(?:,\d+)?)\s*pontos?/i);
    if (!m) return null;
    return Math.round(parseFloat(m[1].replace(",", "."))) + "%";
  }

  /* ── Leitura da tabela "Roadmap" pivotada (Campo × Onda 1..5, schema 2.0.0) ── */
  function ondaLabels(state) {
    var s = sec(state, "Roadmap");
    if (!s || !s.table || !s.table.header) return [];
    return s.table.header.slice(1);
  }
  function roadmapField(state, campo, onda) {
    var r = rows(state, "Roadmap").find(function (row) { return row["Campo"] === campo; });
    if (!r) return null;
    var v = r[onda];
    return (v === undefined || v === null || v === "") ? null : v;
  }
  function entregaveisFor(state, onda) {
    var s = sec(state, "Entregáveis");
    var list = (s && s.byOnda && s.byOnda[onda]) || [];
    return list.map(function (row) {
      return {
        nome: row["Entregável"] || row[Object.keys(row)[0]] || "—",
        status: row["Status"] || null,
        pct: firstPercent(row["Percentual"]),
        obs: (row["Observações"] && row["Observações"] !== "—") ? row["Observações"] : null
      };
    });
  }
  function entregaveisCounts(list) {
    var c = { total: list.length, concluido: 0, andamento: 0, planejado: 0 };
    list.forEach(function (e) {
      var cls = statusClass(e.status);
      if (cls === "concluido") c.concluido++;
      else if (cls === "andamento") c.andamento++;
      else if (cls === "planejado") c.planejado++;
    });
    return c;
  }
  /* Retorna { exact: 28.6, rounded: 29 } lidos da linha "Total" da tabela
     "Percentual Global do MVP 1.0" — o valor exato nunca é recalculado
     aqui, apenas extraído do texto já pronto; o arredondamento é somente
     para apresentação visual (Math.round é formatação, não cálculo de
     indicador). Retorna null quando a linha/valor não existe. */
  function mvpGlobalPercent(state) {
    var r = rows(state, "Percentual Global do MVP 1.0").find(function (row) { return /total/i.test(row["Componente"] || ""); });
    if (!r) return null;
    var col = r["Contribuição ao MVP (pontos)"] !== undefined ? r["Contribuição ao MVP (pontos)"] : r["Contribuição"];
    var exact = decimalPercent(col);
    if (exact === null) return null;
    return { exact: exact, rounded: Math.round(exact) };
  }
  function currentOndaEntry(state) {
    var labels = ondaLabels(state);
    for (var idx = 0; idx < labels.length; idx++) {
      var st = roadmapField(state, "Status", labels[idx]);
      if (st && /em desenvolvimento|em andamento/i.test(st)) return { label: labels[idx], nome: roadmapField(state, "Nome", labels[idx]), status: st };
    }
    return null;
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
    if (/em andamento|em desenvolvimento/.test(t)) return "andamento";
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

  var PROJECT_NAME_LABEL = "SOMA BlueprintOS / +Compras";

  function renderTopbar(state) {
    document.getElementById("proj-name").textContent = PROJECT_NAME_LABEL;
    document.getElementById("proj-version").textContent = stripMd(kv(state, "Cabeçalho", "Project Version") || "");
    var statusText = kv(state, "Cabeçalho", "Status");
    document.getElementById("proj-status-wrap").innerHTML = statusText ? '<span class="badge b-neutro" title="' + escapeHtml(statusText) + '">' + escapeHtml(statusText.length > 46 ? statusText.slice(0, 46) + "…" : statusText) + "</span>" : naSpan();

    var pct = mvpGlobalPercent(state);
    var fill = document.getElementById("mvp-fill");
    var pctEl = document.getElementById("mvp-pct");
    if (pct !== null) {
      fill.style.width = pct.exact + "%";
      fill.classList.remove("pending");
      pctEl.textContent = pct.rounded + "%";
      pctEl.title = "Percentual Global do MVP 1.0 (valor exato): " + String(pct.exact).replace(".", ",") + "%";
      pctEl.classList.remove("pending");
    } else {
      fill.classList.add("pending");
      pctEl.textContent = "N/D";
      pctEl.classList.add("pending");
      pctEl.title = "Percentual Global do MVP não encontrado na tabela \"Percentual Global do MVP 1.0\" (linha Total).";
    }

    document.getElementById("footer-loaded-at").textContent = kv(state, "Cabeçalho", "Last Update") || kv(state, "Cabeçalho", "Generated At") || state.meta.lastUpdate || "não informado no documento-fonte";
  }

  function renderExecutive(state) {
    var labeled = (sec(state, "Resumo Executivo") || {}).labeled || {};
    var situacaoAtual = labeled["Situação Atual"] || { text: "", items: [] };
    var proximoMarco = labeled["Próximo Marco"] || { text: "", items: [] };
    var principaisRiscos = labeled["Principais Riscos"] || { text: "", items: [] };
    var ultimasEntregas = labeled["Últimas Entregas"] || { text: "", items: [] };
    var proximosObjetivos = labeled["Próximos Objetivos"] || { text: "", items: [] };
    var mvpPct = mvpGlobalPercent(state);
    var atual = currentOndaEntry(state);
    var ondaAtualLabel = atual ? atual.label + (atual.nome ? " — " + atual.nome : "") + " (" + atual.status + ")" : null;

    var html = "";

    html += '<div class="block">' + sectionTitle("Resumo Executivo") +
      '<div class="exec-summary">' + escapeHtml(situacaoAtual.text || NA_TEXT) + "</div></div>";

    html += '<div class="block grid grid-4">';
    html += card("Situação Atual", situacaoAtual.text ? escapeHtml(situacaoAtual.text) : NA_TEXT, { left: "info" });
    html += card("Percentual do MVP", mvpPct !== null ? '<span style="font-family:var(--mono);font-weight:700;font-size:20px;color:var(--text-primary)" title="Valor exato: ' + String(mvpPct.exact).replace(".", ",") + '%">' + mvpPct.rounded + "%</span>" : NA_TEXT, { left: mvpPct !== null ? "ok" : "pend" });
    html += card("Onda Atual", ondaAtualLabel ? escapeHtml(ondaAtualLabel) : "Nenhuma Onda com status \"Em desenvolvimento\" no momento",
      { left: ondaAtualLabel ? "info" : "pend" });
    html += card("Próximo Marco", proximoMarco.text ? escapeHtml(proximoMarco.text) : NA_TEXT, { left: "warn" });
    html += "</div>";

    html += '<div class="block">' + card("Principais Riscos", principaisRiscos.items.length ? renderBulletList(principaisRiscos.items) : NA_TEXT, { left: "pend" }) + "</div>";

    html += '<div class="block grid grid-2">';
    html += '<div>' + sectionTitle("Últimas Entregas") + renderBulletList(ultimasEntregas.items) + "</div>";
    html += '<div>' + sectionTitle("Próximos Objetivos") + renderBulletList(proximosObjetivos.items) + "</div>";
    html += "</div>";

    html += renderRoadmapDosProdutos(state);

    document.getElementById("panel-executive").innerHTML = html;
  }

  /* Divide um item "Rótulo: valor" já pronto no texto-fonte em suas duas
     partes — segmentação de texto, não cálculo (mesmo princípio já usado
     em parseLabeledBlocks para "**Rótulo:** texto"). */
  function splitLabelValue(item) {
    var m = String(item || "").match(/^([^:]+):\s*(.*)$/);
    return m ? { label: m[1].trim(), value: m[2].trim() } : { label: null, value: item };
  }
  function labeledItemsMap(block) {
    var map = {};
    ((block && block.items) || []).forEach(function (item) {
      var kv2 = splitLabelValue(item);
      if (kv2.label) map[kv2.label] = kv2.value;
    });
    return map;
  }

  /* Seção "Roadmap dos Produtos" — final da aba Executive. Objetivos,
     percentual e marco do MVP 1.0 vêm da seção "Roadmap dos Produtos" do
     DASHBOARD_STATE; objetivo/status/prazo por Onda reaproveitam os campos
     já lidos pela tabela "Roadmap" (nenhum campo novo inventado). MVP 1.1
     (objetivo geral e escopo adiado) vem também da seção "Roadmap dos
     Produtos" — nunca de lista fixa no HTML. */
  function renderRoadmapDosProdutos(state) {
    var labeled = (sec(state, "Roadmap dos Produtos") || {}).labeled || {};
    var mvp10Block = labeled["MVP 1.0"];
    var mvp11Block = labeled["MVP 1.1"];
    if (!mvp10Block && !mvp11Block) return "";

    var mvp10 = labeledItemsMap(mvp10Block);
    var mvp11 = labeledItemsMap(mvp11Block);

    var ondaListHtml = "";
    var labels = ondaLabels(state);
    if (labels.length) {
      ondaListHtml = '<div class="product-wave-list">' + labels.map(function (onda) {
        var nome = roadmapField(state, "Nome", onda);
        var status = roadmapField(state, "Status", onda);
        var objetivo = roadmapField(state, "Objetivo", onda);
        var prazo = roadmapField(state, "Fim Planejado", onda);
        return (
          '<div class="product-wave-item">' +
          '<div class="product-wave-header">' +
          '<span class="product-wave-name">' + escapeHtml(onda + (nome ? " — " + nome : "")) + "</span>" +
          statusBadge(status, { short: true }) +
          "</div>" +
          (!isEmptyValue(objetivo) ? '<span class="product-wave-objetivo">' + escapeHtml(objetivo) + "</span>" : "") +
          (!isEmptyValue(prazo) ? '<span class="product-wave-prazo">Prazo planejado: ' + escapeHtml(prazo) + "</span>" : "") +
          "</div>"
        );
      }).join("") + "</div>";
    }

    var mvp10Card = mvp10Block ? (
      '<div class="product-card">' +
      '<div class="product-card-title">MVP 1.0</div>' +
      (mvp10["Objetivo geral"] ? '<div class="product-card-objetivo">' + escapeHtml(mvp10["Objetivo geral"]) + "</div>" : "") +
      '<div class="product-card-stats">' +
      (mvp10["Percentual Global Atual"] ? '<span class="product-stat"><span class="product-stat-label">Percentual Global</span><span class="product-stat-value">' + escapeHtml(mvp10["Percentual Global Atual"]) + "</span></span>" : "") +
      (mvp10["Onda Atual"] ? '<span class="product-stat"><span class="product-stat-label">Onda Atual</span><span class="product-stat-value">' + escapeHtml(mvp10["Onda Atual"]) + "</span></span>" : "") +
      "</div>" +
      ondaListHtml +
      (mvp10["Marco Final"] ? '<div class="product-card-marco">Marco final: ' + escapeHtml(mvp10["Marco Final"]) + "</div>" : "") +
      "</div>"
    ) : "";

    var escopoChips = mvp11["Escopo adiado"]
      ? '<div class="product-chip-list">' + mvp11["Escopo adiado"].split(",").map(function (c) {
          c = c.trim();
          return c ? '<span class="product-chip">' + escapeHtml(c) + "</span>" : "";
        }).join("") + "</div>"
      : "";

    var mvp11Card = mvp11Block ? (
      '<div class="product-card">' +
      '<div class="product-card-title">MVP 1.1</div>' +
      (mvp11["Objetivo geral"] ? '<div class="product-card-objetivo">' + escapeHtml(mvp11["Objetivo geral"]) + "</div>" : "") +
      escopoChips +
      "</div>"
    ) : "";

    return (
      '<div class="block">' +
      sectionTitle("Roadmap dos Produtos") +
      '<div class="product-grid">' + mvp10Card + mvp11Card + "</div>" +
      "</div>"
    );
  }

  function extractWaveNumber(text) {
    var m = (text || "").match(/Onda\s*(\d+)/i);
    return m ? "Onda " + m[1] : "";
  }

  function renderBulletList(items) {
    if (!items || !items.length) return '<div class="card-body">' + NA_TEXT + "</div>";
    return '<ul class="bullet-list">' + items.map(function (i) { return "<li>" + escapeHtml(i) + "</li>"; }).join("") + "</ul>";
  }

  function countChip(label, value, cls) { return '<span class="count-chip ' + (cls || "") + '">' + escapeHtml(String(value)) + " " + escapeHtml(label) + "</span>"; }

  /* ══════════════════════════════════════════════════════════════════
     GRÁFICO DE GANTT — Onda 1 — Fundação Funcional (aba Roadmap).
     Responsabilidade exclusivamente visual: todas as datas vêm de
     DASHBOARD_STATE.md (Foundation §Data Real, Roadmap §Início/Fim
     Planejado/Real/Replanejado). Nenhuma data é fixada aqui. O único
     valor calculado neste arquivo é a data atual do navegador no
     momento da renderização, usada apenas como marcador visual — não
     altera baseline, percentuais ou qualquer outro dado.
     ══════════════════════════════════════════════════════════════════ */

  var GANTT_PAD_DAYS = 2;

  function parseDateBR(s) {
    if (!s) return null;
    var m = String(s).match(/(\d{2})\/(\d{2})\/(\d{4})/);
    if (!m) return null;
    var d = new Date(Date.UTC(parseInt(m[3], 10), parseInt(m[2], 10) - 1, parseInt(m[1], 10)));
    return isNaN(d.getTime()) ? null : d;
  }
  function daysBetween(a, b) { return Math.round((b.getTime() - a.getTime()) / 86400000); }
  function formatTickDate(d) {
    var dd = String(d.getUTCDate()).padStart(2, "0");
    var mm = String(d.getUTCMonth() + 1).padStart(2, "0");
    return dd + "/" + mm;
  }
  function formatFullDate(d) {
    var dd = String(d.getUTCDate()).padStart(2, "0");
    var mm = String(d.getUTCMonth() + 1).padStart(2, "0");
    return dd + "/" + mm + "/" + d.getUTCFullYear();
  }

  function ganttRowsData(state) {
    var out = [];
    var foundationReal = parseDateBR(kv(state, "Foundation", "Data Real"));
    out.push({
      label: "Foundation",
      status: kv(state, "Foundation", "Status"),
      kind: "foundation",
      point: foundationReal,
      pointLabel: "Data Real"
    });
    ondaLabels(state).forEach(function (onda) {
      var nome = roadmapField(state, "Nome", onda);
      out.push({
        label: onda + (nome ? " — " + nome : ""),
        status: roadmapField(state, "Status", onda),
        kind: "onda",
        pctTecnico: firstPercent(roadmapField(state, "Progresso Técnico", onda)),
        planStart: parseDateBR(roadmapField(state, "Início Planejado", onda)),
        planEnd: parseDateBR(roadmapField(state, "Fim Planejado", onda)),
        realStart: parseDateBR(roadmapField(state, "Início Real", onda)),
        realEnd: parseDateBR(roadmapField(state, "Fim Real", onda)),
        replanStart: parseDateBR(roadmapField(state, "Início Replanejado", onda)),
        replanEnd: parseDateBR(roadmapField(state, "Fim Replanejado", onda))
      });
    });
    return out;
  }

  /* Layout do Gantt é 100% relativo (percentual) à largura disponível da
     coluna de timeline — nunca em pixels fixos. Isso garante que o gráfico
     caiba integralmente no card em Desktop/Notebook. Em Tablet, uma
     largura mínima é aplicada via CSS (não aqui) apenas à coluna de
     timeline, habilitando rolagem horizontal somente ali — a coluna de
     nomes das Ondas permanece fixa (sticky) e nunca rola. */
  function renderGantt(state) {
    var data = ganttRowsData(state);
    var allDates = [];
    data.forEach(function (r) {
      [r.point, r.planStart, r.planEnd, r.realStart, r.realEnd, r.replanStart, r.replanEnd].forEach(function (d) {
        if (d) allDates.push(d);
      });
    });
    if (!allDates.length) return "";

    var today = new Date();
    var todayUtc = new Date(Date.UTC(today.getFullYear(), today.getMonth(), today.getDate()));
    var rangeDates = allDates.concat([todayUtc]);
    var minDate = new Date(Math.min.apply(null, rangeDates.map(function (d) { return d.getTime(); })));
    var maxDate = new Date(Math.max.apply(null, rangeDates.map(function (d) { return d.getTime(); })));
    minDate.setUTCDate(minDate.getUTCDate() - GANTT_PAD_DAYS);
    maxDate.setUTCDate(maxDate.getUTCDate() + GANTT_PAD_DAYS);
    var totalDays = Math.max(daysBetween(minDate, maxDate), 1);

    function xForPct(d) { return (daysBetween(minDate, d) / totalDays) * 100; }
    function clampPct(v) { return Math.max(0, Math.min(100, v)); }

    // Eixo de datas: um tick a cada 7 dias corridos, posição em %.
    var ticksHtml = "";
    for (var off = 0; off <= totalDays; off += 7) {
      var td = new Date(minDate.getTime());
      td.setUTCDate(td.getUTCDate() + off);
      ticksHtml += '<div class="gantt-tick" style="left:' + clampPct((off / totalDays) * 100) + '%">' +
        '<span class="gantt-tick-label">' + formatTickDate(td) + "</span></div>";
    }

    var todayPct = clampPct(xForPct(todayUtc));
    var todayTitle = "Data atual: " + formatFullDate(todayUtc);
    var todayLineHtml = '<div class="gantt-today-line" style="left:' + todayPct + '%" title="' + todayTitle + '" aria-label="' + todayTitle + '"></div>';

    var hasReplanned = data.some(function (r) { return r.replanStart && r.replanEnd; });

    var rowsHtml = data.map(function (r) {
      var barsHtml = "";
      if (r.kind === "foundation") {
        if (r.point) {
          var fPct = clampPct(xForPct(r.point));
          barsHtml += '<div class="gantt-marker gantt-marker-foundation" style="left:' + fPct + '%" ' +
            'title="Foundation — Concluído — ' + escapeHtml(r.pointLabel) + ': ' + formatFullDate(r.point) + '" ' +
            'aria-label="Foundation concluída em ' + formatFullDate(r.point) + '"></div>';
        }
      } else {
        if (r.planStart && r.planEnd) {
          var pLeftPct = clampPct(xForPct(r.planStart));
          var pRightPct = clampPct(xForPct(r.planEnd));
          var pWidthPct = Math.max(pRightPct - pLeftPct, 0.6);
          barsHtml += '<div class="gantt-bar gantt-bar-planned" style="left:' + pLeftPct + '%;width:' + pWidthPct + '%" ' +
            'title="' + escapeHtml(r.label) + ' — Planejado: ' + formatFullDate(r.planStart) + ' → ' + formatFullDate(r.planEnd) + '" ' +
            'aria-label="' + escapeHtml(r.label) + ' planejado de ' + formatFullDate(r.planStart) + ' a ' + formatFullDate(r.planEnd) + '"></div>';

          if (r.pctTecnico !== null && r.pctTecnico !== undefined) {
            var fillStartDate = r.realStart || r.planStart;
            var fillLeftPct = clampPct(xForPct(fillStartDate));
            if (fillLeftPct < pLeftPct) fillLeftPct = pLeftPct;
            var maxRightPct = pLeftPct + pWidthPct;
            var fillWidthPct = pWidthPct * (Math.min(r.pctTecnico, 100) / 100);
            if (fillLeftPct + fillWidthPct > maxRightPct) fillWidthPct = Math.max(maxRightPct - fillLeftPct, 0);
            var realizadoTitle = escapeHtml(r.label) + " — Realizado: " + r.pctTecnico + "% do Progresso Técnico (representação visual proporcional dentro do intervalo planejado; não é uma Data Real de conclusão)";
            barsHtml += '<div class="gantt-bar gantt-bar-realizado" style="left:' + fillLeftPct + '%;width:' + fillWidthPct + '%" ' +
              'title="' + realizadoTitle + '" aria-label="' + realizadoTitle + '"></div>';
          }
        }
        if (r.replanStart && r.replanEnd) {
          var rpLeftPct = clampPct(xForPct(r.replanStart));
          var rpRightPct = clampPct(xForPct(r.replanEnd));
          var rpWidthPct = Math.max(rpRightPct - rpLeftPct, 0.6);
          barsHtml += '<div class="gantt-bar gantt-bar-replanned" style="left:' + rpLeftPct + '%;width:' + rpWidthPct + '%" ' +
            'title="' + escapeHtml(r.label) + ' — Replanejado: ' + formatFullDate(r.replanStart) + ' → ' + formatFullDate(r.replanEnd) + '" ' +
            'aria-label="' + escapeHtml(r.label) + ' replanejado de ' + formatFullDate(r.replanStart) + ' a ' + formatFullDate(r.replanEnd) + '"></div>';
        }
      }
      barsHtml += todayLineHtml;
      var statusChip = r.status ? '<span class="gantt-row-status badge b-' + statusClass(r.status) + '">' + escapeHtml(r.status) + "</span>" : "";
      var tecnicoChip = (r.pctTecnico !== null && r.pctTecnico !== undefined) ? '<span class="gantt-row-pct">' + r.pctTecnico + "%</span>" : "";
      return (
        '<div class="gantt-row">' +
        '<div class="gantt-row-label"><span class="gantt-row-name">' + escapeHtml(r.label) + "</span>" + statusChip + tecnicoChip + "</div>" +
        '<div class="gantt-row-track">' + barsHtml + "</div>" +
        "</div>"
      );
    }).join("");

    return (
      '<div class="gantt-section">' +
      sectionTitle("Gráfico de Gantt — Ondas do MVP 1.0") +
      '<div class="gantt-legend">' +
        '<span class="gantt-legend-item"><span class="gantt-swatch gantt-swatch-planned"></span>Planejado</span>' +
        '<span class="gantt-legend-item"><span class="gantt-swatch gantt-swatch-realizado"></span>Realizado</span>' +
        (hasReplanned ? '<span class="gantt-legend-item"><span class="gantt-swatch gantt-swatch-replanned"></span>Replanejado</span>' : "") +
        '<span class="gantt-legend-item"><span class="gantt-swatch gantt-swatch-today"></span>Data atual</span>' +
      "</div>" +
      '<div class="gantt-scroll">' +
        '<div class="gantt-inner">' +
          '<div class="gantt-row gantt-row-axis">' +
            '<div class="gantt-row-label" aria-hidden="true"></div>' +
            '<div class="gantt-row-track gantt-axis">' + ticksHtml + todayLineHtml + "</div>" +
          "</div>" +
          rowsHtml +
        "</div>" +
      "</div>" +
      "</div>"
    );
  }

  function renderRoadmap(state) {
    var html = "";
    var versao = stripMd(kv(state, "Cabeçalho", "Project Version") || "");
    var statusGeral = kv(state, "Cabeçalho", "Status");
    var mvpPct = mvpGlobalPercent(state);

    html += '<div class="cockpit-hero">';
    html += '<div class="cockpit-hero-id"><span class="ch-name">' + escapeHtml(PROJECT_NAME_LABEL) + '</span><span class="ch-version">' + escapeHtml(versao) + "</span></div>";
    html += '<div class="cockpit-hero-status">' + (statusGeral ? '<span class="badge b-neutro">' + escapeHtml(statusGeral) + "</span>" : naSpan()) + "</div>";
    html += '<div class="cockpit-hero-mvp">';
    html += '<span class="mvp-label">Percentual Global do MVP 1.0</span>';
    html += '<div class="mvp-bar-track">' + (mvpPct !== null ? '<div class="mvp-bar-fill" style="width:' + mvpPct.exact + '%"></div>' : '<div class="mvp-bar-fill pending"></div>') + "</div>";
    html += '<span class="mvp-pct' + (mvpPct === null ? " pending" : "") + '" ' + (mvpPct !== null ? 'title="Valor exato: ' + String(mvpPct.exact).replace(".", ",") + '%"' : "") + '>' + (mvpPct !== null ? mvpPct.rounded + "%" : "N/D") + "</span>";
    html += "</div></div>";
    if (mvpPct === null) html += noticeBox("pend", "Percentual Global do MVP: não encontrado na linha \"Total\" da tabela \"Percentual Global do MVP 1.0\" — nenhum valor foi estimado ou calculado fora do <code>DASHBOARD_STATE.md</code>. Publicação deve ser bloqueada até correção da fonte.");

    var foundationStatus = kv(state, "Foundation", "Status");
    var foundationTecnico = firstPercent(kv(state, "Foundation", "Progresso Técnico"));
    var foundationMvp = kv(state, "Foundation", "Contribuição ao MVP");
    var foundationPeso = kv(state, "Foundation", "Peso no MVP");
    var foundationObs = kv(state, "Foundation", "Observações");
    var foundationReal = kv(state, "Foundation", "Data Real");

    html += renderGantt(state);

    html += '<div style="margin-top:var(--space-7)"></div>' + sectionTitle("Foundation");
    html += waveCard({
      name: "Foundation", status: foundationStatus, pctTecnico: foundationTecnico, pctMvp: foundationMvp, peso: foundationPeso,
      dates: [{ label: "Data Real", value: foundationReal }],
      gate: null, objetivo: null, resultado: null, observacoes: foundationObs, entregaveis: null
    });

    html += '<div style="margin-top:var(--space-6)"></div>' + sectionTitle("Ondas do MVP 1.0");
    var labels = ondaLabels(state);
    html += '<div class="grid" style="grid-template-columns:1fr">';
    if (!labels.length) {
      html += noticeBox("pend", NA_TEXT);
    } else {
      labels.forEach(function (onda) {
        html += waveCard({
          name: onda + (roadmapField(state, "Nome", onda) ? " — " + roadmapField(state, "Nome", onda) : ""),
          status: roadmapField(state, "Status", onda),
          pctTecnico: firstPercent(roadmapField(state, "Progresso Técnico", onda)),
          pctMvp: roadmapField(state, "Contribuição ao MVP", onda),
          peso: roadmapField(state, "Peso no MVP", onda),
          duracao: roadmapField(state, "Duração Planejada", onda),
          dates: [
            { label: "Início Planejado", value: roadmapField(state, "Início Planejado", onda) },
            { label: "Fim Planejado", value: roadmapField(state, "Fim Planejado", onda) },
            { label: "Início Real", value: roadmapField(state, "Início Real", onda) },
            { label: "Fim Real", value: roadmapField(state, "Fim Real", onda) },
            { label: "Início Replanejado", value: roadmapField(state, "Início Replanejado", onda) },
            { label: "Fim Replanejado", value: roadmapField(state, "Fim Replanejado", onda) }
          ],
          gate: roadmapField(state, "Gate", onda),
          objetivo: roadmapField(state, "Objetivo", onda),
          resultado: roadmapField(state, "Resultado Esperado", onda),
          observacoes: roadmapField(state, "Observações", onda),
          entregaveis: entregaveisFor(state, onda)
        });
      });
    }
    html += "</div>";

    document.getElementById("panel-roadmap").innerHTML = html;
    wireWaveCardToggles(document.getElementById("panel-roadmap"));
  }

  function toggleWaveCard(head) {
    var el = document.getElementById(head.getAttribute("data-toggle"));
    if (!el) return;
    el.classList.toggle("collapsed");
    head.setAttribute("aria-expanded", el.classList.contains("collapsed") ? "false" : "true");
  }
  function wireWaveCardToggles(root) {
    root.querySelectorAll(".wave-head").forEach(function (head) {
      head.addEventListener("click", function () { toggleWaveCard(head); });
      head.addEventListener("keydown", function (e) {
        if (e.key === "Enter" || e.key === " " || e.key === "Spacebar") { e.preventDefault(); toggleWaveCard(head); }
      });
    });
  }

  /* Campo vazio não aparece: nunca travessão, "Pendente" ou explicação
     entre parênteses — apenas omitido (ver DASHBOARD_STATE.md §Política das datas). */
  function isEmptyValue(v) { return v === null || v === undefined || v === "" || v === "—"; }

  var waveIdCounter = 0;
  function waveCard(w) {
    var id = "wave-" + (waveIdCounter++);
    var bodyId = id + "-body";
    var pctTecnicoKnown = w.pctTecnico !== null && w.pctTecnico !== undefined;
    var pctMvpKnown = w.pctMvp !== null && w.pctMvp !== undefined;
    var fillHtml = pctTecnicoKnown
      ? '<div class="fill" style="width:' + w.pctTecnico + '%"></div>'
      : '<div class="fill pending" style="width:0%"></div>';
    var hasEntregaveis = w.entregaveis !== null && w.entregaveis !== undefined;
    var entregaveis = w.entregaveis || [];
    var counts = entregaveisCounts(entregaveis);
    var entregaveisHtml = entregaveis.length
      ? '<div class="deliv-list">' + entregaveis.map(function (e) {
          var cls = statusClass(e.status);
          var check = cls === "concluido" ? '<span class="deliv-check" aria-hidden="true">✓</span>' : "";
          return '<div class="deliv-row"><span class="deliv-name">' + check + escapeHtml(e.nome) + "</span>" + statusBadge(e.status) +
            (e.pct !== null && e.pct !== undefined ? '<span class="deliv-pct">' + e.pct + "%</span>" : "") +
            (e.obs ? '<div class="deliv-obs">' + escapeHtml(e.obs) + "</div>" : "") + "</div>";
        }).join("") + "</div>"
      : "";
    var countsHtml = entregaveis.length
      ? '<div class="wave-counts">' + countChip("total", counts.total, "grey") + countChip("concluído", counts.concluido, "green") + countChip("em desenvolvimento", counts.andamento, "orange") + countChip("planejado", counts.planejado, "blue") + "</div>"
      : "";

    var datesHtml = (w.dates || []).filter(function (d) { return !isEmptyValue(d.value); })
      .map(function (d) { return waveDate(d.label, d.value); }).join("");
    var datesBlock = datesHtml ? '<div class="wave-dates">' + datesHtml + (w.duracao && !isEmptyValue(w.duracao) ? '<div class="wave-duration">' + escapeHtml(w.duracao) + "</div>" : "") + "</div>" : "";

    var gateBlock = !isEmptyValue(w.gate) ? '<div class="wave-gate">Gate: ' + escapeHtml(w.gate) + "</div>" : "";
    var objetivoCollapsedBlock = !isEmptyValue(w.objetivo) ? '<div class="wave-objetivo-collapsed">' + escapeHtml(w.objetivo) + "</div>" : "";
    var objetivoBlock = !isEmptyValue(w.objetivo) ? sectionTitle("Objetivo") + '<div class="card-body" style="margin-bottom:var(--space-4)">' + escapeHtml(w.objetivo) + "</div>" : "";
    var resultadoBlock = !isEmptyValue(w.resultado) ? sectionTitle("Resultado Esperado") + '<div class="card-body" style="margin-bottom:var(--space-4)">' + escapeHtml(w.resultado) + "</div>" : "";
    var entregaveisBlock = hasEntregaveis ? sectionTitle("Entregáveis") + entregaveisHtml : "";
    var observacoesBlock = !isEmptyValue(w.observacoes) ? sectionTitle("Observações") + '<div class="card-body" style="margin-top:var(--space-2)">' + escapeHtml(w.observacoes) + "</div>" : "";

    var pctMvpDisplay = pctMvpKnown ? pointsToDisplayPercent(w.pctMvp) : null;
    var pctTecnicoText = pctTecnicoKnown ? w.pctTecnico + "%" : "0%";
    var mvpContribBlock = pctMvpDisplay
      ? '<div class="wave-mvp-contrib">' +
          '<span class="wp-label">Contribuição ao MVP</span>' +
          '<span class="wp-value wp-mvp" title="Valor exato: ' + escapeHtml(w.pctMvp) + '">' + escapeHtml(pctMvpDisplay) + "</span>" +
        "</div>"
      : "";

    return (
      '<div class="wave-card collapsed" id="' + id + '">' +
      '<div class="wave-head" data-toggle="' + id + '" role="button" tabindex="0" aria-expanded="false" aria-controls="' + bodyId + '">' +
      '<span class="wave-chev" aria-hidden="true">▼</span><span class="wave-name">' + escapeHtml(w.name || "—") + "</span>" +
      statusBadge(w.status) +
      (!isEmptyValue(w.peso) ? '<span class="wave-weight">Peso Gerencial: ' + escapeHtml(w.peso) + "</span>" : "") +
      "</div>" +
      '<div class="wave-progress-row">' +
        '<div class="wave-progress-labeled">' +
          '<span class="wp-label">Progresso Técnico</span>' +
          '<div class="wave-progress">' + fillHtml + "</div>" +
          '<span class="wp-value wp-tecnico">' + escapeHtml(pctTecnicoText) + "</span>" +
        "</div>" +
        mvpContribBlock +
      "</div>" +
      countsHtml +
      datesBlock +
      gateBlock +
      objetivoCollapsedBlock +
      '<div class="wave-body" id="' + bodyId + '">' +
      objetivoBlock +
      resultadoBlock +
      entregaveisBlock +
      observacoesBlock +
      "</div>" +
      "</div>"
    );
  }

  function waveDate(label, value) {
    return '<div><div class="dl">' + escapeHtml(label) + '</div><div class="dv">' + escapeHtml(value) + "</div></div>";
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

    document.getElementById("backlog-state-summary").innerHTML = html;

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
    renderRoadmap(state);
    renderExecutive(state);
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
    var tabs = document.querySelectorAll(".nav-item");
    tabs.forEach(function (btn) {
      btn.addEventListener("click", function () {
        var target = btn.getAttribute("data-tab");
        tabs.forEach(function (b) { b.setAttribute("aria-current", b === btn ? "true" : "false"); });
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
      .then(function (md) {
        boot(md);
        renderBacklogDetalhado();
        renderFlow();
      })
      .catch(function (err) {
        showError("Falha ao buscar " + STATE_URL + " (" + err.message + "). Provável bloqueio de CORS em file://.");
      });
  }

  document.addEventListener("DOMContentLoaded", init);

  /* ══════════════════════════════════════════════════════════════════
     PARTE 2 (preservada) — Backlog detalhado (67 itens) / Fluxo de
     Compras / Arquitetura. Conteúdo e lógica idênticos aos que já
     existiam neste Dashboard antes desta correção — apenas preservados,
     sem alteração de dados ou regra. Fonte: .ai/BACKLOG.md (snapshot).
     ══════════════════════════════════════════════════════════════════ */

  var BACKLOG_DATA = [{"code": "A1", "fase": "Foundation", "nome": "Arquitetura Base", "objetivo": "Estabelecer solution .NET, projetos, camadas, contratos fundamentais, convenções, estrutura inicial e health check.", "deps": "Nenhuma além da inicialização.", "status": "Implementado", "obs": "Código, testes e histórico Git comprovam a fundação. Ver Work Order."}, {"code": "A2", "fase": "Foundation", "nome": "AI Runtime", "objetivo": "Implementar contratos de modelos de IA, providers, mensagens, configuração, execução e tratamento básico de respostas.", "deps": "A1.", "status": "Implementado", "obs": "Código, testes e histórico Git comprovam o runtime. Ver Work Order."}, {"code": "A3", "fase": "Foundation", "nome": "Agent Framework", "objetivo": "Implementar agentes, agente-base, contexto, resultados, fábrica, registro e execução padronizada.", "deps": "A2.", "status": "Implementado", "obs": "Código, testes e histórico Git comprovam o framework. Ver Work Order."}, {"code": "A4", "fase": "Foundation", "nome": "Workflow e Observabilidade Fundamental", "objetivo": "Implementar workflow sequencial básico, logging estruturado, correlation ID, métricas fundamentais e diagnóstico.", "deps": "A3.", "status": "Implementado", "obs": "Código, testes e histórico Git comprovam o workflow básico. Ver Work Order."}, {"code": "A5", "fase": "Foundation", "nome": "Configuração Multiempresa", "objetivo": "Definir empresas, unidades de negócio, configurações isoladas, feature flags e preparação para multi-tenancy.", "deps": "A1; Identity e persistência futuras.", "status": "Não comprovado", "obs": "Não há evidência suficiente. Ver Work Order."}, {"code": "A6", "fase": "Foundation", "nome": "Agente Comprador Sênior", "objetivo": "Implementar estratégias de negociação, análise de contexto, memória de negociação e recomendações de compra.", "deps": "A2 e A3.", "status": "Parcial", "obs": "Parcial: estratégia e memória existem; agente concreto não. Ver Work Order."}, {"code": "A7", "fase": "Foundation", "nome": "Sistema de Documentação", "objetivo": "Implementar geração e publicação de documentação executiva, cliente e engenharia nos formatos definidos.", "deps": "A1.", "status": "Implementado", "obs": "Código Documentation e histórico Git comprovam a entrega. Ver Work Order."}, {"code": "B1", "fase": "Sourcing Intelligence", "nome": "Cadastro e Perfil de Fornecedores", "objetivo": "Criar domínio, persistência e APIs para fornecedores, contatos, categorias, unidades atendidas e situação cadastral.", "deps": "A1; H1/H2 propostos.", "status": "Concluída", "obs": "Código, migration e validação de conectividade concluídos; aplicação da migration pendente de autorização."}, {"code": "B2", "fase": "Sourcing Intelligence", "nome": "Descoberta Inicial de Fornecedores", "objetivo": "Consultar o ERP SOMA_DESENV somente para leitura, aplicar score explicável e persistir descobertas no +Compras.", "deps": "B1.", "status": "Concluída", "obs": "Código, testes e commit `a19e496`; validação operacional ERP pendente de ambiente. Score é estrutura inicial; não desclassifica a entrega."}, {"code": "B2.1", "fase": "Sourcing Intelligence", "nome": "Validação Operacional e Sincronização de Fornecedores com ERP", "objetivo": "Sincronizar fornecedores entre +Compras e ERP com contrato canônico, adaptadores por BU, regra temporal, inativação, idempotência e auditoria imutável.", "deps": "B1; B2; acesso ao ERP SOMA_DESENV.", "status": "Concluída", "obs": "Importação/exportação, atualização, inativação, auditoria e concorrência validadas; commits `b08769f` e `3b6d54b`. CLIFORs 315501, 315502, 315503 e 315505 confirmados no Linx."}, {"code": "B2.1.1", "fase": "Sourcing Intelligence", "nome": "Completar Mapeamento Canônico ERP → +Compras", "objetivo": "Preencher o contrato canônico com dados de identificação, endereço, contato, banco, comercial, fiscal e fornecimento.", "deps": "B2.1.", "status": "Concluída", "obs": "Mapeamento e importação idempotente comprovados; commit `0240c35`. B2.1.2 concluída posteriormente."}, {"code": "B2.1.2", "fase": "Sourcing Intelligence", "nome": "Validação Operacional e Sincronização de Fornecedores com ERP", "objetivo": "Consultar fornecedores no ERP `SOMA_DESENV` e sincronizar para o banco `MaisCompras` por uma camada de integração desacoplada.", "deps": "B2.1; B2.1.1; modelo Linx alinhado; VPN e SQL Server corporativo.", "status": "Concluída", "obs": "`IFornecedorErpReader`, `SomaFornecedorReader`, `SincronizarFornecedoresErpUseCase`, endpoint `GET /api/fornecedores/sincronizar-erp`, testes unitários e teste de integração condicionado à VPN/configuração. O alinhamento estrutural Linx foi concluído no commit `77861eb`; o fluxo real SOMA → +Compras foi endurecido posteriormente na B2.1.3."}, {"code": "B2.1.3", "fase": "Sourcing Intelligence", "nome": "Endurecimento da Integração ERP de Fornecedores", "objetivo": "Tornar a sincronização ERP de fornecedores uma rotina operacional com lotes, histórico, erros parciais, logs e métricas.", "deps": "B2.1.2; VPN e SQL Server corporativo para validação real.", "status": "Concluída", "obs": "Leitura paginada, entidades `SincronizacaoFornecedor` e `ErroSincronizacaoFornecedor`, migration `202608020001_B213FornecedorErpSyncHardening`, retorno detalhado do endpoint, logs estruturados, 8 testes unitários em `SincronizarFornecedoresErpUseCaseTests`, e validação real executada em 02/08/2026 contra API em Docker, VPN corporativa e banco `MaisCompras`. `dotnet build` e `dotnet test backend/BlueprintOS.sln` aprovados. Nenhuma regra de negócio foi alterada."}, {"code": "B2.2", "fase": "Sourcing Intelligence", "nome": "Consulta CNPJ e Enriquecimento de Fornecedor", "objetivo": "Consultar dados externos por `Cnpj_Cpf` como sugestão revisável para o cadastro +Compras, com auditoria e sem atualização automática de ERP.", "deps": "B2.1, B2.1.1 e B2.1.2 concluídas; provedor externo gratuito BrasilAPI para B2.2.2.", "status": "Concluída", "obs": "B2.2.1 a B2.2.4 concluídas: ICnpjConsultaProvider, BrasilApiCnpjProvider, comparação/aprovação/rejeição e tela React CadastroFornecedor. Consulta segue como sugestão revisável; não há atualização automática de ERP."}, {"code": "B3", "fase": "Sourcing Intelligence", "nome": "Cadastro e Integração de Itens", "objetivo": "Criar consulta ERP, cadastro próprio, famílias, categorias, seleção manual e relacionamentos com fornecedores.", "deps": "B1; B2.1 e B2.2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão. Requer aprovação explícita."}, {"code": "B4", "fase": "Sourcing Intelligence", "nome": "Compras e Pedidos Operacionais", "objetivo": "Criar solicitação, rascunho, itens, aprovação humana, persistência +Compras e status de integração.", "deps": "B3.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão. Requer aprovação explícita."}, {"code": "B5", "fase": "Sourcing Intelligence", "nome": "Portal Operacional Integrado", "objetivo": "Evoluir o portal como interface dos módulos de fornecedor, item e pedido, com seleção e cadastro manuais.", "deps": "B1 a B4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "B6", "fase": "Sourcing Intelligence", "nome": "Integrações ERP por BU", "objetivo": "Consolidar adaptadores desacoplados, criação confirmada de pedido, identificador externo e reprocessamento.", "deps": "B3 e B4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "B7", "fase": "Sourcing Intelligence", "nome": "Fluxo Operacional Ponta a Ponta", "objetivo": "Validar o ciclo fornecedor, item, pedido, integração e auditoria técnica básica.", "deps": "B1 a B6.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C1", "fase": "Negotiation Automation", "nome": "Dossiê de Negociação", "objetivo": "Consolidar histórico, fornecedor, preços, riscos, demanda, metas e argumentos antes da negociação.", "deps": "B1, B3, B4 e B6.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C2", "fase": "Negotiation Automation", "nome": "Planejador de Negociação", "objetivo": "Gerar estratégia, objetivo, faixa-alvo, concessões, alternativas, limites e sequência de negociação.", "deps": "C1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C3", "fase": "Negotiation Automation", "nome": "Agente de Negociação", "objetivo": "Executar negociações assistidas ou automatizadas por canais controlados, mantendo contexto e regras de autonomia.", "deps": "C2 e C5.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C4", "fase": "Negotiation Automation", "nome": "Memória Persistente de Negociação", "objetivo": "Persistir interações, propostas, contrapropostas, decisões, aprendizados e resultados.", "deps": "C3; persistência proposta.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C5", "fase": "Negotiation Automation", "nome": "Aprovações e Alçadas", "objetivo": "Implementar limites de autonomia, aprovação humana, segregação de funções e trilha de decisão.", "deps": "H1 e H2 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C6", "fase": "Negotiation Automation", "nome": "Avaliação de Resultado", "objetivo": "Comparar resultado negociado com baseline, meta, orçamento, mercado e histórico.", "deps": "C1, C3 e C4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "C7", "fase": "Negotiation Automation", "nome": "Central de Negociações", "objetivo": "Disponibilizar fila, status, intervenções humanas, resultados, alertas e indicadores de negociação.", "deps": "C1 a C6; H1/H2 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D1", "fase": "Contract & Compliance", "nome": "Integração com Plataforma Jurídica", "objetivo": "Criar contratos de integração para consulta de contratos, vigência, partes, status e metadados jurídicos.", "deps": "G1; plataforma jurídica a aprovar.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D2", "fase": "Contract & Compliance", "nome": "Obrigações e Marcos Contratuais", "objetivo": "Controlar entregas, renovações, reajustes, vencimentos, garantias e obrigações associadas à compra.", "deps": "D1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D3", "fase": "Contract & Compliance", "nome": "Compliance de Compras", "objetivo": "Validar políticas internas, documentação obrigatória, concorrência, alçadas e impedimentos.", "deps": "B3, C5 e H2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D4", "fase": "Contract & Compliance", "nome": "Agente de Compliance", "objetivo": "Avaliar processos de compra, explicar inconsistências e recomendar correções antes da aprovação.", "deps": "D3, Knowledge e AI Runtime.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D5", "fase": "Contract & Compliance", "nome": "Gestão de Exceções", "objetivo": "Registrar desvios, justificativas, aprovações extraordinárias, responsáveis e prazo de regularização.", "deps": "D3 e C5.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D6", "fase": "Contract & Compliance", "nome": "Auditoria e Evidências", "objetivo": "Gerar trilha imutável de ações, decisões, documentos, agentes, usuários e integrações.", "deps": "D1 a D5; H4/H5 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "D7", "fase": "Contract & Compliance", "nome": "Painel Contratual e de Compliance", "objetivo": "Consolidar vencimentos, obrigações, riscos, exceções e conformidade operacional.", "deps": "D1 a D6; H1/H2 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E1", "fase": "Supplier Risk & ESG", "nome": "Modelo de Risco de Fornecedor", "objetivo": "Definir dimensões, indicadores, pesos, níveis, histórico e metodologia explicável de risco.", "deps": "B1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E2", "fase": "Supplier Risk & ESG", "nome": "Integração de Dados de Risco", "objetivo": "Consumir fontes internas e externas autorizadas sobre situação financeira, fiscal, operacional e reputacional.", "deps": "E1 e G1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E3", "fase": "Supplier Risk & ESG", "nome": "Monitoramento Contínuo", "objetivo": "Executar reavaliações periódicas, detectar alterações e gerar alertas relevantes.", "deps": "E1 e E2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E4", "fase": "Supplier Risk & ESG", "nome": "Agente de Risco", "objetivo": "Interpretar sinais, produzir análise explicável e recomendar mitigação ou bloqueio.", "deps": "E1 a E3; AI Runtime.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E5", "fase": "Supplier Risk & ESG", "nome": "Avaliação ESG", "objetivo": "Registrar critérios ambientais, sociais e de governança por fornecedor e categoria.", "deps": "B1 e B2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E6", "fase": "Supplier Risk & ESG", "nome": "Planos de Mitigação", "objetivo": "Criar ações, responsáveis, prazos, evidências e acompanhamento para riscos e desvios ESG.", "deps": "E1, E4 e E5.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "E7", "fase": "Supplier Risk & ESG", "nome": "Cockpit de Risco e ESG", "objetivo": "Exibir mapa de risco, evolução, criticidade, alertas, mitigação e exposição da cadeia.", "deps": "E1 a E6; H1/H2 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F1", "fase": "Predictive Analytics", "nome": "Camada Analítica de Compras", "objetivo": "Criar modelos de dados analíticos, indicadores, dimensões, fatos e pipelines de atualização.", "deps": "B3; persistência e integração propostas.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F2", "fase": "Predictive Analytics", "nome": "Previsão de Demanda", "objetivo": "Projetar demanda por item, categoria, empresa, unidade e período usando histórico disponível.", "deps": "F1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F3", "fase": "Predictive Analytics", "nome": "Previsão de Preços", "objetivo": "Estimar tendências e intervalos de preço, deixando explícitos nível de confiança e limitações.", "deps": "F1 e B4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F4", "fase": "Predictive Analytics", "nome": "Previsão de Lead Time", "objetivo": "Estimar prazo de entrega e probabilidade de atraso por fornecedor, item e contexto.", "deps": "F1, B1 e B3.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F5", "fase": "Predictive Analytics", "nome": "Detecção de Anomalias", "objetivo": "Identificar desvios em preço, volume, frequência, fornecedor, pedido e comportamento operacional.", "deps": "F1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F6", "fase": "Predictive Analytics", "nome": "Simulação de Cenários", "objetivo": "Comparar fornecedores, lotes, prazos, condições, concentração, câmbio e estratégias de compra.", "deps": "F1 a F5.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "F7", "fase": "Predictive Analytics", "nome": "Analytics Executivo", "objetivo": "Consolidar savings, riscos, previsões, eficiência, compliance e oportunidades para gestão.", "deps": "F1 a F6; D/E propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G1", "fase": "Marketplace & Integrations", "nome": "Integration Framework", "objetivo": "Criar contratos, adapters, filas, retries, idempotência, telemetria e governança de integrações.", "deps": "A1; H4/H5 propostos.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G2", "fase": "Marketplace & Integrations", "nome": "Integração ERP de Requisições", "objetivo": "Receber requisições e demandas dos diferentes ERPs das unidades de negócio.", "deps": "G1; ERP a identificar.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G3", "fase": "Marketplace & Integrations", "nome": "Integração ERP de Pedidos", "objetivo": "Criar, atualizar e consultar pedidos nos ERPs responsáveis por cada unidade de negócio.", "deps": "G1 e B3; ERP a identificar.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G4", "fase": "Marketplace & Integrations", "nome": "Integração de Notas Fiscais", "objetivo": "Consultar e registrar informações de notas fiscais e seu vínculo com pedidos e fornecedores.", "deps": "G1 e G3.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G5", "fase": "Marketplace & Integrations", "nome": "Integração n8n e Workflows Externos", "objetivo": "Permitir automações externas governadas, autenticadas, auditáveis e idempotentes.", "deps": "G1, H1, H2 e H4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G6", "fase": "Marketplace & Integrations", "nome": "Portal e Marketplace de Fornecedores", "objetivo": "Permitir interação controlada de fornecedores para cadastro, documentos, propostas e acompanhamento.", "deps": "B1, G1 e H1/H2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "G7", "fase": "Marketplace & Integrations", "nome": "Central de Integrações", "objetivo": "Gerenciar conexões, credenciais, status, falhas, filas, reprocessamentos e indicadores.", "deps": "G1 a G6; H4/H5.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H1", "fase": "Enterprise Scale & Governance", "nome": "Identidade Corporativa com Entra ID", "objetivo": "Implementar autenticação, claims, grupos, usuários, service principals e integração com Microsoft Entra ID.", "deps": "A1; tenant Entra a aprovar.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H2", "fase": "Enterprise Scale & Governance", "nome": "Autorização e Segregação de Funções", "objetivo": "Implementar papéis, permissões, políticas, alçadas e segregação por aplicativo e empresa.", "deps": "H1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H3", "fase": "Enterprise Scale & Governance", "nome": "Multi-Tenancy em Produção", "objetivo": "Garantir isolamento lógico, configuração, segurança, dados e operação por empresa e unidade de negócio.", "deps": "H1 e H2.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H4", "fase": "Enterprise Scale & Governance", "nome": "Observabilidade Corporativa", "objetivo": "Implementar logs, métricas, tracing, alertas, dashboards, SLOs, auditoria operacional e custos de IA.", "deps": "H1/H2 e infraestrutura proposta.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H5", "fase": "Enterprise Scale & Governance", "nome": "Segurança, LGPD e Governança de IA", "objetivo": "Implementar classificação de dados, retenção, consentimento, anonimização, controles e governança dos agentes.", "deps": "H1 a H4.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H6", "fase": "Enterprise Scale & Governance", "nome": "Plataforma Cloud e CI/CD", "objetivo": "Preparar Google Cloud, pipelines, ambientes, secrets, infraestrutura como código, backup e recuperação.", "deps": "H1 a H5; G1.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "H7", "fase": "Enterprise Scale & Governance", "nome": "Produção, Escala e Operação Assistida", "objetivo": "Executar readiness review, testes de carga, runbooks, suporte, continuidade, rollout e acompanhamento produtivo.", "deps": "Demais capacidades necessárias para produção.", "status": "Planejado", "obs": "Não executada; sem evidência de conclusão."}, {"code": "A8", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Audience-Specific Publishers", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Não comprovado", "obs": "Referência histórica; confirmar por código/Git antes de considerar concluída."}, {"code": "A9", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Publication Engine", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Implementado", "obs": "Código de publicação e histórico Git."}, {"code": "A10", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Governance and Work Order Foundation", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Implementado", "obs": "Documentação e histórico Git."}, {"code": "A11", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Engineering Blueprint", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Implementado", "obs": "Documento e histórico Git."}, {"code": "A12", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Especificação Oficial das 56 Work Orders", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Implementado", "obs": "Catálogo, Work Orders e validações documentais desta sprint."}, {"code": "Portal +Compras Frontend", "fase": "Entregas históricas (fora do catálogo de 56)", "nome": "Portal +Compras Frontend", "objetivo": "Entrega histórica fora do catálogo oficial de 56 sprints.", "deps": "-", "status": "Concluído tecnicamente no frontend (parcial)", "obs": "Commit `8ee8f4e`; shell/navegação, módulo Fornecedores conectado à API real, demais módulos demonstrativos, Design System AZZAS 2154/GDT aplicado; build frontend aprovado. Backend não revalidado neste ciclo (sem SDK .NET no ambiente)."}];

  function backlogStatusClass(s) { return "st-" + s.replace(/ /g, "\\ ").replace(/\(/g, "\\(").replace(/\)/g, "\\)"); }
  function backlogTrackText(d) { if (d.status === "Implementado" || d.status === "Concluída" || d.status === "Concluída em código") return "✓ Concluído"; if (d.status === "Parcial" || d.status === "Concluído tecnicamente no frontend (parcial)") return "Em andamento"; if (d.status === "Planejado") return "A fazer"; return ""; }
  function backlogTrackClass(d) { if (d.status === "Implementado" || d.status === "Concluída" || d.status === "Concluída em código") return "mt-done"; if (d.status === "Parcial" || d.status === "Concluído tecnicamente no frontend (parcial)") return "mt-doing"; if (d.status === "Planejado") return "mt-todo"; return ""; }

  function renderBacklogListSummary() {
    var total = BACKLOG_DATA.length;
    var counts = {};
    BACKLOG_DATA.forEach(function (d) { counts[d.status] = (counts[d.status] || 0) + 1; });
    var done = (counts["Implementado"] || 0) + (counts["Concluída"] || 0) + (counts["Concluída em código"] || 0);
    var el = document.getElementById("summary");
    if (!el) return;
    el.innerHTML =
      '<div class="card"><div class="num">' + total + '</div><div class="lbl">Sprints totais</div></div>' +
      '<div class="card"><div class="num">' + done + '</div><div class="lbl">Implementadas/Concluídas</div></div>' +
      '<div class="card"><div class="num">' + (counts["Parcial"] || 0) + '</div><div class="lbl">Parciais</div></div>' +
      '<div class="card"><div class="num">' + (counts["Planejado"] || 0) + '</div><div class="lbl">Planejadas</div></div>' +
      '<div class="card" style="flex:1.4"><div class="num">' + done + "/" + total + '</div><div class="lbl">Progresso — implementadas/concluídas</div>' +
      '<div class="progress-outer"><div class="progress-inner" style="width:' + (done / total * 100).toFixed(0) + '%"></div></div></div>';
  }
  function backlogGroupByPhase(list) {
    var order = []; var map = {};
    list.forEach(function (d) { if (!map[d.fase]) { map[d.fase] = []; order.push(d.fase); } map[d.fase].push(d); });
    return order.map(function (f) { return { fase: f, items: map[f] }; });
  }
  function renderBacklogList() {
    var searchEl = document.getElementById("search");
    var filterEl = document.getElementById("filterStatus");
    var container = document.getElementById("phases");
    if (!searchEl || !filterEl || !container) return;
    var q = searchEl.value.trim().toLowerCase();
    var fStatus = filterEl.value;
    var filtered = BACKLOG_DATA.filter(function (d) {
      if (fStatus && d.status !== fStatus) return false;
      if (q) { var hay = (d.code + " " + d.nome + " " + d.objetivo).toLowerCase(); if (hay.indexOf(q) === -1) return false; }
      return true;
    });
    var groups = backlogGroupByPhase(filtered);
    container.innerHTML = "";
    groups.forEach(function (g) {
      var total = g.items.length;
      var done = g.items.filter(function (d) { return d.status === "Implementado" || d.status === "Concluída" || d.status === "Concluída em código"; }).length;
      var groupEl = document.createElement("div");
      groupEl.className = "phase-group";
      var header = document.createElement("div");
      header.className = "phase-header";
      header.innerHTML = '<div><span class="chev">▼</span> <span class="phase-title">' + escapeHtml(g.fase) + '</span></div><div class="phase-meta">' + done + "/" + total + " implementadas/concluídas</div>";
      header.addEventListener("click", function () { groupEl.classList.toggle("collapsed"); });
      groupEl.appendChild(header);
      var body = document.createElement("div");
      body.className = "phase-body";
      g.items.forEach(function (d) {
        var txt = backlogTrackText(d);
        var row = document.createElement("div");
        row.className = "sprint-row";
        var trackHtml = txt ? '<span class="' + backlogTrackClass(d) + '">' + txt + "</span>" : "";
        row.innerHTML = '<div class="code">' + escapeHtml(d.code) + '</div><div><div class="name">' + escapeHtml(d.nome) + '</div><div class="obj">' + escapeHtml(d.objetivo) + '</div><div class="deps">Depende de: ' + escapeHtml(d.deps) + "</div>" + (d.obs ? '<div class="obs">' + escapeHtml(d.obs) + "</div>" : "") + '</div><div><span class="badge ' + backlogStatusClass(d.status) + '">' + escapeHtml(d.status) + '</span></div><div class="mytrack">' + trackHtml + "</div>";
        body.appendChild(row);
      });
      groupEl.appendChild(body);
      container.appendChild(groupEl);
    });
  }
  function renderBacklogDetalhado() {
    renderBacklogListSummary();
    renderBacklogList();
    var searchEl = document.getElementById("search");
    var filterEl = document.getElementById("filterStatus");
    if (searchEl) searchEl.addEventListener("input", renderBacklogList);
    if (filterEl) filterEl.addEventListener("change", renderBacklogList);
  }

  var FLOW = [
    { t: "Usuário solicita", d: "Registra a necessidade de compra sem depender do comprador para iniciar." },
    { t: "IA interpreta", d: "Entende a intenção e transforma a solicitação em dados estruturados." },
    { t: "Sistema categoriza", d: "Define categoria, conta contábil, centro de custo e OPEX/CAPEX." },
    { t: "Valida budget", d: "Confere orçamento disponível e política antes de avançar." },
    { t: "Define aprovadores", d: "Motor de workflow define alçadas e aprovadores automaticamente." },
    { t: "Dispara cotação", d: "Inicia RFQ/RFP com fornecedores homologados quando necessário." },
    { t: "IA equaliza propostas", d: "Compara preço, prazo, frete, impostos e SLA entre fornecedores." },
    { t: "Comprador negocia", d: "Atua estrategicamente usando dados para negociar condições." },
    { t: "Aprovação final", d: "Segue para aprovação conforme política e alçada definida." },
    { t: "Pedido ERP", d: "Gera ou integra o pedido de compra no ERP." },
    { t: "Fornecedor informado", d: "Recebe pedido, prazos e condições de forma automática." },
    { t: "Recebimento", d: "Área solicitante confirma recebimento do item ou execução do serviço." },
    { t: "Carga fiscal e pagamento", d: "Upload de NF, XML, boleto e dados bancários." },
    { t: "Entrada fiscal no ERP", d: "Classificação contábil, impostos e geração de contas a pagar." },
    { t: "Pagamento", d: "Programação, integração bancária e baixa financeira no ERP." },
    { t: "Encerramento", d: "SLA, saving, histórico do fornecedor e retroalimentação da IA." }
  ];
  function renderFlow() {
    var pillsEl = document.getElementById("flowPills");
    var listEl = document.getElementById("flowList");
    if (!pillsEl || !listEl) return;
    pillsEl.innerHTML = FLOW.map(function (s) { return '<span class="flow-pill">' + escapeHtml(s.t) + "</span>"; }).join("");
    listEl.innerHTML = FLOW.map(function (s, i) {
      return '<div class="flow-row"><div class="flow-num">' + (i + 1) + '</div><div><div class="flow-name">' + escapeHtml(s.t) + '</div><div class="flow-desc">' + escapeHtml(s.d) + "</div></div></div>";
    }).join("");
  }
})();
