import { FormEvent, useMemo, useState } from "react";
import {
  analyzeEnrichment,
  consultCnpj,
  createSupplierDraft,
  decideEnrichment,
  normalizeDocument,
  searchSupplierByDocument
} from "./supplierEnrichmentApi";
import type {
  ConsultaCnpjResultado,
  Fornecedor,
  FornecedorCampoDivergencia,
  FornecedorEnriquecimentoAnalise
} from "./linxSupplierContract";

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
      <header className="portal-header">
        <div className="brand-mark">AZZAS 2154</div>
        <div className="logo-suffix">+Compras · Cadastro de Fornecedor</div>
        <div className="user-chip">COMPRAS</div>
      </header>

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
          <form className="card form-card" onSubmit={handleConsult}>
            <label htmlFor="cnpjCpf">Cnpj_Cpf</label>
            <div className="input-row">
              <input
                id="cnpjCpf"
                value={documento}
                onChange={(event) => setDocumento(event.target.value)}
                placeholder="12345678000195"
                maxLength={18}
              />
              <button className="btn btn-primary" disabled={loading} type="submit">
                <SearchIcon /> Consultar CNPJ
              </button>
            </div>
            {error && <div className="notice notice-crit">{error}</div>}
          </form>

          {consulta && <ConsultaPanel consulta={consulta} />}

          {consulta?.situacaoCadastral === "Baixada" && (
            <div className="notice notice-warn">
              <strong>Atencao:</strong> Fornecedor possui situacao cadastral BAIXADA. Deseja continuar?
              <label className="check-line">
                <input type="checkbox" checked={baixadaConfirmed} onChange={(event) => setBaixadaConfirmed(event.target.checked)} />
                Confirmar continuidade
              </label>
            </div>
          )}

          {analise && (
            <section className="card">
              <div className="card-heading">
                <div>
                  <div className="section-title">Comparacao</div>
                  <h2>Divergencias encontradas</h2>
                </div>
                <span className="badge">{analise.divergencias.length} campos</span>
              </div>
              {analise.alertas.map((alerta) => <div className="notice notice-warn" key={alerta}>{alerta}</div>)}
              <DivergenceTable divergencias={analise.divergencias} selectedFields={selectedFields} onToggle={toggleField} />
              <div className="actions">
                <button className="btn btn-secondary" disabled={loading || selectedFields.length === 0} onClick={() => handleDecision("rejeitar")}>
                  <XIcon /> Rejeitar
                </button>
                <button className="btn btn-primary" disabled={loading || selectedFields.length === 0} onClick={() => handleDecision("aprovar")}>
                  <CheckIcon /> Aceitar
                </button>
              </div>
            </section>
          )}
        </div>
      </section>
    </main>
  );
}

function ConsultaPanel({ consulta }: { consulta: ConsultaCnpjResultado }) {
  return (
    <section className="card">
      <div className="card-heading">
        <div>
          <div className="section-title">Consulta realizada</div>
          <h2>Dados retornados</h2>
        </div>
        <span className={`status status-${consulta.situacaoCadastral.toLowerCase()}`}>{consulta.situacaoCadastral}</span>
      </div>
      <DataGrid title="Identificacao" items={[
        ["Cnpj_Cpf", consulta.cnpj_Cpf],
        ["RazaoSocial", consulta.razaoSocial],
        ["NomeFantasia", consulta.nomeFantasia],
        ["TipoPessoa", consulta.tipoPessoa]
      ]} />
      <DataGrid title="Situacao" items={[
        ["SituacaoCadastral", consulta.situacaoCadastral],
        ["DataSituacaoCadastral", formatDate(consulta.dataSituacaoCadastral)]
      ]} />
      <DataGrid title="Endereco" items={[
        ["Cep", consulta.cep],
        ["Logradouro", consulta.logradouro],
        ["Numero", consulta.numero],
        ["Complemento", consulta.complemento],
        ["Bairro", consulta.bairro],
        ["Cidade", consulta.cidade],
        ["Estado", consulta.estado]
      ]} />
      <DataGrid title="Contato" items={[
        ["Email", consulta.email],
        ["Telefone", consulta.telefone]
      ]} />
    </section>
  );
}

function DataGrid({ title, items }: { title: string; items: Array<[string, string | null | undefined]> }) {
  return (
    <div className="data-block">
      <div className="section-title">{title}</div>
      <div className="data-grid">
        {items.map(([label, value]) => (
          <div className="field-readonly" key={label}>
            <span>{label}</span>
            <strong>{value || "Nao informado"}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

function DivergenceTable({ divergencias, selectedFields, onToggle }: {
  divergencias: FornecedorCampoDivergencia[];
  selectedFields: string[];
  onToggle: (field: string) => void;
}) {
  if (divergencias.length === 0) return <div className="empty-state">Nenhuma divergencia encontrada.</div>;
  return (
    <table className="divergence-table">
      <thead>
        <tr><th>Usar</th><th>Campo</th><th>Atual</th><th>Sugestao</th><th>Decisao</th></tr>
      </thead>
      <tbody>
        {divergencias.map((item) => {
          const protectedField = protectedFields.has(item.campo);
          return (
            <tr key={item.campo}>
              <td>
                <input
                  aria-label={`Selecionar ${item.campo}`}
                  type="checkbox"
                  disabled={protectedField}
                  checked={!protectedField && selectedFields.includes(item.campo)}
                  onChange={() => onToggle(item.campo)}
                />
              </td>
              <td>{item.campo}{protectedField && <span className="lock-note">ERP</span>}</td>
              <td>{item.valorAtual || "Nao informado"}</td>
              <td>{item.valorSugerido || "Nao informado"}</td>
              <td><span className="badge">{item.statusDecisao}</span></td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function formatDate(value?: string | null) {
  if (!value) return "Nao informado";
  return new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" }).format(new Date(`${value}T00:00:00Z`));
}

function formatDateTime(value?: string | null) {
  if (!value) return "Aguardando";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

function SearchIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.35-4.35" /></svg>;
}

function CheckIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><polyline points="20 6 9 17 4 12" /></svg>;
}

function XIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>;
}
