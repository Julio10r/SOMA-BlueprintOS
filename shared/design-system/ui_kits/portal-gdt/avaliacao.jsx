// AvaliacaoScreen — Batch C
// Tab "Decisão" — visão executiva: KPI hero + ranking sortable + bar chart de áreas

// Performance bucket por demanda (fake)
const PERF_BY_ID = {
  "GDT-042": { tag: "superou",   label: "Superou",  delta: +12.4 },
  "GDT-041": { tag: "dentro",    label: "No prazo", delta:  +2.1 },
  "GDT-039": { tag: "superou",   label: "Superou",  delta: +18.7 },
  "GDT-038": { tag: "dentro",    label: "No prazo", delta:  -1.8 },
  "GDT-035": { tag: "abaixo",    label: "Abaixo",   delta: -32.5 },
  "GDT-033": { tag: "dentro",    label: "No prazo", delta:  +0.9 },
  "GDT-030": { tag: "superou",   label: "Superou",  delta: +24.1 },
};

// Saving estimado vs realizado (fake)
const SAVING_BY_ID = {
  "GDT-042": { est: 340_000, real: 382_000 },
  "GDT-041": { est: 1_200_000, real: 1_225_000 },
  "GDT-039": { est: 2_400_000, real: 2_849_000 },
  "GDT-038": { est: 480_000, real: 471_000 },
  "GDT-035": { est: 90_000, real: 60_750 },
  "GDT-033": { est: 220_000, real: 222_000 },
  "GDT-030": { est: 3_100_000, real: 3_846_000 },
};

function fmtMoney(v) {
  if (v >= 1_000_000) return "R$ " + (v / 1_000_000).toFixed(2) + "M";
  if (v >= 1_000)     return "R$ " + Math.round(v / 1_000) + "K";
  return "R$ " + v.toLocaleString("pt-BR");
}

function AvaliacaoScreen({ data, onToast }) {
  const [sortBy, setSortBy] = React.useState("real");        // "real" | "est" | "delta"
  const [sortDir, setSortDir] = React.useState("desc");      // "asc" | "desc"
  const [expandedId, setExpandedId] = React.useState(null);

  // Cabeçalho KPI: total realizado, composição FTE + ganhos
  const totalReal = data.demands.reduce((s, d) => s + (SAVING_BY_ID[d.id]?.real || 0), 0);
  const totalEst  = data.demands.reduce((s, d) => s + (SAVING_BY_ID[d.id]?.est  || 0), 0);
  const fteValue  = Math.round(totalReal * 0.56);   // ~56% via realocação FTE
  const ganhoVal  = totalReal - fteValue;

  // Ordenação
  const sortedDemands = [...data.demands].sort((a, b) => {
    const sa = SAVING_BY_ID[a.id] || { est: 0, real: 0 };
    const sb = SAVING_BY_ID[b.id] || { est: 0, real: 0 };
    let av, bv;
    if (sortBy === "real")  { av = sa.real; bv = sb.real; }
    else if (sortBy === "est") { av = sa.est; bv = sb.est; }
    else { av = sa.real - sa.est; bv = sb.real - sb.est; }
    return sortDir === "desc" ? bv - av : av - bv;
  });

  const handleSort = (col) => {
    if (sortBy === col) setSortDir(d => d === "desc" ? "asc" : "desc");
    else { setSortBy(col); setSortDir("desc"); }
  };

  const SortArrow = ({ col }) => sortBy !== col ? null :
    <span className="sort-arr">{sortDir === "desc" ? "↓" : "↑"}</span>;

  return (
    <div className="page-main">
      <div className="page-head">
        <div className="page-title">
          <h1>Realização de valor</h1>
          <p>Visão executiva — comparativo entre projeção e valor efetivamente realizado em 2026.</p>
        </div>
      </div>

      {/* KPI hero */}
      <div className="kpi-hero">
        <div className="kpi-hero-top">
          <div>
            <div className="kpi-hero-lbl">Valor total realizado · acumulado 2026</div>
          </div>
          <div className="kpi-hero-icon">
            <Icon name="trend" size={18} />
          </div>
        </div>
        <div className="kpi-hero-val">{fmtMoney(totalReal)}</div>
        <div className="kpi-hero-sub">
          {totalReal > totalEst ? "Acima" : "Abaixo"} da projeção de {fmtMoney(totalEst)}
        </div>
        <div className="kpi-compo">
          <div className="kpi-compo-item">
            <span className="il">FTE realocado</span>
            <span className="iv">{fmtMoney(fteValue)}</span>
          </div>
          <span className="kpi-compo-sep">+</span>
          <div className="kpi-compo-item">
            <span className="il">Ganhos diretos</span>
            <span className="iv">{fmtMoney(ganhoVal)}</span>
          </div>
          <span className="kpi-compo-sep">=</span>
          <div className="kpi-compo-item">
            <span className="il">Total</span>
            <span className="iv">{fmtMoney(totalReal)}</span>
          </div>
        </div>
      </div>

      {/* Bar chart por área */}
      <div className="dash-card">
        <Eyebrow style={{ marginBottom: 12 }}>Demandas por área</Eyebrow>
        <div className="bar-chart">
          {data.areas.map(a => (
            <div className="bar-row" key={a.name}>
              <span className="bar-label">{a.name}</span>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: a.fill + "%", background: a.color }}></div>
              </div>
              <span className="bar-value">{a.count}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Ranking sortable */}
      <Eyebrow style={{ marginTop: 28, marginBottom: 10 }}>Ranking de iniciativas · {sortedDemands.length} demandas</Eyebrow>
      <div className="rank-card">
        <table className="rank-table">
          <colgroup>
            <col style={{ width: "5%" }} />
            <col style={{ width: "38%" }} />
            <col style={{ width: "13%" }} />
            <col style={{ width: "13%", textAlign: "right" }} />
            <col style={{ width: "13%", textAlign: "right" }} />
            <col style={{ width: "18%", textAlign: "right" }} />
          </colgroup>
          <thead>
            <tr>
              <th>#</th>
              <th>Iniciativa</th>
              <th>Performance</th>
              <th className={"sortable" + (sortBy === "est" ? " active" : "")} onClick={() => handleSort("est")}>
                Estimado <SortArrow col="est" />
              </th>
              <th className={"sortable" + (sortBy === "real" ? " active" : "")} onClick={() => handleSort("real")}>
                Realizado <SortArrow col="real" />
              </th>
              <th className={"sortable" + (sortBy === "delta" ? " active" : "")} onClick={() => handleSort("delta")}>
                Δ vs. estimado <SortArrow col="delta" />
              </th>
            </tr>
          </thead>
          <tbody>
            {sortedDemands.map((d, i) => {
              const perf = PERF_BY_ID[d.id] || { tag: "dentro", label: "—", delta: 0 };
              const sav  = SAVING_BY_ID[d.id] || { est: 0, real: 0 };
              const deltaPct = sav.est ? ((sav.real - sav.est) / sav.est) * 100 : 0;
              return (
                <React.Fragment key={d.id}>
                  <tr onClick={() => setExpandedId(expandedId === d.id ? null : d.id)}>
                    <td className="rank-num">{String(i + 1).padStart(2, "0")}</td>
                    <td>
                      <div className="rank-name">{d.name}</div>
                      <div className="rank-area">{d.area}</div>
                    </td>
                    <td><span className={"perf-badge perf-" + perf.tag}>{perf.label}</span></td>
                    <td className="rank-val-muted">{fmtMoney(sav.est)}</td>
                    <td className="rank-val-hero">{fmtMoney(sav.real)}</td>
                    <td>
                      <span className={"delta-tag " + (deltaPct >= 0 ? "delta-pos" : "delta-neg")}>
                        {deltaPct >= 0 ? "+" : ""}{deltaPct.toFixed(1)}%
                      </span>
                    </td>
                  </tr>
                  {expandedId === d.id && (
                    <tr className="rank-detail-row">
                      <td colSpan="6">
                        <div className="rank-detail-inner">
                          <div className="di-item"><span className="di-l">FTE estimado</span><span className="di-v">{d.fte}</span></div>
                          <div className="di-item"><span className="di-l">Score final</span><span className="di-v">{d.score}</span></div>
                          <div className="di-item"><span className="di-l">Solicitante</span><span className="di-v" style={{ fontSize: 12 }}>{d.solicitante.split("@")[0]}</span></div>
                          <div className="di-item"><span className="di-l">SLA</span><span className="di-v"><span className={"sla sla-" + d.sla}>{d.slaLabel}</span></span></div>
                        </div>
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

Object.assign(window, { AvaliacaoScreen });
