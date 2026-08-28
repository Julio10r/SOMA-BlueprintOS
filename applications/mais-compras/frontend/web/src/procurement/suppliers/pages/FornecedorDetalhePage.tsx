import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { ConfirmToggleAtivoFornecedorModal } from "../components/ConfirmToggleAtivoFornecedorModal";
import { ManualFornecedorForm } from "../components/ManualFornecedorForm";
import {
  alterarStatusFornecedor,
  garantirFornecedorNoErp,
  getFornecedor,
  updateFornecedor
} from "../services/supplierEnrichmentApi";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { labelStatusSincronizacao, type Fornecedor, type ManualFornecedorDraft } from "../types/linxSupplierContract";

const businessUnit = "SOMA";

function fornecedorParaDraft(fornecedor: Fornecedor): ManualFornecedorDraft {
  return {
    razaoSocial: fornecedor.razaoSocial ?? "",
    nomeFantasia: fornecedor.nomeFantasia ?? "",
    cnpj_Cpf: fornecedor.cnpj_Cpf ?? "",
    tipoPessoa: fornecedor.tipoPessoa ?? "PJ",
    email: fornecedor.email ?? "",
    telefone: fornecedor.telefone ?? "",
    website: fornecedor.website ?? "",
    cep: fornecedor.cep ?? "",
    logradouro: fornecedor.logradouro ?? "",
    numero: fornecedor.numero ?? "",
    complemento: fornecedor.complemento ?? "",
    bairro: fornecedor.bairro ?? "",
    cidade: fornecedor.cidade ?? "",
    estado: fornecedor.estado ?? "",
    pais: fornecedor.pais ?? "BR",
    categoria: fornecedor.categoria ?? "",
    cnaePrincipalCodigo: fornecedor.cnaePrincipalCodigo ?? "",
    cnaePrincipalDescricao: fornecedor.cnaePrincipalDescricao ?? ""
  };
}

function statusDoFornecedor(fornecedor: Fornecedor): "Ativo" | "Inativo" {
  return fornecedor.status === "Inativo" ? "Inativo" : "Ativo";
}

/**
 * Detalhe de Fornecedor: somente leitura por padrão, com seções em acordeão (Identificação aberta
 * por padrão; Endereço/Contato/Atividade econômica/Integração ERP colapsáveis). "Editar fornecedor"
 * alterna para o mesmo layout de campos do cadastro manual (ManualFornecedorForm), salvando via PUT.
 */
export function FornecedorDetalhePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const voltarQueryString = searchParams.toString() ? `?${searchParams.toString()}` : "";

  const [fornecedor, setFornecedor] = useState<Fornecedor | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  const [editando, setEditando] = useState(false);
  const [draft, setDraft] = useState<ManualFornecedorDraft | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [erroSalvar, setErroSalvar] = useState<string | null>(null);

  const [confirmandoToggle, setConfirmandoToggle] = useState(false);
  const [alternandoStatus, setAlternandoStatus] = useState(false);
  const [erroToggle, setErroToggle] = useState<string | null>(null);

  const [sincronizando, setSincronizando] = useState(false);
  const [avisoSincronizacao, setAvisoSincronizacao] = useState<string | null>(null);

  const correlationId = useMemo(() => `fornecedor-detalhe-${crypto.randomUUID()}`, []);

  const carregar = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    setNotFound(false);
    try {
      const found = await getFornecedor(id);
      if (!found) {
        setNotFound(true);
        return;
      }
      setFornecedor(found);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar fornecedor.");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  function iniciarEdicao() {
    if (!fornecedor) return;
    setDraft(fornecedorParaDraft(fornecedor));
    setErroSalvar(null);
    setEditando(true);
  }

  async function salvarEdicao(novoDraft: ManualFornecedorDraft) {
    if (!id) return;
    setSalvando(true);
    setErroSalvar(null);
    try {
      const atualizado = await updateFornecedor(id, novoDraft);
      setFornecedor(atualizado);
      setEditando(false);
    } catch (err) {
      setErroSalvar(err instanceof Error ? err.message : "Falha ao salvar fornecedor.");
    } finally {
      setSalvando(false);
    }
  }

  async function confirmarToggleAtivo() {
    if (!fornecedor) return;
    setAlternandoStatus(true);
    setErroToggle(null);
    try {
      const atualizado = await alterarStatusFornecedor(fornecedor.id, statusDoFornecedor(fornecedor) === "Inativo");
      setFornecedor(atualizado);
      setConfirmandoToggle(false);
    } catch (err) {
      setErroToggle(err instanceof Error ? err.message : "Falha ao alterar o status do fornecedor.");
    } finally {
      setAlternandoStatus(false);
    }
  }

  async function sincronizarComErp() {
    if (!fornecedor) return;
    setSincronizando(true);
    setAvisoSincronizacao(null);
    try {
      await garantirFornecedorNoErp(fornecedor.id, businessUnit, correlationId);
      setAvisoSincronizacao("Sincronização com o ERP concluída.");
      await carregar();
    } catch (err) {
      setAvisoSincronizacao(err instanceof Error ? err.message : "Falha ao sincronizar com o ERP.");
    } finally {
      setSincronizando(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <button type="button" className="btn btn-secondary" onClick={() => navigate(`/fornecedores${voltarQueryString}`)}>
          Voltar
        </button>
        <h1>Detalhe do fornecedor</h1>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {notFound && <div className="notice notice-crit">Fornecedor não encontrado.</div>}
      {loading && <div className="empty-state" role="status">Carregando fornecedor...</div>}

      {fornecedor && !editando && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Fornecedor</div>
              <h2>{fornecedor.razaoSocial}</h2>
              <p>{fornecedor.cnpj_Cpf}{fornecedor.nomeFantasia ? ` · ${fornecedor.nomeFantasia}` : ""}</p>
            </div>
            <div className="badges-row">
              <StatusBadge value={statusDoFornecedor(fornecedor)} tone="situacao" />
              <StatusBadge value={labelStatusSincronizacao(fornecedor.statusSincronizacao)} tone="situacao" />
            </div>
          </div>

          {avisoSincronizacao && <div className="notice notice-warn">{avisoSincronizacao}</div>}

          <details open>
            <summary>Identificação</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>Razão Social</span><strong>{fornecedor.razaoSocial}</strong></div>
              <div className="field-readonly"><span>Nome Fantasia</span><strong>{fornecedor.nomeFantasia || "—"}</strong></div>
              <div className="field-readonly"><span>CNPJ</span><strong>{fornecedor.cnpj_Cpf}</strong></div>
              <div className="field-readonly"><span>Tipo de pessoa</span><strong>{fornecedor.tipoPessoa || "—"}</strong></div>
              <div className="field-readonly"><span>Categoria</span><strong>{fornecedor.categoria || "—"}</strong></div>
            </div>
          </details>

          <details>
            <summary>Endereço</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>CEP</span><strong>{fornecedor.cep || "—"}</strong></div>
              <div className="field-readonly"><span>Logradouro</span><strong>{fornecedor.logradouro || "—"}</strong></div>
              <div className="field-readonly"><span>Número</span><strong>{fornecedor.numero || "—"}</strong></div>
              <div className="field-readonly"><span>Complemento</span><strong>{fornecedor.complemento || "—"}</strong></div>
              <div className="field-readonly"><span>Bairro</span><strong>{fornecedor.bairro || "—"}</strong></div>
              <div className="field-readonly"><span>Cidade</span><strong>{fornecedor.cidade || "—"}</strong></div>
              <div className="field-readonly"><span>UF</span><strong>{fornecedor.estado || "—"}</strong></div>
              <div className="field-readonly"><span>País</span><strong>{fornecedor.pais || "—"}</strong></div>
            </div>
          </details>

          <details>
            <summary>Contato</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>E-mail</span><strong>{fornecedor.email || "—"}</strong></div>
              <div className="field-readonly"><span>Telefone</span><strong>{fornecedor.telefone || "—"}</strong></div>
              <div className="field-readonly"><span>Website</span><strong>{fornecedor.website || "—"}</strong></div>
            </div>
          </details>

          <details>
            <summary>Atividade econômica</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>CNAE principal</span><strong>{fornecedor.cnaePrincipalCodigo || "—"}</strong></div>
              <div className="field-readonly"><span>Descrição</span><strong>{fornecedor.cnaePrincipalDescricao || "—"}</strong></div>
            </div>
          </details>

          <details>
            <summary>Integração ERP</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>Sistema ERP</span><strong>{fornecedor.erpSistema || "—"}</strong></div>
              <div className="field-readonly"><span>Id no ERP</span><strong>{fornecedor.erpFornecedorId || "—"}</strong></div>
            </div>
          </details>

          <div className="actions">
            <button type="button" className="btn btn-secondary" onClick={sincronizarComErp} disabled={sincronizando}>
              {sincronizando ? "Sincronizando..." : "Sincronizar com ERP"}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => {
                setErroToggle(null);
                setConfirmandoToggle(true);
              }}
            >
              {statusDoFornecedor(fornecedor) === "Ativo" ? "Inativar fornecedor" : "Ativar fornecedor"}
            </button>
            <button type="button" className="btn btn-primary" onClick={iniciarEdicao}>
              Editar fornecedor
            </button>
          </div>
        </section>
      )}

      {fornecedor && editando && draft && (
        <ManualFornecedorForm
          draft={draft}
          onDraftChange={setDraft}
          onSubmit={salvarEdicao}
          onCancel={() => setEditando(false)}
          loading={salvando}
          error={erroSalvar}
          submitLabel="Salvar alterações"
          cnpjEditavel={false}
        />
      )}

      {fornecedor && confirmandoToggle && (
        <ConfirmToggleAtivoFornecedorModal
          fornecedor={fornecedor}
          ativando={statusDoFornecedor(fornecedor) === "Inativo"}
          error={erroToggle}
          loading={alternandoStatus}
          onConfirm={confirmarToggleAtivo}
          onCancel={() => setConfirmandoToggle(false)}
        />
      )}
    </div>
  );
}
