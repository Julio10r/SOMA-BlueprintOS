import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ItemFiscalTable } from "../components/ItemFiscalTable";
import { useItensFiscais } from "../hooks/useItensFiscais";
import { statusItemFiscal, type StatusItemFiscal } from "../types/itemFiscalTypes";

/**
 * Listagem de Itens Fiscais (B3 - Bloco 3, Discovery homologado). Cadastro local do +Compras — ao
 * contrário dos cadastros de apoio dos Blocos 1/2, existe ação de criação real aqui.
 */
export function ItensFiscaisPage() {
  const navigate = useNavigate();
  const { itensFiscais, loading, error, toggleStatus } = useItensFiscais();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusItemFiscal | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const itensFiltrados = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return itensFiscais.filter((item) => {
      const combinaBusca =
        !termo ||
        item.codigo.toLowerCase().includes(termo) ||
        item.descricao.toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusItemFiscal(item) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [itensFiscais, busca, statusFiltro]);

  async function handleToggleAtivo(item: Parameters<typeof toggleStatus>[0]) {
    setToggleErro(null);
    try {
      await toggleStatus(item);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status do item fiscal.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Cadastros</div>
        <h1>Gestão de Itens Fiscais</h1>
        <p>
          Cadastro único de itens para compras — a granularidade (genérico ou específico) é decidida pela área de
          Compras. Unidade e Conta Contábil são obrigatórias e selecionadas entre os cadastros de apoio do Linx.
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Itens Fiscais</div>
            <h2>Itens fiscais cadastrados</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
            Novo item fiscal
          </button>
        </div>

        <div className="input-row">
          <label>
            Pesquisar
            <input
              type="text"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Código ou Descrição"
            />
          </label>
          <label>
            Status
            <select value={statusFiltro} onChange={(event) => setStatusFiltro(event.target.value as StatusItemFiscal | "Todos")}>
              <option value="Todos">Todos</option>
              <option value="Ativo">Ativo</option>
              <option value="Inativo">Inativo</option>
            </select>
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}
        {toggleErro && <div className="notice notice-crit">{toggleErro}</div>}

        {loading ? (
          <div className="empty-state">Carregando itens fiscais...</div>
        ) : (
          <ItemFiscalTable
            itens={itensFiltrados}
            onVisualizar={(item) => navigate(item.id)}
            onEditar={(item) => navigate(`${item.id}/editar`)}
            onToggleAtivo={handleToggleAtivo}
          />
        )}
      </section>
    </div>
  );
}
