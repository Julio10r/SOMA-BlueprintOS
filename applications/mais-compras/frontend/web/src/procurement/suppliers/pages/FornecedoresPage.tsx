import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { CadastroFornecedor } from "../components/CadastroFornecedor";
import { ConfirmToggleAtivoFornecedorModal } from "../components/ConfirmToggleAtivoFornecedorModal";
import { FornecedorPagination } from "../components/FornecedorPagination";
import { FornecedorTable } from "../components/FornecedorTable";
import { manualFornecedorDraftInicial, ManualFornecedorForm } from "../components/ManualFornecedorForm";
import { NovoFornecedorEntryModal } from "../components/NovoFornecedorEntryModal";
import {
  alterarStatusFornecedor,
  createFornecedorManual,
  searchFornecedoresPaginado
} from "../services/supplierEnrichmentApi";
import type { Fornecedor, FornecedorStatusFiltro, ManualFornecedorDraft } from "../types/linxSupplierContract";

const PAGE_SIZE = 20;
/** Debounce da busca por nome/CNPJ: evita disparar uma requisição por tecla digitada. */
const SEARCH_DEBOUNCE_MS = 320;

type PainelNovoFornecedor = "escolha" | "cnpj" | "manual" | null;

/**
 * Listagem/browse de Fornecedores (redesenho O1.x): busca por CNPJ/nome, filtro de status,
 * paginação server-side. Substitui a versão anterior, que renderizava diretamente o wizard de
 * consulta de CNPJ como única tela — esse wizard (CadastroFornecedor) agora é um dos dois caminhos
 * de "+ Novo fornecedor", acessível como painel, não mais como a tela inteira.
 *
 * Estado de busca/filtro/página vive na URL (useSearchParams) para que navegar para o detalhe e
 * voltar restaure o contexto da listagem.
 */
export function FornecedoresPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get("search") ?? "";
  const status = (searchParams.get("status") as FornecedorStatusFiltro | null) ?? "Todos";
  const page = Number(searchParams.get("page") ?? "1") || 1;

  const [fornecedores, setFornecedores] = useState<Fornecedor[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Valor digitado no campo de busca — refletido na UI imediatamente. Só é propagado para a URL
  // (e, portanto, para a requisição real) após SEARCH_DEBOUNCE_MS sem novas teclas, evitando um
  // GET por tecla digitada (ex: "amazon" disparando 6 requisições).
  const [searchInput, setSearchInput] = useState(q);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    setSearchInput(q);
  }, [q]);

  useEffect(() => {
    if (searchInput === q) return;
    const timeoutId = window.setTimeout(() => {
      updateParams({ search: searchInput });
    }, SEARCH_DEBOUNCE_MS);
    return () => window.clearTimeout(timeoutId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchInput]);

  const [painelNovo, setPainelNovo] = useState<PainelNovoFornecedor>(null);
  const [manualDraft, setManualDraft] = useState<ManualFornecedorDraft>(manualFornecedorDraftInicial);
  const [salvandoManual, setSalvandoManual] = useState(false);
  const [erroManual, setErroManual] = useState<string | null>(null);

  const [fornecedorParaAlternar, setFornecedorParaAlternar] = useState<Fornecedor | null>(null);
  const [alternandoStatus, setAlternandoStatus] = useState(false);
  const [erroToggle, setErroToggle] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    // Aborta qualquer requisição anterior ainda em voo: se "am" ainda não respondeu quando
    // "amazon" é digitado, a resposta atrasada de "am" nunca deve sobrescrever o resultado
    // correto de "amazon".
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError(null);
    try {
      const resultado = await searchFornecedoresPaginado({ q, status, page, pageSize: PAGE_SIZE }, controller.signal);
      if (controller.signal.aborted) return;
      setFornecedores(resultado.items);
      setTotalCount(resultado.totalCount);
    } catch (err) {
      if (controller.signal.aborted || (err instanceof DOMException && err.name === "AbortError")) return;
      setError(err instanceof Error ? err.message : "Falha ao carregar fornecedores.");
      setFornecedores([]);
      setTotalCount(0);
    } finally {
      if (!controller.signal.aborted) setLoading(false);
    }
  }, [q, status, page]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  function updateParams(next: Partial<{ search: string; status: string; page: string }>) {
    const params = new URLSearchParams(searchParams);
    Object.entries(next).forEach(([key, value]) => {
      if (!value || value === "Todos") params.delete(key);
      else params.set(key, value);
    });
    if (!("page" in next)) params.delete("page");
    setSearchParams(params);
  }

  async function confirmarToggleAtivo() {
    if (!fornecedorParaAlternar) return;
    setAlternandoStatus(true);
    setErroToggle(null);
    try {
      await alterarStatusFornecedor(fornecedorParaAlternar.id, fornecedorParaAlternar.status === "Inativo");
      setFornecedorParaAlternar(null);
      await carregar();
    } catch (err) {
      setErroToggle(err instanceof Error ? err.message : "Falha ao alterar o status do fornecedor.");
    } finally {
      setAlternandoStatus(false);
    }
  }

  async function handleCadastrarManual(draft: ManualFornecedorDraft) {
    setSalvandoManual(true);
    setErroManual(null);
    try {
      await createFornecedorManual(draft);
      setPainelNovo(null);
      setManualDraft(manualFornecedorDraftInicial);
      await carregar();
    } catch (err) {
      setErroManual(err instanceof Error ? err.message : "Falha ao cadastrar fornecedor.");
    } finally {
      setSalvandoManual(false);
    }
  }

  const querystring = searchParams.toString();
  const toQueryString = querystring ? `?${querystring}` : "";

  return (
    <main className="supplier-page">
      <div className="page-stack">
        <header className="page-header">
          <h1>Fornecedores</h1>
          <p>Consulte, cadastre e gerencie o status dos fornecedores do +Compras.</p>
        </header>

        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Fornecedores</div>
              <h2>Fornecedores cadastrados</h2>
            </div>
            <button type="button" className="btn btn-primary" onClick={() => setPainelNovo("escolha")}>
              + Novo fornecedor
            </button>
          </div>

          <div className="input-row">
            <label htmlFor="fornecedores-pesquisa">
              Pesquisar
              <input
                id="fornecedores-pesquisa"
                name="pesquisa"
                type="text"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Buscar por CNPJ ou nome..."
              />
            </label>
            <label htmlFor="fornecedores-status">
              Status
              <select
                id="fornecedores-status"
                name="status"
                value={status}
                onChange={(event) => updateParams({ status: event.target.value })}
              >
                <option value="Todos">Todos</option>
                <option value="Ativo">Ativos</option>
                <option value="Inativo">Inativos</option>
              </select>
            </label>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state" role="status">Carregando fornecedores...</div>
          ) : fornecedores.length === 0 ? (
            <div className="empty-state">
              <p>Nenhum fornecedor encontrado.</p>
              <div className="actions">
                {(q || status !== "Todos") && (
                  <button type="button" className="btn btn-secondary" onClick={() => setSearchParams({})}>
                    Limpar busca
                  </button>
                )}
                <button type="button" className="btn btn-primary" onClick={() => setPainelNovo("escolha")}>
                  + Novo fornecedor
                </button>
              </div>
            </div>
          ) : (
            <>
              <FornecedorTable
                fornecedores={fornecedores}
                onToggleAtivo={(fornecedor) => {
                  setErroToggle(null);
                  setFornecedorParaAlternar(fornecedor);
                }}
                toQueryString={toQueryString}
              />
              <FornecedorPagination
                page={page}
                pageSize={PAGE_SIZE}
                totalCount={totalCount}
                onPageChange={(nextPage) => updateParams({ page: String(nextPage) })}
              />
            </>
          )}
        </section>
      </div>

      {painelNovo === "escolha" && (
        <NovoFornecedorEntryModal
          onSelectCnpj={() => setPainelNovo("cnpj")}
          onSelectManual={() => setPainelNovo("manual")}
          onCancel={() => setPainelNovo(null)}
        />
      )}

      {painelNovo === "cnpj" && (
        <div className="modal-overlay" role="dialog" aria-modal="true">
          <div className="modal-card modal-card-wide card">
            <div className="card-heading">
              <h2>Consultar por CNPJ</h2>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => {
                  setPainelNovo(null);
                  carregar();
                }}
              >
                Fechar
              </button>
            </div>
            <CadastroFornecedor />
          </div>
        </div>
      )}

      {painelNovo === "manual" && (
        <div className="modal-overlay" role="dialog" aria-modal="true">
          <div className="modal-card modal-card-wide">
            <ManualFornecedorForm
              draft={manualDraft}
              onDraftChange={setManualDraft}
              onSubmit={handleCadastrarManual}
              onCancel={() => {
                setPainelNovo(null);
                setErroManual(null);
                setManualDraft(manualFornecedorDraftInicial);
              }}
              loading={salvandoManual}
              error={erroManual}
            />
          </div>
        </div>
      )}

      {fornecedorParaAlternar && (
        <ConfirmToggleAtivoFornecedorModal
          fornecedor={fornecedorParaAlternar}
          ativando={fornecedorParaAlternar.status === "Inativo"}
          error={erroToggle}
          loading={alternandoStatus}
          onConfirm={confirmarToggleAtivo}
          onCancel={() => setFornecedorParaAlternar(null)}
        />
      )}
    </main>
  );
}
