type ConfigGroup = { titulo: string; itens: Array<{ label: string; valor: string }> };

const gruposMock: ConfigGroup[] = [
  {
    titulo: "Integração ERP",
    itens: [
      { label: "Sistema ERP", valor: "SOMA_DESENV" },
      { label: "Unidade de negócio padrão", valor: "SOMA" },
      { label: "Timeout de integração", valor: "30s" }
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
    titulo: "Notificações",
    itens: [
      { label: "Alertas de situação cadastral", valor: "Ativado" },
      { label: "Resumo diário por e-mail", valor: "Desativado" }
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
        <h1>Configurações</h1>
        <p>Parâmetros de integração e notificações do portal.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Em desenvolvimento:</strong> valores ilustrativos. A edição destes parâmetros ainda não está disponível nesta tela.
      </div>

      {gruposMock.map((grupo) => (
        <section className="card" key={grupo.titulo}>
          <div className="card-heading">
            <div>
              <div className="section-title">Configuração</div>
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
