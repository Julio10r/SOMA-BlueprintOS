import { FormEvent, useMemo, useRef, useState } from "react";
import {
  analyzeEnrichment,
  consultCnpj,
  createSupplierDraft,
  decideEnrichment,
  normalizeDocument,
  searchSupplierByDocument
} from "../services/supplierEnrichmentApi";
import type {
  ConsultaCnpjResultado,
  Fornecedor,
  FornecedorEnriquecimentoAnalise,
  SituacaoCadastralCnpj
} from "../types/linxSupplierContract";
import { CnpjSearch } from "./CnpjSearch";
import { SupplierComparison } from "./SupplierComparison";
import { ApprovalPanel } from "./ApprovalPanel";
import { NovoFornecedorPanel, type NovoFornecedorDraft } from "./NovoFornecedorPanel";

const businessUnit = "SOMA";
const erpSistema = "SOMA_DESENV";
const protectedFields = new Set(["NomeFantasia", "Cnpj_Cpf"]);
const situacoesQueExigemConfirmacao = new Set<SituacaoCadastralCnpj>(["Baixada", "Suspensa", "Inapta"]);

/**
 * State machine explicita do fluxo de consulta/cadastro de Fornecedor por
 * CNPJ (ADR-0023, B2.6 — corrige BUG-1: CONSULTAR nunca significa
 * CADASTRAR):
 *
 *   Idle -> Validating -> Consulting -> Review -> Persisting -> Success
 *                              |             |
 *                       ErrorValidacao  ErrorConsulta / ErrorPersistencia
 *
 * "Review" nunca escreve no +Compras. A unica transicao que persiste um
 * Fornecedor novo e "Review -> Persisting", disparada exclusivamente pelo
 * clique explicito em "Cadastrar fornecedor" (NovoFornecedorPanel). Quando o
 * documento consultado ja corresponde a um Fornecedor existente, a mesma
 * garantia vale para a decisao de aprovar/rejeitar campos divergentes
 * (ApprovalPanel), que so persiste apos clique explicito.
 */
type FlowState =
  | "Idle"
  | "Validating"
  | "Consulting"
  | "Review"
  | "Persisting"
  | "Success"
  | "ErrorValidacao"
  | "ErrorConsulta"
  | "ErrorPersistencia";

const draftInicial: NovoFornecedorDraft = {
  razaoSocial: "", nomeFantasia: "", email: "", telefone: "",
  cep: "", logradouro: "", numero: "", complemento: "", bairro: "", cidade: "", estado: ""
};

export function CadastroFornecedor() {
  const [documento, setDocumento] = useState("");
  const [flowState, setFlowState] = useState<FlowState>("Idle");
  const [supplier, setSupplier] = useState<Fornecedor | null>(null);
  const [consulta, setConsulta] = useState<ConsultaCnpjResultado | null>(null);
  const [analise, setAnalise] = useState<FornecedorEnriquecimentoAnalise | null>(null);
  const [selectedFields, setSelectedFields] = useState<string[]>([]);
  const [situacaoConfirmed, setSituacaoConfirmed] = useState(false);
  const [novoDraft, setNovoDraft] = useState<NovoFornecedorDraft>(draftInicial);
  const [status, setStatus] = useState<string>("Informe um CNPJ, CPF ou documento alfanumerico.");
  const [error, setError] = useState<string | null>(null);
  const correlationId = useMemo(() => `b224-${crypto.randomUUID()}`, []);
  // Guarda contra submissao duplicada: complementa o disabled do botao,
  // cobrindo double-click/duplo Enter dentro da mesma janela de evento.
  const persistindoRef = useRef(false);

  const loading = flowState === "Validating" || flowState === "Consulting" || flowState === "Persisting";
  const requiresSituacaoConfirmation = !!consulta
    && !!consulta.situacaoCadastral
    && situacoesQueExigemConfirmacao.has(consulta.situacaoCadastral);

  async function handleConsult(event: FormEvent) {
    event.preventDefault();
    const normalized = normalizeDocument(documento);
    setError(null);
    setConsulta(null);
    setAnalise(null);
    setSelectedFields([]);
    setSituacaoConfirmed(false);
    setNovoDraft(draftInicial);
    setSupplier(null);

    setFlowState("Validating");
    if (!/^[A-Za-z0-9]{1,14}$/.test(normalized)) {
      setError("Informe CPF/CNPJ com ate 14 caracteres alfanumericos.");
      setFlowState("ErrorValidacao");
      return;
    }

    setFlowState("Consulting");
    setStatus("Consultando fonte externa (somente leitura — nenhum Fornecedor e criado nesta etapa).");
    try {
      // Consulta e busca de fornecedor existente sao ambas operacoes de
      // leitura: nenhuma delas persiste um Fornecedor. A criacao so ocorre
      // depois, na etapa "Review", por confirmacao explicita do usuario.
      const [existingSupplier, query] = await Promise.all([
        searchSupplierByDocument(normalized),
        consultCnpj(normalized, { businessUnit, erpSistema, correlationId })
      ]);

      setConsulta(query);

      // CNPJ ja existente no +Compras NUNCA deve resultar em erro de "ja existe" (principio
      // de convergencia CREATE/ADD_ROLE/UPDATE, B2.9/ADR-0023): a existencia local do Fornecedor
      // e verificada ANTES de decidir por um estado de erro generico, mesmo que a reconsulta
      // externa (BrasilAPI) tenha falhado de forma transitoria (ex: rate limit por reconsultar o
      // mesmo documento logo apos o CREATE). Corrige BUG: reconsultar o CNPJ recem-cadastrado
      // caia em "ErrorConsulta" em vez de carregar o fornecedor existente para revisao/edicao.
      if (existingSupplier) {
        setSupplier(existingSupplier);
        if (query.sucesso) {
          const enrichment = await analyzeEnrichment(existingSupplier.id, query, { businessUnit, erpSistema, correlationId });
          setAnalise(enrichment);
          setSelectedFields(enrichment.divergencias.map((item) => item.campo).filter((campo) => !protectedFields.has(campo)));
          setStatus("Fornecedor localizado e divergencias calculadas. Revise antes de aprovar ou rejeitar.");
        } else {
          // Fornecedor ja existe no +Compras, mas a reconsulta externa falhou (rate limit, fonte
          // indisponivel, timeout, etc.). Isso nunca deve ser tratado como "ja existe" nem como
          // erro de consulta: carregamos o fornecedor existente sem divergencias calculadas,
          // avisando o usuario que os dados externos nao puderam ser atualizados agora.
          setAnalise({
            fornecedorId: existingSupplier.id,
            cnpj_Cpf: normalized,
            consultaId: null,
            fonteConsulta: query.fonteConsulta,
            correlationId,
            divergencias: [],
            alertas: ["Nao foi possivel reconsultar a fonte externa agora; exibindo dados ja cadastrados."]
          });
          setSelectedFields([]);
          setStatus("Fornecedor ja cadastrado. Reconsulta externa indisponivel no momento — revise os dados atuais.");
        }
        setFlowState("Review");
        return;
      }

      if (!query.sucesso) {
        setStatus("Consulta registrada sem dados de enriquecimento.");
        setFlowState("ErrorConsulta");
        return;
      }

      {
        setNovoDraft({
          razaoSocial: query.razaoSocial ?? "",
          nomeFantasia: query.nomeFantasia ?? "",
          email: query.email ?? "",
          telefone: query.telefone ?? "",
          cep: query.cep ?? "",
          logradouro: query.logradouro ?? "",
          numero: query.numero ?? "",
          complemento: query.complemento ?? "",
          bairro: query.bairro ?? "",
          cidade: query.cidade ?? "",
          estado: query.estado ?? ""
        });
        setStatus("Nenhum fornecedor encontrado para este documento. Revise os dados antes de cadastrar.");
      }
      setFlowState("Review");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao consultar CNPJ.");
      setStatus("Consulta interrompida.");
      setFlowState("ErrorConsulta");
    }
  }

  async function handleDecision(decision: "aprovar" | "rejeitar") {
    if (!supplier || !consulta || !analise) return;
    if (requiresSituacaoConfirmation && !situacaoConfirmed) {
      setError(`Confirme a situacao cadastral ${consulta.situacaoCadastral} antes de continuar.`);
      return;
    }
    if (persistindoRef.current) return;

    persistindoRef.current = true;
    setFlowState("Persisting");
    setError(null);
    try {
      const fields = decision === "aprovar"
        ? selectedFields.filter((field) => !protectedFields.has(field))
        : selectedFields;
      const updated = await decideEnrichment(supplier.id, decision, consulta, fields, { businessUnit, erpSistema, correlationId });
      setAnalise(updated);
      setStatus(decision === "aprovar" ? "Campos aprovados e persistidos no +Compras." : "Divergencias rejeitadas e auditadas.");
      setFlowState("Success");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao registrar decisao.");
      setFlowState("ErrorPersistencia");
    } finally {
      persistindoRef.current = false;
    }
  }

  async function handleCadastrarNovoFornecedor() {
    if (!consulta) return;
    if (requiresSituacaoConfirmation && !situacaoConfirmed) {
      setError(`Confirme a situacao cadastral ${consulta.situacaoCadastral} antes de continuar.`);
      return;
    }
    // Guarda contra submissao duplicada: bloqueia reentrada enquanto uma
    // persistencia ja esta em curso (o botao tambem fica disabled, mas o
    // ref cobre cliques que cheguem antes do re-render).
    if (persistindoRef.current) return;

    persistindoRef.current = true;
    setFlowState("Persisting");
    setError(null);
    try {
      const normalized = normalizeDocument(documento);
      const created = await createSupplierDraft(normalized, { ...consulta, ...novoDraft });
      setSupplier(created);
      setStatus("Fornecedor cadastrado no +Compras.");
      setFlowState("Success");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao cadastrar fornecedor.");
      setFlowState("ErrorPersistencia");
    } finally {
      persistindoRef.current = false;
    }
  }

  function toggleField(field: string) {
    if (protectedFields.has(field)) return;
    setSelectedFields((current) => current.includes(field)
      ? current.filter((item) => item !== field)
      : [...current, field]);
  }

  const showExistingSupplierFlow = flowState !== "Idle" && !!supplier && !!analise;
  const showNovoFornecedorFlow = (flowState === "Review" || flowState === "Persisting" || flowState === "ErrorPersistencia")
    && !supplier && !!consulta && consulta.sucesso;

  return (
    <main className="supplier-page">
      <section className="workspace">
        <aside className="summary-panel">
          <div className="section-title">B2.6</div>
          <h1>Cadastro com enriquecimento CNPJ</h1>
          <p>{status}</p>
          <dl>
            <div><dt>Estado</dt><dd className="mono">{flowState}</dd></div>
            <div><dt>Fonte</dt><dd>{consulta?.fonteConsulta ?? "Aguardando consulta"}</dd></div>
            <div><dt>Data/hora</dt><dd>{formatDateTime(consulta?.dataConsulta)}</dd></div>
            <div><dt>Usuario</dt><dd>Identidade temporaria de desenvolvimento</dd></div>
            <div><dt>CorrelationId</dt><dd className="mono">{correlationId}</dd></div>
          </dl>
        </aside>

        <div className="content">
          <CnpjSearch value={documento} onChange={setDocumento} onSubmit={handleConsult} loading={loading} error={error} />

          {consulta && consulta.sucesso && (
            <SupplierComparison
              consulta={consulta}
              divergencias={analise?.divergencias}
              selectedFields={selectedFields}
              protectedFields={protectedFields}
              onToggleField={toggleField}
            />
          )}

          {showExistingSupplierFlow && (
            <ApprovalPanel
              alertas={analise!.alertas}
              situacaoCadastral={consulta?.situacaoCadastral}
              baixadaConfirmed={situacaoConfirmed}
              onBaixadaConfirmChange={setSituacaoConfirmed}
              selectedFieldsCount={selectedFields.length}
              loading={loading}
              onApprove={() => handleDecision("aprovar")}
              onReject={() => handleDecision("rejeitar")}
            />
          )}

          {showNovoFornecedorFlow && (
            <NovoFornecedorPanel
              draft={novoDraft}
              onDraftChange={setNovoDraft}
              situacaoCadastral={consulta?.situacaoCadastral}
              confirmacaoNecessaria={requiresSituacaoConfirmation}
              confirmado={situacaoConfirmed}
              onConfirmadoChange={setSituacaoConfirmed}
              loading={loading}
              onCadastrar={handleCadastrarNovoFornecedor}
            />
          )}
        </div>
      </section>
    </main>
  );
}

function formatDateTime(value?: string | null) {
  if (!value) return "Aguardando";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}
