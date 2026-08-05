type ConfigGroup = { titulo: string; itens: Array<{ label: string; valor: string }> };

const gruposMock: ConfigGroup[] = [
  {
    titulo: "Integracao ERP",
    itens: [
      { label: "Sistema ERP", valor: "SOMA_DESENV" },
      { label: "Unidade de negocio padrao", valor: "SOMA" },
      { label: "Timeout de integracao", valor: "30s" }
    ]
  },
  {
    titulo: "Consulta de CNPJ",
    itens: [
      { label: "Provedor", valor: "BrasilAPI" },
      { label: "Timeout de consulta", valor: "10s" }
    ]
  },
  {
    titulo: "Notificacoes",
    itens: [
      { label: "Alertas de situacao cadastral", valor: "Ativado" },
      { label: "Resumo diario por e-mail", valor: "Desativado" }
    ]
  }
];

/**
 * Tela demonstrativa (sem chamadas de API). Reflete parametros que hoje
 * vivem em appsettings.json do backend, apenas para contexto visual.
 */
export function ConfiguracoesPage() {
  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Configuracoes</h1>
        <p>Parametros de integracao e notificacoes do portal.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Em desenvolvimento:</strong> valores ilustrativos. A edicao destes parametros ainda nao esta disponivel nesta tela.
      </div>

      {gruposMock.map((grupo) => (
        <section className="card" key={grupo.titulo}>
          <div className="card-heading">
            <div>
              <div className="section-title">Configuracao</div>
              <h2>{grupo.titulo}</h2>
            </div>
          </div>
          <div className="data-grid">
            {grupo.itens.map((item) => (
              <div className="field-readonly" key={item.label}>
                <span>{item.label}</span>
                <strong>{item.valor}</strong>
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
