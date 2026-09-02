import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { UnidadeMedidaTable } from "../components/UnidadeMedidaTable";
import { useUnidadesMedida } from "../hooks/useUnidadesMedida";
import { statusUnidadeMedida, type StatusUnidadeMedida } from "../types/unidadeMedidaTypes";

/**
 * Listagem de Unidades de Medida (B3 - Bloco 2, Discovery homologado). Integracao real com o ERP
 * `SOMA_DESENV`, via `services/unidadesMedidaApi.ts`: leitura combinada com os metadados locais do
 * +Compras, sem escrita no ERP.
 *
 * Nao existe acao de criacao: Unidade de Medida e um dado de apoio integrado do ERP, nunca criado pelo
 * +Compras.
 */
export function UnidadesMedidaPage() {
  const navigate = useNavigate();
  const { unidadesMedida, loading, error, toggleAtivo } = useUnidadesMedida();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusUnidadeMedida | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const unidadesFiltradas = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return unidadesMedida.filter((unidade) => {
      const combinaBusca =
        !termo ||
        unidade.codigoErp.toLowerCase().includes(termo) ||
        unidade.descricaoErp.toLowerCase().includes(termo) ||
        (unidade.descricaoMaisCompras ?? "").toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusUnidadeMedida(unidade) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [unidadesMedida, busca, statusFiltro]);

  async function handleToggleAtivo(unidade: Parameters<typeof toggleAtivo>[0]) {
    setToggleErro(null);
    try {
      await toggleAtivo(unidade);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status da unidade de medida no +Compras.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Gestão de Unidades de Medida</h1>
        <p>
          Unidades de Medida são cadastro de apoio do ERP. O +Compras não cria nem altera o cadastro no ERP: apenas
          administra metadados locais (descrição +Compras e status de uso no +Compras).
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Unidades de Medida</div>
            <h2>Unidades de medida integradas do ERP</h2>
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
            <select value={statusFiltro} onChange={(event) => setStatusFiltro(event.target.value as StatusUnidadeMedida | "Todos")}>
              <option value="Todos">Todos</option>
              <option value="Ativo">Ativo</option>
              <option value="Inativo">Inativo</option>
            </select>
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}
        {toggleErro && <div className="notice notice-crit">{toggleErro}</div>}

        {loading ? (
          <div className="empty-state">Carregando unidades de medida...</div>
        ) : (
          <UnidadeMedidaTable
            unidades={unidadesFiltradas}
            onVisualizar={(unidade) => navigate(unidade.id)}
            onEditar={(unidade) => navigate(`${unidade.id}/editar`)}
            onToggleAtivo={handleToggleAtivo}
          />
        )}
      </section>
    </div>
  );
}
