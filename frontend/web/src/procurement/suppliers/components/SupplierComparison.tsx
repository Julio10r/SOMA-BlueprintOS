import { useState, type ReactNode } from "react";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { SituacaoCadastralBadge } from "./SituacaoCadastralBadge";
import type { ConsultaCnpjResultado, Fornecedor, FornecedorCampoDivergencia } from "../types/linxSupplierContract";

/**
 * Mostra os dados retornados pela consulta externa de CNPJ e, quando
 * disponivel, a tabela de divergencias campo a campo (dado atual x sugestao
 * externa). Puramente apresentacional/selecao: a decisao de aprovar ou
 * rejeitar fica a cargo do ApprovalPanel; este componente apenas alterna a
 * selecao de cada linha via onToggleField.
 */
export function SupplierComparison({
  consulta,
  divergencias,
  selectedFields,
  protectedFields,
  onToggleField
}: {
  consulta: ConsultaCnpjResultado;
  divergencias?: FornecedorCampoDivergencia[];
  selectedFields: string[];
  protectedFields: Set<string>;
  onToggleField: (field: string) => void;
}) {
  return (
    <>
      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Consulta realizada</div>
            <h2>Dados retornados</h2>
          </div>
          <SituacaoCadastralBadge value={consulta.situacaoCadastral} />
        </div>
        <div className="info-stack">
          <InfoCard title="Identificacao" defaultOpen>
            <InfoGrid items={[
              ["CNPJ/CPF", consulta.cnpj_Cpf],
              ["Razão Social", consulta.razaoSocial],
              ["Nome Fantasia", consulta.nomeFantasia],
              ["Tipo de Pessoa", consulta.tipoPessoa]
            ]} />
          </InfoCard>
          <InfoCard title="Situacao cadastral" defaultOpen>
            <InfoGrid items={[
              ["Situação Cadastral", consulta.situacaoCadastral],
              ["Data da Situação Cadastral", formatDate(consulta.dataSituacaoCadastral)]
            ]} />
          </InfoCard>
          <InfoCard title="CNAE principal">
            <InfoGrid items={[
              ["Código", formatCnaeCodigo(consulta.cnaePrincipalCodigo)],
              ["Descrição", consulta.cnaePrincipalDescricao]
            ]} />
          </InfoCard>
          <InfoCard title="Endereco">
            <InfoGrid items={[
              ["CEP", consulta.cep],
              ["Logradouro", consulta.logradouro],
              ["Número", consulta.numero],
              ["Complemento", consulta.complemento],
              ["Bairro", consulta.bairro],
              ["Cidade", consulta.cidade],
              ["Estado", consulta.estado]
            ]} />
          </InfoCard>
          <InfoCard title="Contato">
            <InfoGrid items={[
              ["E-mail", consulta.email],
              ["Telefone", consulta.telefone]
            ]} />
          </InfoCard>
        </div>
      </section>

      {divergencias && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Comparacao</div>
              <h2>Divergencias encontradas</h2>
            </div>
            <span className="badge">{divergencias.length} campos</span>
          </div>
          <DivergenceTable
            divergencias={divergencias}
            selectedFields={selectedFields}
            protectedFields={protectedFields}
            onToggle={onToggleField}
          />
        </section>
      )}
    </>
  );
}

/**
 * DR-10 (Design Review Pos-Onda 1): quando o fornecedor ja existe localmente
 * mas a reconsulta externa falhou (rate limit, fonte indisponivel, timeout),
 * o fluxo anterior deixava a tela sem NENHUM dado visivel do fornecedor —
 * apenas o alerta de falha e um painel de decisao vazio. Isso viola "nenhuma
 * decisao relevante sem contexto visivel". Este bloco exibe os dados JA
 * CADASTRADOS localmente (objeto `Fornecedor` retornado por
 * `searchSupplierByDocument`), para que o usuario tenha o que revisar mesmo
 * sem dados atualizados da fonte externa. Os botoes Aceitar/Rejeitar
 * continuam desabilitados (nenhuma divergencia foi calculada), mas a UI
 * deixa de aparecer vazia.
 */
export function ExistingSupplierSnapshot({ supplier }: { supplier: Fornecedor }) {
  return (
    <section className="card">
      <div className="card-heading">
        <div>
          <div className="section-title">Reconsulta indisponivel</div>
          <h2>Dados atuais no +Compras</h2>
        </div>
      </div>
      <p className="notice notice-warn">
        Nao foi possivel obter dados atualizados da fonte externa agora. Nenhuma divergencia foi calculada,
        por isso os botoes Aceitar/Rejeitar permanecem desabilitados ate uma nova consulta bem-sucedida. Os
        dados abaixo sao os ja cadastrados no +Compras.
      </p>
      <div className="info-stack">
        <InfoCard title="Identificacao" defaultOpen>
          <InfoGrid items={[
            ["CNPJ/CPF", supplier.cnpj_Cpf],
            ["Razão Social", supplier.razaoSocial],
            ["Nome Fantasia", supplier.nomeFantasia],
            ["Tipo de Pessoa", supplier.tipoPessoa]
          ]} />
        </InfoCard>
        <InfoCard title="CNAE principal">
          <InfoGrid items={[
            ["Código", formatCnaeCodigo(supplier.cnaePrincipalCodigo)],
            ["Descrição", supplier.cnaePrincipalDescricao]
          ]} />
        </InfoCard>
        <InfoCard title="Endereco">
          <InfoGrid items={[
            ["CEP", supplier.cep],
            ["Logradouro", supplier.logradouro],
            ["Número", supplier.numero],
            ["Complemento", supplier.complemento],
            ["Bairro", supplier.bairro],
            ["Cidade", supplier.cidade],
            ["Estado", supplier.estado]
          ]} />
        </InfoCard>
        <InfoCard title="Contato">
          <InfoGrid items={[
            ["E-mail", supplier.email],
            ["Telefone", supplier.telefone]
          ]} />
        </InfoCard>
      </div>
    </section>
  );
}

/**
 * Card colapsavel com header mono-uppercase + chevron (padrao
 * component-info-card.html). Identificacao e Situacao cadastral (os dados
 * de decisao mais importantes) abrem por padrao via `defaultOpen`; blocos
 * secundarios (CNAE, Endereco, Contato) comecam fechados para reduzir a
 * sensacao de tela monumental que motivou a reprovacao do PO.
 */
export function InfoCard({ title, defaultOpen, badge, children }: {
  title: string;
  defaultOpen?: boolean;
  badge?: ReactNode;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(!!defaultOpen);
  return (
    <div className={`info-card${open ? " open" : ""}`}>
      <button type="button" className="info-h" aria-expanded={open} onClick={() => setOpen((current) => !current)}>
        <span className="info-t">{title}</span>
        <span className="info-h-right">
          {badge}
          <ChevIcon />
        </span>
      </button>
      <div className="info-b">{children}</div>
    </div>
  );
}

function ChevIcon() {
  return (
    <svg className="chev" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <polyline points="6 9 12 15 18 9" />
    </svg>
  );
}

function InfoGrid({ items }: { items: Array<[string, string | null | undefined]> }) {
  return (
    <div className="info-grid">
      {items.map(([label, value]) => (
        <div className="info-row" key={label}>
          <span className="l">{label}</span>
          <span className="v">{value || "Nao informado"}</span>
        </div>
      ))}
    </div>
  );
}

function DivergenceTable({ divergencias, selectedFields, protectedFields, onToggle }: {
  divergencias: FornecedorCampoDivergencia[];
  selectedFields: string[];
  protectedFields: Set<string>;
  onToggle: (field: string) => void;
}) {
  if (divergencias.length === 0) return <div className="empty-state">Nenhuma divergencia encontrada.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr><th>Usar</th><th>Campo</th><th>Atual</th><th>Sugestao</th><th>Decisao</th></tr>
      </thead>
      <tbody>
        {divergencias.map((item) => {
          const isProtected = protectedFields.has(item.campo);
          return (
            <tr key={item.campo}>
              <td>
                <input
                  aria-label={`Selecionar ${item.campo}`}
                  type="checkbox"
                  disabled={isProtected}
                  checked={!isProtected && selectedFields.includes(item.campo)}
                  onChange={() => onToggle(item.campo)}
                />
              </td>
              <td>{item.campo}{isProtected && <span className="lock-note">ERP</span>}</td>
              <td>{item.valorAtual || "Nao informado"}</td>
              <td>{item.valorSugerido || "Nao informado"}</td>
              <td><StatusBadge value={item.statusDecisao} tone="decisao" /></td>
            </tr>
          );
        })}
      </tbody>
    </table>
    </div>
  );
}

function formatDate(value?: string | null) {
  if (!value) return "Nao informado";
  return new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" }).format(new Date(`${value}T00:00:00Z`));
}

/**
 * Aplica a mascara de apresentacao do CNAE (ex.: "6201501" -> "6201-5/01"). Puramente
 * apresentacional — a persistencia sempre usa a representacao canonica em digitos puros (B2.8).
 * Codigos fora do formato de 7 digitos sao exibidos sem mascara (nunca lanca excecao).
 */
function formatCnaeCodigo(value?: string | null): string | null | undefined {
  if (!value) return value;
  return /^\d{7}$/.test(value) ? `${value.slice(0, 4)}-${value.slice(4, 5)}/${value.slice(5, 7)}` : value;
}
