type AgenteMock = { nome: string; papel: string; descricao: string };

const agentesMock: AgenteMock[] = [
  { nome: "Agente de Triagem de CNPJ", papel: "Enriquecimento cadastral", descricao: "Sugere correções cadastrais a partir de fontes externas, sempre com revisão humana antes de gravar." },
  { nome: "Agente de Recomendação de Negociação", papel: "Negociações", descricao: "Aponta oportunidades de renegociação com base em histórico de compras e sazonalidade." },
  { nome: "Agente de Monitoramento de Risco", papel: "Compliance", descricao: "Observa mudanças de situação cadastral e sinaliza fornecedores em risco." }
];

/**
 * Visao futura do modulo Agentes IA. Tela demonstrativa, sem chamadas de
 * API e sem estrutura funcional: apenas contexto visual, conforme
 * docs/product/PortalMapa.md (estado "Planejado").
 */
export function AgentesIAPage() {
  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Agentes IA</h1>
        <p>Visão futura: agentes de inteligência artificial aplicados ao ciclo de compras.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Visão futura:</strong> este módulo ainda não possui Work Order aprovada nem estrutura funcional. O conteúdo abaixo é apenas ilustrativo.
      </div>

      <div className="supplier-card-list">
        {agentesMock.map((agente) => (
          <div className="card" key={agente.nome}>
            <div className="card-heading">
              <div>
                <div className="section-title">{agente.papel}</div>
                <h2>{agente.nome}</h2>
              </div>
              <span className="badge">Planejado</span>
            </div>
            <p>{agente.descricao}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
