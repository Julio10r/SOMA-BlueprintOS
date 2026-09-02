/**
 * Ponto de entrada de "+ Novo fornecedor": oferece dois caminhos explícitos — consultar por CNPJ
 * (reaproveita o fluxo existente de CadastroFornecedor) ou preencher manualmente (sem depender de uma
 * consulta prévia). Implementado como estado de painel na própria listagem (não como rota separada),
 * mais simples dado que não há necessidade de deep-link para este passo intermediário de escolha.
 */
export function NovoFornecedorEntryModal({
  onSelectCnpj,
  onSelectManual,
  onCancel
}: {
  onSelectCnpj: () => void;
  onSelectManual: () => void;
  onCancel: () => void;
}) {
  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card card">
        <h2>Novo fornecedor</h2>
        <p>Como você quer cadastrar este fornecedor?</p>
        {/* Gate de homologação (2026-09-01): as duas opções têm a mesma importância — lado a
            lado, mesmo estilo de botão (btn-secondary, identidade preto/branco) — nenhuma delas
            é o caminho "padrão" sobre a outra. */}
        <div className="actions-choice">
          <button type="button" className="btn btn-secondary" onClick={onSelectCnpj}>
            Consultar por CNPJ
          </button>
          <button type="button" className="btn btn-secondary" onClick={onSelectManual}>
            Preencher manualmente
          </button>
        </div>
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            Cancelar
          </button>
        </div>
      </div>
    </div>
  );
}
