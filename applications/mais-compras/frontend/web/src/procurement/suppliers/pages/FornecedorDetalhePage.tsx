import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useAuth } from "../../../auth/hooks/useAuth";
import { PERMISSOES } from "../../../auth/types/authTypes";
import { ConfirmToggleAtivoFornecedorModal } from "../components/ConfirmToggleAtivoFornecedorModal";
import { ManualFornecedorForm } from "../components/ManualFornecedorForm";
import {
  alterarStatusFornecedor,
  atualizarFornecedorDoErp,
  garantirFornecedorNoErp,
  getFornecedor,
  updateFornecedor
} from "../services/supplierEnrichmentApi";
import { ConfirmDialog } from "../../../shared/components/ConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { labelStatusSincronizacao, splitTelefone, type Fornecedor, type ManualFornecedorDraft } from "../types/linxSupplierContract";

const businessUnit = "SOMA";
const erpSistemaPadrao = "SOMA_DESENV";

function fornecedorParaDraft(fornecedor: Fornecedor): ManualFornecedorDraft {
  const { ddi, numero } = splitTelefone(fornecedor.telefone);
  return {
    razaoSocial: fornecedor.razaoSocial ?? "",
    nomeFantasia: fornecedor.nomeFantasia ?? "",
    cnpj_Cpf: fornecedor.cnpj_Cpf ?? "",
    tipoPessoa: fornecedor.tipoPessoa ?? "PJ",
    email: fornecedor.email ?? "",
    telefoneDdi: ddi,
    telefone: numero,
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
  const { usuario } = useAuth();
  // Gate de homologação (2026-09-01): "Editar fornecedor" (e, dentro da edição, "Ativar/Inativar")
  // exige Fornecedor.Editar — mesmo padrão de gate de permissão do restante do +Compras.
  const permissoesEfetivas = (usuario?.permissoes ?? []).map((codigo) => codigo.toLowerCase());
  const podeEditarFornecedor = permissoesEfetivas.includes(PERMISSOES.fornecedorEditar.toLowerCase());
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
  // Gate de homologação (2026-09-01): salvar edição de fornecedor pede confirmação num modal da
  // própria aplicação antes de persistir — nunca window.confirm nativo do navegador.
  const [draftParaConfirmar, setDraftParaConfirmar] = useState<ManualFornecedorDraft | null>(null);

  const [confirmandoToggle, setConfirmandoToggle] = useState(false);
  const [alternandoStatus, setAlternandoStatus] = useState(false);
  const [erroToggle, setErroToggle] = useState<string | null>(null);

  // Duas ações distintas e independentes (Gate de homologação, 2026-09-01, item 2): "Enviar ao
  // ERP" (+Compras -> ERP, upsert existente) e "Atualizar do ERP" (ERP -> +Compras, engine de
  // sincronização já existente no backend). Cada uma com seu próprio estado de loading/aviso —
  // nunca a mesma ação por trás de um único rótulo ambíguo.
  const [enviandoAoErp, setEnviandoAoErp] = useState(false);
  const [atualizandoDoErp, setAtualizandoDoErp] = useState(false);
  // Um único aviso de ERP (Gate de homologação, 2026-09-01): a mensagem de "Enviar ao ERP" e a de
  // "Atualizar do ERP" nunca aparecem empilhadas — escrever uma nova sempre substitui a anterior.
  const [avisoErp, setAvisoErp] = useState<string | null>(null);

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

  async function confirmarSalvarEdicao() {
    if (!draftParaConfirmar) return;
    const novoDraft = draftParaConfirmar;
    setDraftParaConfirmar(null);
    await salvarEdicao(novoDraft);
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

  /** +Compras -> ERP: envia os dados locais atuais, cria/atualiza o registro no ERP. */
  async function enviarAoErp() {
    if (!fornecedor) return;
    setEnviandoAoErp(true);
    setAvisoErp(null);
    try {
      await garantirFornecedorNoErp(fornecedor.id, businessUnit, correlationId);
      setAvisoErp("Envio ao ERP concluído.");
      await carregar();
    } catch (err) {
      setAvisoErp(err instanceof Error ? err.message : "Falha ao enviar ao ERP.");
    } finally {
      setEnviandoAoErp(false);
    }
  }

  /**
   * ERP -> +Compras: relê o Linx e reflete no +Compras via ISincronizarFornecedorUseCase — a
   * mesma engine que já resolve conflito por timestamp/hash. Se houver conflito não resolvido
   * automaticamente, o backend responde de acordo com a state machine de Review já existente
   * (nenhuma sobrescrita silenciosa é feita aqui além do que a engine já decide).
   */
  async function atualizarDoErp() {
    if (!fornecedor) return;
    setAtualizandoDoErp(true);
    setAvisoErp(null);
    try {
      await atualizarFornecedorDoErp({
        fornecedorId: fornecedor.id,
        businessUnit,
        erpSistema: fornecedor.erpSistema || erpSistemaPadrao,
        erpFornecedorId: fornecedor.erpFornecedorId,
        correlationId
      });
      setAvisoErp("Dados atualizados a partir do ERP.");
      await carregar();
    } catch (err) {
      setAvisoErp(err instanceof Error ? err.message : "Falha ao atualizar a partir do ERP.");
    } finally {
      setAtualizandoDoErp(false);
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

          {avisoErp && <div className="notice notice-warn">{avisoErp}</div>}

          <details open>
            <summary>Identificação</summary>
            <div className="data-grid">
              <div className="field-readonly"><span>Tipo de pessoa</span><strong>{fornecedor.tipoPessoa || "—"}</strong></div>
            </div>
            <div className="data-grid">
              <div className="field-readonly"><span>Razão Social</span><strong>{fornecedor.razaoSocial}</strong></div>
              <div className="field-readonly"><span>Nome Fantasia</span><strong>{fornecedor.nomeFantasia || "—"}</strong></div>
              <div className="field-readonly"><span>CNPJ</span><strong>{fornecedor.cnpj_Cpf}</strong></div>
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
            <button type="button" className="btn btn-secondary" onClick={atualizarDoErp} disabled={atualizandoDoErp}>
              {atualizandoDoErp ? "Atualizando..." : "Atualizar do ERP"}
            </button>
            <button type="button" className="btn btn-secondary" onClick={enviarAoErp} disabled={enviandoAoErp}>
              {enviandoAoErp ? "Enviando..." : "Enviar ao ERP"}
            </button>
            {/* Ativar/Inativar não fica na visão de leitura (item de feedback do homologador,
                2026-09-01) — só é acessível dentro da edição do fornecedor. */}
            {podeEditarFornecedor && (
              <button type="button" className="btn btn-primary" onClick={iniciarEdicao}>
                Editar fornecedor
              </button>
            )}
          </div>
        </section>
      )}

      {fornecedor && editando && draft && podeEditarFornecedor && (
        <section className="card">
          {/* Ativar/Inativar só existe dentro da edição (item de feedback do homologador,
              2026-09-01), gated pela mesma permissão Fornecedor.Editar. */}
          <div className="actions">
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
          </div>
          <ManualFornecedorForm
            draft={draft}
            onDraftChange={setDraft}
            onSubmit={(novoDraft) => setDraftParaConfirmar(novoDraft)}
            onCancel={() => setEditando(false)}
            loading={salvando}
            error={erroSalvar}
            submitLabel="Salvar alterações"
            cnpjEditavel={false}
          />
        </section>
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

      {draftParaConfirmar && (
        <ConfirmDialog
          title="Confirmar edição"
          message="Deseja realmente salvar as alterações deste fornecedor?"
          confirmLabel="Salvar alterações"
          onConfirm={confirmarSalvarEdicao}
          onCancel={() => setDraftParaConfirmar(null)}
        />
      )}
    </div>
  );
}
