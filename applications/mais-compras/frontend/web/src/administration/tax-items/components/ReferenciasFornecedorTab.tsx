import { FormEvent, useState } from "react";
import type { OpcaoFornecedor } from "../hooks/useFornecedoresAtivos";
import type { ItemFiscalReferenciaFornecedor } from "../types/itemFiscalReferenciaFornecedorTypes";

/**
 * Aba "Referências por Fornecedor" do formulário de Item Fiscal (B3 - Bloco 4, Discovery homologado).
 * DE/PARA entre o código interno do Item Fiscal e o código que cada fornecedor usa para o mesmo item -
 * um Item Fiscal pode ter referências de múltiplos fornecedores, e cada fornecedor tem no máximo uma
 * referência por Item Fiscal (estrutura comprovada em Linx).
 *
 * `fornecedorId` é imutável após incluída a referência - editar só permite corrigir o código; para trocar
 * de fornecedor, remova e inclua novamente. Remoção é FÍSICA, sem inativação (comprovado em Linx:
 * `ITEM_FISCAL_REF_FORNECEDOR` não tem coluna de status).
 */
export function ReferenciasFornecedorTab({ referencias, opcoesFornecedor, loading, error, onIncluir, onAtualizar, onRemover }: {
  referencias: ItemFiscalReferenciaFornecedor[];
  opcoesFornecedor: { opcoes: OpcaoFornecedor[]; loading: boolean; error: string | null };
  loading: boolean;
  error: string | null;
  onIncluir: (fornecedorId: string, codigoItemFornecedor: string) => Promise<void>;
  onAtualizar: (id: string, codigoItemFornecedor: string) => Promise<void>;
  onRemover: (id: string) => Promise<void>;
}) {
  const [fornecedorId, setFornecedorId] = useState("");
  const [codigo, setCodigo] = useState("");
  const [salvando, setSalvando] = useState(false);
  const [erroAcao, setErroAcao] = useState<string | null>(null);
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [codigoEdicao, setCodigoEdicao] = useState("");

  const fornecedoresJaReferenciados = new Set(referencias.map((r) => r.fornecedorId));
  const opcoesDisponiveis = opcoesFornecedor.opcoes.filter((o) => !fornecedoresJaReferenciados.has(o.id));

  async function handleIncluir(event: FormEvent) {
    event.preventDefault();
    setErroAcao(null);
    setSalvando(true);
    try {
      await onIncluir(fornecedorId, codigo);
      setFornecedorId("");
      setCodigo("");
    } catch (e) {
      setErroAcao(e instanceof Error ? e.message : "Falha ao incluir a referência do fornecedor.");
    } finally {
      setSalvando(false);
    }
  }

  function iniciarEdicao(referencia: ItemFiscalReferenciaFornecedor) {
    setEditandoId(referencia.id);
    setCodigoEdicao(referencia.codigoItemFornecedor);
    setErroAcao(null);
  }

  async function handleSalvarEdicao(id: string) {
    setErroAcao(null);
    setSalvando(true);
    try {
      await onAtualizar(id, codigoEdicao);
      setEditandoId(null);
    } catch (e) {
      setErroAcao(e instanceof Error ? e.message : "Falha ao salvar a referência do fornecedor.");
    } finally {
      setSalvando(false);
    }
  }

  async function handleRemover(id: string) {
    setErroAcao(null);
    setSalvando(true);
    try {
      await onRemover(id);
    } catch (e) {
      setErroAcao(e instanceof Error ? e.message : "Falha ao remover a referência do fornecedor.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="data-block">
      <div className="notice notice-warn">
        A referência serve para identificar, no futuro processamento de XML de NF-e/NFS-e, qual Item Fiscal
        corresponde ao código que cada fornecedor usa. O processamento de XML não é implementado nesta etapa.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}
      {erroAcao && <div className="notice notice-crit">{erroAcao}</div>}
      {opcoesFornecedor.error && <div className="notice notice-crit">{opcoesFornecedor.error}</div>}

      {loading ? (
        <div className="empty-state">Carregando referências...</div>
      ) : referencias.length === 0 ? (
        <div className="empty-state">Nenhuma referência de fornecedor cadastrada ainda.</div>
      ) : (
        <div className="table-scroll">
        <table className="divergence-table">
          <thead>
            <tr>
              <th>Fornecedor</th>
              <th>Código no fornecedor</th>
              <th aria-label="Ações" />
            </tr>
          </thead>
          <tbody>
            {referencias.map((referencia) => (
              <tr key={referencia.id}>
                <td>{referencia.fornecedorNome}</td>
                <td>
                  {editandoId === referencia.id ? (
                    <input
                      aria-label={`Código no fornecedor ${referencia.fornecedorNome}`}
                      value={codigoEdicao}
                      onChange={(event) => setCodigoEdicao(event.target.value)}
                      disabled={salvando}
                    />
                  ) : (
                    referencia.codigoItemFornecedor
                  )}
                </td>
                <td className="actions">
                  {editandoId === referencia.id ? (
                    <>
                      <button type="button" className="btn btn-secondary" onClick={() => setEditandoId(null)} disabled={salvando}>
                        Cancelar
                      </button>
                      <button type="button" className="btn btn-primary" onClick={() => handleSalvarEdicao(referencia.id)} disabled={salvando}>
                        Salvar
                      </button>
                    </>
                  ) : (
                    <>
                      <button type="button" className="btn btn-secondary" onClick={() => iniciarEdicao(referencia)} disabled={salvando}>
                        Editar
                      </button>
                      <button type="button" className="btn btn-reject" onClick={() => handleRemover(referencia.id)} disabled={salvando}>
                        Remover
                      </button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      )}

      <form className="data-block" onSubmit={handleIncluir}>
        <div className="section-title">Incluir referência</div>
        <div className="data-grid-3">
          <label>
            Fornecedor
            <select
              value={fornecedorId}
              onChange={(event) => setFornecedorId(event.target.value)}
              required
              disabled={salvando || opcoesFornecedor.loading}
            >
              <option value="" disabled>
                Selecione um fornecedor
              </option>
              {opcoesDisponiveis.map((opcao) => (
                <option key={opcao.id} value={opcao.id}>
                  {opcao.nome}
                </option>
              ))}
            </select>
          </label>

          <label>
            Código no fornecedor
            <input value={codigo} onChange={(event) => setCodigo(event.target.value)} required disabled={salvando} />
          </label>
        </div>

        <div className="actions">
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : "Incluir referência"}
          </button>
        </div>
      </form>
    </div>
  );
}
