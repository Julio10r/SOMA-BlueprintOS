import { FormEvent, useMemo, useState } from "react";
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
  FornecedorEnriquecimentoAnalise
} from "../types/linxSupplierContract";
import { CnpjSearch } from "./CnpjSearch";
import { SupplierComparison } from "./SupplierComparison";
import { ApprovalPanel } from "./ApprovalPanel";

const businessUnit = "SOMA";
const erpSistema = "SOMA_DESENV";
const protectedFields = new Set(["NomeFantasia", "Cnpj_Cpf"]);

export function CadastroFornecedor() {
  const [documento, setDocumento] = useState("");
  const [supplier, setSupplier] = useState<Fornecedor | null>(null);
  const [consulta, setConsulta] = useState<ConsultaCnpjResultado | null>(null);
  const [analise, setAnalise] = useState<FornecedorEnriquecimentoAnalise | null>(null);
  const [selectedFields, setSelectedFields] = useState<string[]>([]);
  const [baixadaConfirmed, setBaixadaConfirmed] = useState(false);
  const [status, setStatus] = useState<string>("Informe um CNPJ, CPF ou documento alfanumerico.");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const correlationId = useMemo(() => `b224-${crypto.randomUUID()}`, []);

  async function handleConsult(event: FormEvent) {
    event.preventDefault();
    const normalized = normalizeDocument(documento);
    setError(null);
    setConsulta(null);
    setAnalise(null);
    setSelectedFields([]);
    setBaixadaConfirmed(false);

    if (!/^[A-Za-z0-9]{1,14}$/.test(normalized)) {
      setError("Informe CPF/CNPJ com ate 14 caracteres alfanumericos.");
      return;
    }

    setLoading(true);
    try {
      setStatus("Consultando fonte externa e cadastro +Compras.");
      const [existingSupplier, query] = await Promise.all([
        searchSupplierByDocument(normalized),
        consultCnpj(normalized, { businessUnit, erpSistema, correlationId })
      ]);

      if (!query.sucesso) {
        setConsulta(query);
        setStatus("Consulta registrada sem dados de enriquecimento.");
        return;
      }

      const currentSupplier = existingSupplier ?? await createSupplierDraft(normalized, query);
      const enrichment = await analyzeEnrichment(currentSupplier.id, query, { businessUnit, erpSistema, correlationId });
      setSupplier(currentSupplier);
      setConsulta(query);
      setAnalise(enrichment);
      setSelectedFields(enrichment.divergencias.map((item) => item.campo).filter((campo) => !protectedFields.has(campo)));
      setStatus(existingSupplier ? "Fornecedor localizado e divergencias calculadas." : "Fornecedor criado no +Compras e enriquecimento preparado.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao consultar CNPJ.");
      setStatus("Consulta interrompida.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDecision(decision: "aprovar" | "rejeitar") {
    if (!supplier || !consulta || !analise) return;
    if (consulta.situacaoCadastral === "Baixada" && !baixadaConfirmed) {
      setError("Confirme a situacao cadastral BAIXADA antes de continuar.");
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const fields = decision === "aprovar"
        ? selectedFields.filter((field) => !protectedFields.has(field))
        : selectedFields;
      const updated = await decideEnrichment(supplier.id, decision, consulta, fields, { businessUnit, erpSistema, correlationId });
      setAnalise(updated);
      setStatus(decision === "aprovar" ? "Campos aprovados e persistidos no +Compras." : "Divergencias rejeitadas e auditadas.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao registrar decisao.");
    } finally {
      setLoading(false);
    }
  }

  function toggleField(field: string) {
    if (protectedFields.has(field)) return;
    setSelectedFields((current) => current.includes(field)
      ? current.filter((item) => item !== field)
      : [...current, field]);
  }

  return (
    <main className="supplier-page">
      <section className="workspace">
        <aside className="summary-panel">
          <div className="section-title">B2.2.4</div>
          <h1>Cadastro com enriquecimento CNPJ</h1>
          <p>{status}</p>
          <dl>
            <div><dt>Fonte</dt><dd>{consulta?.fonteConsulta ?? "Aguardando consulta"}</dd></div>
            <div><dt>Data/hora</dt><dd>{formatDateTime(consulta?.dataConsulta)}</dd></div>
            <div><dt>Usuario</dt><dd>Identidade temporaria de desenvolvimento</dd></div>
            <div><dt>CorrelationId</dt><dd className="mono">{correlationId}</dd></div>
          </dl>
        </aside>

        <div className="content">
          <CnpjSearch value={documento} onChange={setDocumento} onSubmit={handleConsult} loading={loading} error={error} />

          {consulta && (
            <SupplierComparison
              consulta={consulta}
              divergencias={analise?.divergencias}
              selectedFields={selectedFields}
              protectedFields={protectedFields}
              onToggleField={toggleField}
            />
          )}

          {analise && (
            <ApprovalPanel
              alertas={analise.alertas}
              situacaoCadastral={consulta?.situacaoCadastral}
              baixadaConfirmed={baixadaConfirmed}
              onBaixadaConfirmChange={setBaixadaConfirmed}
              selectedFieldsCount={selectedFields.length}
              loading={loading}
              onApprove={() => handleDecision("aprovar")}
              onReject={() => handleDecision("rejeitar")}
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
