/**
 * Controle de paginação server-side da listagem de Fornecedores. Segue o mesmo padrão visual dos
 * demais controles de ação (`btn btn-secondary`) já usados no módulo — não existe um componente de
 * paginação pronto no design system (`resources/design-system/preview/`), então este foi construído
 * seguindo a mesma linguagem visual.
 */
export function FornecedorPagination({
  page,
  pageSize,
  totalCount,
  onPageChange
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className="pagination-bar">
      <span className="pagination-summary">
        Exibindo {from}–{to} de {totalCount} fornecedores
      </span>
      <div className="pagination-controls">
        <button
          type="button"
          className="btn btn-secondary"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Anterior
        </button>
        <span className="pagination-page">
          Página {page} de {totalPages}
        </span>
        <button
          type="button"
          className="btn btn-secondary"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Próxima
        </button>
      </div>
    </div>
  );
}
