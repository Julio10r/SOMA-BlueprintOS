type KpiMock = { label: string; value: string; trend: string };
type BarMock = { label: string; percent: number };

const kpisMock: KpiMock[] = [
  { label: "Economia acumulada (ano)", value: "R$ 482.900,00", trend: "+12% vs. ano anterior" },
  { label: "Tempo médio de cotação", value: "4,2 dias", trend: "-0,8 dia vs. mês anterior" },
  { label: "Fornecedores ativos", value: "186", trend: "+9 no trimestre" },
  { label: "SLA de aprovação", value: "92%", trend: "+3pp vs. meta" }
];

const categoriasMock: BarMock[] = [
  { label: "Tecidos", percent: 78 },
  { label: "Aviamentos", percent: 54 },
  { label: "Malhas", percent: 63 },
  { label: "Embalagens", percent: 41 }
];

/**
 * Tela demonstrativa (sem chamadas de API): indicadores visuais estaticos
 * ate a entrega do dominio de Indicadores.
 */
export function IndicadoresPage() {
  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Indicadores</h1>
        <p>Visão consolidada de desempenho de compras.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Em desenvolvimento:</strong> indicadores ilustrativos. Os valores não refletem dados reais até a integração do domínio.
      </div>

      <section className="kpi-grid">
        {kpisMock.map((kpi) => (
          <div className="card kpi-card" key={kpi.label}>
            <div className="section-title">{kpi.label}</div>
            <div className="mono-kpi kpi-value">{kpi.value}</div>
            <p className="caption">{kpi.trend}</p>
          </div>
        ))}
      </section>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Volume</div>
            <h2>Participação por categoria</h2>
          </div>
        </div>
        <div className="bar-chart">
          {categoriasMock.map((item) => (
            <div className="bar-row" key={item.label}>
              <span className="bar-label">{item.label}</span>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: `${item.percent}%` }} />
              </div>
              <span className="bar-value mono">{item.percent}%</span>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
