// CuradoriaScreen — Batch B
// Visualiza demandas agrupadas em filas SLA (A urgente / B rápida / C analítica / D aguardando)
// Click expande compare-row (original × validado), com ações de curar/devolver/descartar.

const CUR_FILAS = [
  { key: "A", label: "Urgente",    sla: "SLA: 1 dia",  desc: "Decisão obrigatória" },
  { key: "B", label: "Rápida",     sla: "SLA: 2 dias", desc: "Curadoria expressa" },
  { key: "C", label: "Analítica",  sla: "SLA: 5 dias", desc: "Demanda mais discutida" },
  { key: "D", label: "Aguardando", sla: "SLA: pausado", desc: "Em espera externa" },
];

// Pendências exemplo por demanda (fake)
const PENDENCIA_POR_ID = {
  "GDT-042": ["Saving sem valor confirmado"],
  "GDT-041": ["Área não definida", "FTE faltando"],
  "GDT-039": [],
  "GDT-038": [],
  "GDT-033": ["Sem descrição"],
};

const STATE_BY_STATUS = {
  novo:        "pendente",
  avaliacao:   "parcial",
  aprovado:    "curado",
  execucao:    "curado",
  concluida:   "curado",
  rejeitado:   "descartada",
  aguardando:  "parcial",
};

function CurCard({ demand, expanded, onToggle, onCurar, onDescartar }) {
  const state = STATE_BY_STATUS[demand.status] || "pendente";
  const pend = PENDENCIA_POR_ID[demand.id] || [];

  return (
    <div className={"cur-card cur-" + state + (expanded ? " expanded" : "")}>
      <button className="cur-card-header" onClick={onToggle}>
        <span className="cur-id">{demand.id}</span>
        <div className="cur-card-meta">
          <span className="cur-nome">{demand.name}</span>
          <span className="cur-area">{demand.area}</span>
        </div>
        <div className="cur-card-right">
          <span className={"sla sla-" + demand.sla}>{demand.slaLabel}</span>
          <span className={"cur-badge cur-badge-" + state}>
            <span className="dot"></span>
            {state === "pendente" ? "Pendente" : state === "parcial" ? "Parcial" : state === "curado" ? "Curado" : "Descartada"}
          </span>
          <Icon name="chevron-down" size={14} style={{ transform: expanded ? "rotate(180deg)" : "none", transition: "transform 200ms ease", color: "var(--text-muted)" }} />
        </div>
      </button>

      {expanded && (
        <div className="cur-card-body">
          {/* Compare row */}
          <Eyebrow style={{ marginBottom: 8 }}>Compare · ticket original × validado pela curadoria</Eyebrow>
          <div className="compare-row">
            <div className="compare-col">
              <span className="compare-label">Original (ticket)</span>
              <span className="compare-val">{demand.area.toLowerCase()} - {demand.name.toLowerCase().slice(0, 32)}</span>
            </div>
            <div className="compare-col validado">
              <span className="compare-label">Validado (curadoria)</span>
              <span className="compare-val">{demand.area} · {demand.name}</span>
            </div>
          </div>

          {/* Métricas */}
          <div className="cur-metrics">
            <div className="cur-metric"><span className="ml">Saving</span><span className="mv mono">{demand.saving}</span></div>
            <div className="cur-metric"><span className="ml">FTE</span><span className="mv mono">{demand.fte}</span></div>
            <div className="cur-metric"><span className="ml">Score</span><span className="mv mono">{demand.score}</span></div>
            <div className="cur-metric"><span className="ml">Complexidade</span><span className="mv"><span className={"complexity c-" + demand.complexidade}>{demand.complexidade}</span></span></div>
          </div>

          {/* Descrição */}
          <Eyebrow style={{ marginTop: 14, marginBottom: 6 }}>Descrição</Eyebrow>
          <p className="cur-desc">{demand.descricao}</p>

          {/* Pendências */}
          {pend.length > 0 && (
            <>
              <Eyebrow style={{ marginTop: 14, marginBottom: 6 }}>Pendências de validação</Eyebrow>
              <div className="pend-list">
                {pend.map(p => <span className="pend-tag" key={p}>{p}</span>)}
              </div>
            </>
          )}

          {/* Ações */}
          {state !== "descartada" && (
            <div className="cur-actions">
              {state !== "curado" && (
                <Button variant="approve" icon="check" small onClick={() => onCurar(demand.id)} disabled={pend.length > 0}>
                  {pend.length > 0 ? `Resolver ${pend.length} pendência${pend.length > 1 ? "s" : ""}` : "Marcar como curado"}
                </Button>
              )}
              <Button variant="reject" icon="x" small onClick={() => onDescartar(demand.id)}>Descartar</Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function CuradoriaScreen({ data, onToast }) {
  const [filter, setFilter] = React.useState("todas");
  const [expandedId, setExpandedId] = React.useState(null);
  const [demands, setDemands] = React.useState(data.demands);

  const setStatusAndToast = (id, newStatus, newLabel, kind, msg) => {
    setDemands(ds => ds.map(d => d.id === id ? { ...d, status: newStatus, statusLabel: newLabel } : d));
    setExpandedId(null);
    onToast({ kind, msg });
  };

  const handleCurar     = (id) => setStatusAndToast(id, "aprovado",  "Aprovado",  "success", `${id} marcado como curado.`);
  const handleDescartar = (id) => setStatusAndToast(id, "rejeitado", "Rejeitado", "error",   `${id} descartado.`);

  // Stats no topo
  const total     = demands.length;
  const pendentes = demands.filter(d => STATE_BY_STATUS[d.status] === "pendente").length;
  const curadas   = demands.filter(d => STATE_BY_STATUS[d.status] === "curado").length;

  // Agrupar por fila (após filtros)
  const byFila = (filaKey) => demands.filter(d => {
    if (filter === "pendentes" && STATE_BY_STATUS[d.status] !== "pendente") return false;
    if (filter === "curadas"   && STATE_BY_STATUS[d.status] !== "curado") return false;
    return d.fila === filaKey;
  });

  return (
    <div className="page-main">
      <div className="page-head">
        <div className="page-title">
          <h1>Curadoria de demandas</h1>
          <p>Triagem e validação das demandas antes da decisão executiva.</p>
        </div>
      </div>

      {/* Stats bar */}
      <div className="stats-bar">
        <div className="stat-card">
          <div className="stat-value">{total}</div>
          <div className="stat-label">Total em curadoria</div>
        </div>
        <div className="stat-card left-rej">
          <div className="stat-value" style={{ color: "var(--rejeitado)" }}>{pendentes}</div>
          <div className="stat-label">Pendentes de validação</div>
        </div>
        <div className="stat-card left-apr">
          <div className="stat-value" style={{ color: "var(--aprovado)" }}>{curadas}</div>
          <div className="stat-label">Prontas para decisão</div>
        </div>
      </div>

      {/* Filtros rápidos */}
      <div className="filters">
        {[
          { id: "todas",     label: "Todas",     count: total },
          { id: "pendentes", label: "Pendentes", count: pendentes },
          { id: "curadas",   label: "Prontas",   count: curadas },
        ].map(f => (
          <button
            key={f.id}
            className={"fpill" + (filter === f.id ? " active" : "")}
            onClick={() => setFilter(f.id)}
          >
            {f.label} <span className="count">{f.count}</span>
          </button>
        ))}
      </div>

      {/* Filas agrupadas */}
      {CUR_FILAS.map(fila => {
        const items = byFila(fila.key);
        return (
          <section className={"fila-section fila-" + fila.key.toLowerCase()} key={fila.key}>
            <header className="fila-header">
              <div className="fila-header-left">
                <span className="fila-tag">{fila.key}</span>
                <span className="fila-name">{fila.label}</span>
                <span className="fila-sla">{fila.sla} · {fila.desc}</span>
              </div>
              <span className="fila-count">{items.length} {items.length === 1 ? "item" : "itens"}</span>
            </header>
            <div className="fila-body">
              {items.length === 0 ? (
                <div className="fila-empty">Nenhuma demanda nesta fila.</div>
              ) : items.map(d => (
                <CurCard
                  key={d.id}
                  demand={d}
                  expanded={expandedId === d.id}
                  onToggle={() => setExpandedId(expandedId === d.id ? null : d.id)}
                  onCurar={handleCurar}
                  onDescartar={handleDescartar}
                />
              ))}
            </div>
          </section>
        );
      })}
    </div>
  );
}

Object.assign(window, { CuradoriaScreen });
