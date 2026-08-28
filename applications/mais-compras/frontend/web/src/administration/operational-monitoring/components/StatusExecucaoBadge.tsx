/** Badge de status de uma execucao de sincronizacao, reaproveitando as classes `.status-*` ja existentes
 * (`styles.css`) — nenhum Design System novo. */
export function StatusExecucaoBadge({ status }: { status: string }) {
  const className = status === "Sucesso" ? "status status-ativo" : status === "Erro" ? "status status-inativo" : "status badge";
  return <span className={className}>{status}</span>;
}
