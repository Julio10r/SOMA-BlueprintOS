// AcompanhamentoScreen — Batch D
// Tab "Acompanhamento" — lookup por ID ou e-mail + visualização da demanda com status stepper.

const STEPPER_DEFS = [
  { key: "registrada", label: "Registrada",   short: "" },
  { key: "curada",     label: "Curada",       short: "" },
  { key: "avaliacao",  label: "Em avaliação", short: "" },
  { key: "aprovada",   label: "Aprovada",     short: "" },
  { key: "execucao",   label: "Em execução",  short: "" },
  { key: "concluida",  label: "Concluída",    short: "" },
];

// Map status do dataset para passo do stepper
const STATUS_TO_STEP = {
  novo:        2,   // curada → em avaliação (current)
  avaliacao:   2,
  aprovado:    3,
  execucao:    4,
  concluida:   5,
  aguardando:  2,
  rejeitado:   3,   // current = aprovada (skipped no caso de rejeitado)
};

function StatusStepper({ currentStep, skippedSteps = [] }) {
  return (
    <div className="stepper">
      <div className="stepper-track">
        {STEPPER_DEFS.map((step, i) => {
          let state = "next";
          if (skippedSteps.includes(i))  state = "skipped";
          else if (i < currentStep)      state = "done";
          else if (i === currentStep)    state = "current";
          else if (i === currentStep + 1) state = "next";
          else state = "";

          return (
            <div className={"step " + state} key={step.key}>
              <div className="step-line"></div>
              <div className="step-dot">
                {state === "done" ? <Icon name="check" size={12} /> : i + 1}
              </div>
              <span className="step-lbl">{step.label}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function InfoCard({ title, defaultOpen = true, children }) {
  const [open, setOpen] = React.useState(defaultOpen);
  return (
    <div className={"info-card" + (open ? " open" : "")}>
      <button className="info-card-h" onClick={() => setOpen(!open)}>
        <span className="info-card-t">{title}</span>
        <Icon name="chevron-down" size={14} style={{ transform: open ? "rotate(180deg)" : "none", transition: "transform 200ms ease", color: "var(--text-muted)" }} />
      </button>
      {open && <div className="info-card-b">{children}</div>}
    </div>
  );
}

function AvaliacaoComment({ status, author, when, children }) {
  return (
    <div className={"aval-comment aval-" + status}>
      <span className="aval-meta">{status === "aprovado" ? "Aprovada" : status === "rejeitado" ? "Rejeitada" : "Em avaliação"} · {author} · {when}</span>
      {children}
    </div>
  );
}

function DemandView({ demand, onBack }) {
  const currentStep = STATUS_TO_STEP[demand.status] ?? 0;
  const skipped = demand.status === "rejeitado" ? [3, 4, 5] : [];

  return (
    <div className="acomp-view">
      <button className="back-btn" onClick={onBack}>
        <Icon name="chevron-left" size={14} /> Buscar outra demanda
      </button>

      <div className="page-head" style={{ marginBottom: 18 }}>
        <div className="page-title">
          <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 6 }}>
            <span className="cur-id">{demand.id}</span>
            <span className={"badge b-" + demand.status}>{demand.statusLabel}</span>
            <span className={"complexity c-" + demand.complexidade}>{demand.complexidade}</span>
          </div>
          <h1>{demand.name}</h1>
          <p>{demand.area} · solicitada por {demand.solicitante.split("@")[0]} em {demand.data}</p>
        </div>
      </div>

      {/* Status stepper */}
      <div className="stepper-card">
        <Eyebrow style={{ marginBottom: 16 }}>Andamento</Eyebrow>
        <StatusStepper currentStep={currentStep} skippedSteps={skipped} />
      </div>

      {/* Avaliação comment (se houver) */}
      {(demand.status === "aprovado" || demand.status === "rejeitado" || demand.status === "avaliacao") && (
        <AvaliacaoComment
          status={demand.status}
          author="bruno.afonso"
          when="15/04/2026"
        >
          {demand.status === "aprovado"
            ? `"Aprovado para sprint de maio. Priorizar acima de outras demandas do trimestre dado o saving anual projetado de ${demand.saving}."`
            : demand.status === "rejeitado"
            ? `"Já existe iniciativa equivalente no programa Visão Única (Q3). Realocar esforço."`
            : `"Falta clareza no saving estimado. Solicitando ao time da área que revise a base de cálculo antes de aprovarmos para sprint."`}
        </AvaliacaoComment>
      )}

      {/* Info cards */}
      <InfoCard title="Informações gerais" defaultOpen={true}>
        <div className="info-grid">
          <div className="info-row"><span className="il">Área</span><span className="iv">{demand.area}</span></div>
          <div className="info-row"><span className="il">Solicitante</span><span className="iv">{demand.solicitante}</span></div>
          <div className="info-row"><span className="il">Criada em</span><span className="iv">{demand.data}</span></div>
          <div className="info-row"><span className="il">SLA</span><span className="iv"><span className={"sla sla-" + demand.sla}>{demand.slaLabel}</span></span></div>
          <div className="info-row"><span className="il">FTE estimado</span><span className="iv mono">{demand.fte}</span></div>
          <div className="info-row"><span className="il">Saving anual</span><span className="iv mono" style={{ color: "var(--aprovado)" }}>{demand.saving}</span></div>
        </div>
      </InfoCard>

      <InfoCard title="Descrição" defaultOpen={true}>
        <p className="info-text">{demand.descricao}</p>
      </InfoCard>

      <InfoCard title="Score & recomendação" defaultOpen={false}>
        <div className="info-grid">
          <div className="info-row"><span className="il">Score final</span><span className="iv mono" style={{ fontSize: 22 }}>{demand.score}</span></div>
          <div className="info-row"><span className="il">Recomendação</span><span className="iv" style={{ color: demand.score >= 70 ? "var(--aprovado)" : demand.score < 50 ? "var(--rejeitado)" : "var(--avaliacao)", fontWeight: 600 }}>{demand.score >= 70 ? "APROVAR" : demand.score < 50 ? "REJEITAR" : "REVISAR"}</span></div>
          <div className="info-row"><span className="il">Nível de prioridade</span><span className="iv">{demand.nivel}</span></div>
          <div className="info-row"><span className="il">Fila</span><span className="iv mono">{demand.fila}</span></div>
        </div>
      </InfoCard>
    </div>
  );
}

function LookupForm({ data, onFound, onNotFound }) {
  const [mode, setMode] = React.useState("id");
  const [query, setQuery] = React.useState("");
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState(null);

  const submit = (e) => {
    e.preventDefault();
    setError(null);
    const q = query.trim();
    if (!q) { setError("Informe um " + (mode === "id" ? "código" : "e-mail") + "."); return; }

    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      if (mode === "id") {
        const d = data.demands.find(x => x.id.toLowerCase() === q.toLowerCase());
        if (d) onFound(d);
        else { setError(`Demanda ${q} não encontrada.`); onNotFound(); }
      } else {
        const d = data.demands.find(x => x.solicitante.toLowerCase() === q.toLowerCase());
        if (d) onFound(d);
        else { setError(`Nenhuma demanda encontrada para ${q}.`); onNotFound(); }
      }
    }, 400);
  };

  return (
    <div className="lookup-wrap">
      <form className="lookup-card" onSubmit={submit}>
        <div className="lookup-icon">
          <Icon name="search" size={18} />
        </div>
        <h2>Acompanhar demanda</h2>
        <p className="lookup-sub">Localize uma demanda registrada usando o código GDT ou o e-mail do solicitante.</p>

        <div className="lookup-tabs">
          <button type="button" className={"lookup-tab" + (mode === "id" ? " active" : "")} onClick={() => { setMode("id"); setQuery(""); setError(null); }}>Por ID</button>
          <button type="button" className={"lookup-tab" + (mode === "email" ? " active" : "")} onClick={() => { setMode("email"); setQuery(""); setError(null); }}>Por e-mail</button>
        </div>

        <div className="field-group">
          <label className="field-label">{mode === "id" ? "Código da demanda" : "E-mail do solicitante"}</label>
          <input
            className={"field-input" + (error ? " error" : "")}
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder={mode === "id" ? "GDT-000" : "nome@somagrupo.com.br"}
            autoFocus
          />
          {error && <span className="auth-error">{error}</span>}
        </div>

        <p className="lookup-hint">
          {mode === "id"
            ? "Tente: GDT-042, GDT-039, GDT-030"
            : "Tente: maria.silva@somagrupo.com.br"}
        </p>

        <Button variant="primary" icon="search" loading={loading} type="submit">Buscar</Button>
      </form>
    </div>
  );
}

function AcompanhamentoScreen({ data }) {
  const [foundDemand, setFoundDemand] = React.useState(null);

  if (foundDemand) {
    return (
      <div className="page-main">
        <DemandView demand={foundDemand} onBack={() => setFoundDemand(null)} />
      </div>
    );
  }
  return <LookupForm data={data} onFound={setFoundDemand} onNotFound={() => {}} />;
}

Object.assign(window, { AcompanhamentoScreen });
