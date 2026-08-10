import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { FilialTable } from "../components/FilialTable";
import { useFiliais } from "../hooks/useFiliais";
import { statusFilial, type StatusFilial } from "../types/filialTypes";

/**
 * Listagem de Filiais (Gestao de Filiais, ADR-0020 item 3). Fundacao
 * visual da Sprint O1.3.3: dados mockados em memoria
 * (services/filiaisMockApi.ts), sem integracao com API real e sem
 * escrita no ERP.
 *
 * Nao existe acao de criacao: Filial e um dado mestre integrado do ERP,
 * nunca criado pelo +Compras (por isso nao ha botao "Nova Filial" nesta
 * pagina, ao contrario de Perfis/Usuarios).
 */
export function FiliaisPage() {
  const navigate = useNavigate();
  const { filiais, loading, error, toggleAtivo } = useFiliais();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusFilial | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const filiaisFiltradas = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return filiais.filter((filial) => {
      const combinaBusca =
        !termo ||
        filial.codigoCliFor.toLowerCase().includes(termo) ||
        filial.nomeCliFor.toLowerCase().includes(termo) ||
        (filial.descricaoMaisCompras ?? "").toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusFilial(filial) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [filiais, busca, statusFiltro]);

  async function handleToggleAtivo(filial: Parameters<typeof toggleAtivo>[0]) {
    setToggleErro(null);
    try {
      await toggleAtivo(filial);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status da filial no +Compras.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Gestao de Filiais</h1>
        <p>
          Filiais sao dados mestres do ERP. O +Compras nao cria nem altera dados mestres do ERP: apenas administra
          metadados locais (descricao +Compras e status de uso no +Compras).
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Filiais</div>
            <h2>Filiais integradas do ERP</h2>
          </div>
        </div>

        <div className="input-row">
          <label>
            Pesquisar
            <input
              type="text"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Codigo CliFor, Nome CliFor ou Descricao +Compras"
            />
          </label>
          <label>
            Status no +Compras
            <select value={statusFiltro} onChange={(event) => setStatusFiltro(event.target.value as StatusFilial | "Todos")}>
              <option value="Todos">Todos</option>
              <option value="Ativo">Ativo</option>
              <option value="Inativo">Inativo</option>
            </select>
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}
        {toggleErro && <div className="notice notice-crit">{toggleErro}</div>}

        {loading ? (
          <div className="empty-state">Carregando filiais...</div>
        ) : (
          <FilialTable
            filiais={filiaisFiltradas}
            onVisualizar={(filial) => navigate(filial.id)}
            onEditar={(filial) => navigate(`${filial.id}/editar`)}
            onToggleAtivo={handleToggleAtivo}
          />
        )}
      </section>
    </div>
  );
}
