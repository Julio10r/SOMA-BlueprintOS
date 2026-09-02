import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../../auth/hooks/useAuth";
import { PERMISSOES } from "../../../auth/types/authTypes";
import { FornecedorPagination } from "../components/FornecedorPagination";
import { FornecedorTable } from "../components/FornecedorTable";
import { manualFornecedorDraftInicial, ManualFornecedorForm } from "../components/ManualFornecedorForm";
import { createFornecedorManual, FornecedorJaExisteNoErpError, searchFornecedoresPaginado } from "../services/supplierEnrichmentApi";
import type { Fornecedor, FornecedorStatusFiltro, ManualFornecedorDraft } from "../types/linxSupplierContract";

const PAGE_SIZE = 20;
/** Debounce da busca por nome/CNPJ: evita disparar uma requisição por tecla digitada. */
const SEARCH_DEBOUNCE_MS = 320;

/**
 * Gate de homologação (2026-09-01): "+ Novo fornecedor" leva direto ao formulário único de
 * cadastro (sem etapa intermediária de escolha entre "Consultar por CNPJ"/"Preencher
 * manualmente") — a consulta de CNPJ agora acontece dentro do próprio formulário
 * (ManualFornecedorForm), com confirmação ao terminar de digitar o CNPJ, no mesmo padrão do
 * Visual Linx (achado 1, docs/audits/Discovery-Fornecedor-Tela-001016G1.md: "Deseja consultar
 * online os dados cadastrais deste CNPJ?"). O wizard anterior (CadastroFornecedor + Review de
 * divergências) deixou de ser o ponto de entrada de novo fornecedor; o componente permanece no
 * código (não deletado) caso a capacidade de revisão de divergências seja retomada depois.
 */
type PainelNovoFornecedor = "manual" | null;

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
  const navigate = useNavigate();
  const { usuario } = useAuth();
  // Gate de homologação (2026-09-01): "+ Novo fornecedor" só aparece para quem tem a permissão
  // Fornecedor.Criar — mesmo padrão de gate de permissão já usado na navegação (AppShell.tsx).
  const permissoesEfetivas = (usuario?.permissoes ?? []).map((codigo) => codigo.toLowerCase());
  const podeCriarFornecedor = permissoesEfetivas.includes(PERMISSOES.fornecedorCriar.toLowerCase());

  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get("search") ?? "";
  // Gate de homologação (2026-09-01): a tela deve abrir filtrando Ativos por padrão — sem
  // parâmetro "status" na URL, assume-se "Ativo", nunca "Todos". O usuário continua podendo
  // trocar para Todos/Inativo pelo próprio filtro.
  const status = (searchParams.get("status") as FornecedorStatusFiltro | null) ?? "Ativo";
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
  // Gate de homologação (2026-09-01): CNPJ/CPF já existe como Fornecedor no Linx — aviso com "OK"
  // antes de abrir o detalhe do fornecedor existente (nunca duplicar).
  const [fornecedorJaExistente, setFornecedorJaExistente] = useState<{ mensagem: string; fornecedorId: string } | null>(null);

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

  async function handleCadastrarManual(draft: ManualFornecedorDraft) {
    setSalvandoManual(true);
    setErroManual(null);
    try {
      await createFornecedorManual(draft);
      setPainelNovo(null);
      setManualDraft(manualFornecedorDraftInicial);
      await carregar();
    } catch (err) {
      if (err instanceof FornecedorJaExisteNoErpError) {
        // Nunca duplicar: fecha o formulário de cadastro e mostra o aviso — "OK" abre o detalhe do
        // fornecedor já existente (importado do ERP nesta mesma operação pelo backend).
        setPainelNovo(null);
        setManualDraft(manualFornecedorDraftInicial);
        setFornecedorJaExistente({ mensagem: err.message, fornecedorId: err.fornecedorId });
        return;
      }
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
            {podeCriarFornecedor && (
              <button type="button" className="btn btn-primary" onClick={() => setPainelNovo("manual")}>
                + Novo fornecedor
              </button>
            )}
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
                {podeCriarFornecedor && (
                  <button type="button" className="btn btn-primary" onClick={() => setPainelNovo("manual")}>
                    + Novo fornecedor
                  </button>
                )}
              </div>
            </div>
          ) : (
            <>
              <FornecedorTable fornecedores={fornecedores} toQueryString={toQueryString} />
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
              title="Novo fornecedor"
              subtitle="Preencha as informações para cadastrar um novo fornecedor."
            />
          </div>
        </div>
      )}

      {fornecedorJaExistente && (
        <div className="modal-overlay" role="dialog" aria-modal="true">
          <div className="modal-card card">
            <h2>Fornecedor já cadastrado</h2>
            <p>{fornecedorJaExistente.mensagem}</p>
            <div className="actions">
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => {
                  const fornecedorId = fornecedorJaExistente.fornecedorId;
                  setFornecedorJaExistente(null);
                  navigate(`/fornecedores/${fornecedorId}${toQueryString}`);
                }}
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
