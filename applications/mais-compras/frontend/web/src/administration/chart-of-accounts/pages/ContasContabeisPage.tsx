import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContaContabilTable } from "../components/ContaContabilTable";
import { useContasContabeis } from "../hooks/useContasContabeis";
import { statusContaContabilEfetivo, type StatusContaContabil } from "../types/contaContabilTypes";

/**
 * Listagem de Contas Contabeis (B3 - Bloco 1, Discovery homologado). Integracao real com o ERP
 * `SOMA_DESENV`, via `services/contasContabeisApi.ts`: leitura combinada com os metadados locais do
 * +Compras, sem escrita no ERP.
 *
 * Nao existe acao de criacao: Conta Contabil e um dado de apoio integrado do ERP, nunca criado pelo
 * +Compras.
 */
export function ContasContabeisPage() {
  const navigate = useNavigate();
  const { contasContabeis, loading, error, toggleAtivo } = useContasContabeis();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusContaContabil | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const contasFiltradas = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return contasContabeis.filter((conta) => {
      const combinaBusca =
        !termo ||
        conta.codigoErp.toLowerCase().includes(termo) ||
        conta.descricaoErp.toLowerCase().includes(termo) ||
        (conta.descricaoMaisCompras ?? "").toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusContaContabilEfetivo(conta) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [contasContabeis, busca, statusFiltro]);

  async function handleToggleAtivo(conta: Parameters<typeof toggleAtivo>[0]) {
    setToggleErro(null);
    try {
      await toggleAtivo(conta);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status da conta contábil no +Compras.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Gestão de Contas Contábeis</h1>
        <p>
          Contas Contábeis são cadastro de apoio do ERP (plano de contas do Linx). O +Compras não cria nem altera
          o cadastro no ERP: apenas administra metadados locais (descrição +Compras e status de uso no +Compras).
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Contas Contábeis</div>
            <h2>Contas contábeis integradas do ERP</h2>
          </div>
        </div>

        <div className="input-row">
          <label>
            Pesquisar
            <input
              type="text"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Código, Descrição ERP ou Descrição +Compras"
            />
          </label>
          <label>
            Status no +Compras
            <select value={statusFiltro} onChange={(event) => setStatusFiltro(event.target.value as StatusContaContabil | "Todos")}>
              <option value="Todos">Todos</option>
              <option value="Ativo">Ativo</option>
              <option value="Inativo">Inativo</option>
            </select>
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}
        {toggleErro && <div className="notice notice-crit">{toggleErro}</div>}

        {loading ? (
          <div className="empty-state">Carregando contas contábeis...</div>
        ) : (
          <ContaContabilTable
            contas={contasFiltradas}
            onVisualizar={(conta) => navigate(conta.id)}
            onEditar={(conta) => navigate(`${conta.id}/editar`)}
            onToggleAtivo={handleToggleAtivo}
          />
        )}
      </section>
    </div>
  );
}
