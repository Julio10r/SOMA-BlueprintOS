type NegociacaoMock = {
  id: string;
  fornecedor: string;
  objetivo: string;
  economiaEstimada: string;
  fase: string;
};

const negociacoesMock: NegociacaoMock[] = [
  { id: "NEG-2026-014", fornecedor: "Textil Ipiranga LTDA", objetivo: "Renegociar prazo de pagamento", economiaEstimada: "R$ 8.200,00", fase: "Proposta enviada" },
  { id: "NEG-2026-013", fornecedor: "Malharia Boa Vista", objetivo: "Redução de preço por volume", economiaEstimada: "R$ 15.600,00", fase: "Em análise" },
  { id: "NEG-2026-012", fornecedor: "Embalagens Fenix", objetivo: "Revisão de contrato anual", economiaEstimada: "R$ 3.400,00", fase: "Aguardando fornecedor" }
];

/**
 * Tela demonstrativa (sem chamadas de API). O dominio de Negociacoes ainda
 * nao possui backend integrado.
 */
export function NegociacoesPage() {
  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Negociações</h1>
        <p>Recomendações e acompanhamento de negociações com fornecedores.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Em desenvolvimento:</strong> conteúdo de demonstração. A integração com o motor de recomendação de negociações está em construção.
      </div>

      <div className="supplier-card-list">
        {negociacoesMock.map((negociacao) => (
          <div className="card" key={negociacao.id}>
            <div className="card-heading">
              <div>
                <div className="section-title">{negociacao.id}</div>
                <h2>{negociacao.fornecedor}</h2>
              </div>
              <span className="badge">{negociacao.fase}</span>
            </div>
            <p>{negociacao.objetivo}</p>
            <div className="data-grid" style={{ gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
              <div className="field-readonly">
                <span>Economia estimada</span>
                <strong>{negociacao.economiaEstimada}</strong>
              </div>
              <div className="field-readonly">
                <span>Fase atual</span>
                <strong>{negociacao.fase}</strong>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
