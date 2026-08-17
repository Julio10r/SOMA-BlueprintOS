import { StatusBadge } from "../../../shared/components/StatusBadge";
import { SituacaoCadastralBadge } from "./SituacaoCadastralBadge";
import type { ConsultaCnpjResultado, FornecedorCampoDivergencia } from "../types/linxSupplierContract";

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
        <DataGrid title="Identificacao" items={[
          ["Cnpj_Cpf", consulta.cnpj_Cpf],
          ["RazaoSocial", consulta.razaoSocial],
          ["NomeFantasia", consulta.nomeFantasia],
          ["TipoPessoa", consulta.tipoPessoa]
        ]} />
        <DataGrid title="CNAE principal" items={[
          ["Codigo", formatCnaeCodigo(consulta.cnaePrincipalCodigo)],
          ["Descricao", consulta.cnaePrincipalDescricao]
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
