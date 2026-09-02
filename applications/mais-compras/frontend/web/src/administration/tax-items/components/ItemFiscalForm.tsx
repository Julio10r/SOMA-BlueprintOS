import { FormEvent, ReactNode, useState } from "react";
import type { OpcaoApoio } from "../hooks/useItemFiscalOpcoesDeApoio";
import type { ItemFiscal, ItemFiscalCreateInput, ItemFiscalUpdateInput } from "../types/itemFiscalTypes";

type Aba = "dados-gerais" | "referencias-fornecedor";

/**
 * Cadastro/edição de Item Fiscal (B3 - Bloco 3/4, Discovery homologado). A partir do Bloco 4, o
 * formulário passa a ter dois grupos funcionais distintos o bastante para justificar abas
 * (CadFormFactory — abas autorizadas quando produzem a melhor organização visual): "Dados Gerais" (os
 * mesmos 4 campos do Bloco 3) e "Referências por Fornecedor" (Bloco 4).
 *
 * A aba de Referências só faz sentido para um Item Fiscal já persistido (a referência precisa de um
 * `itemFiscalId` real) — durante a criação ela fica desabilitada, com dica para salvar o item primeiro.
 *
 * `codigo` só aparece no formulário de criação — imutável após criado, nunca editável aqui.
 *
 * Conta Contábil e Unidade de Medida são sempre SELECIONADAS entre os cadastros de apoio já ativos
 * (nunca digitação livre) — mesma regra homologada dos Blocos 1/2. O backend valida de qualquer forma
 * (nunca confiar apenas na UI).
 */
export function ItemFiscalForm({ item, opcoes, error, loading, onSubmit, onCancel, referenciasFornecedor }: {
  item?: ItemFiscal;
  opcoes: { contasContabeis: OpcaoApoio[]; unidadesMedida: OpcaoApoio[]; loading: boolean; error: string | null };
  error: string | null;
  loading: boolean;
  onSubmit: (input: ItemFiscalCreateInput | ItemFiscalUpdateInput) => void;
  onCancel: () => void;
  /** Conteúdo da aba "Referências por Fornecedor" — omitido/`undefined` durante a criação. */
  referenciasFornecedor?: ReactNode;
}) {
  const [aba, setAba] = useState<Aba>("dados-gerais");
  const [codigo, setCodigo] = useState(item?.codigo ?? "");
  const [descricao, setDescricao] = useState(item?.descricao ?? "");
  const [unidadeMedidaCodigoErp, setUnidadeMedidaCodigoErp] = useState(item?.unidadeMedidaCodigoErp ?? "");
  const [contaContabilCodigoErp, setContaContabilCodigoErp] = useState(item?.contaContabilCodigoErp ?? "");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (item) {
      onSubmit({ descricao, unidadeMedidaCodigoErp, contaContabilCodigoErp } satisfies ItemFiscalUpdateInput);
    } else {
      onSubmit({ codigo, descricao, unidadeMedidaCodigoErp, contaContabilCodigoErp } satisfies ItemFiscalCreateInput);
    }
  }

  // Garante que a opção já selecionada apareça mesmo se, por algum motivo, não estiver mais entre as
  // ativas (ex.: item editado depois que a conta/unidade foi inativada) — nunca esconde silenciosamente.
  const contasComSelecionada = incluirSelecionada(opcoes.contasContabeis, contaContabilCodigoErp);
  const unidadesComSelecionada = incluirSelecionada(opcoes.unidadesMedida, unidadeMedidaCodigoErp);

  return (
    <div className="card form-card">
      <div className="card-heading">
        <h2>{item ? "Editar item fiscal" : "Novo item fiscal"}</h2>
      </div>

      <div className="form-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={aba === "dados-gerais"}
          className={`form-tab${aba === "dados-gerais" ? " form-tab-active" : ""}`}
          onClick={() => setAba("dados-gerais")}
        >
          Dados gerais
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={aba === "referencias-fornecedor"}
          className={`form-tab${aba === "referencias-fornecedor" ? " form-tab-active" : ""}`}
          onClick={() => setAba("referencias-fornecedor")}
          disabled={!item}
          title={item ? undefined : "Salve o item fiscal primeiro para incluir referências de fornecedor"}
        >
          Referências por fornecedor
        </button>
      </div>

      {aba === "dados-gerais" ? (
        <form onSubmit={handleSubmit}>
          <div className="notice notice-warn">
            A granularidade do Item Fiscal é decisão da área de Compras — o +Compras não impõe item genérico
            ou específico, marca ou modelo.
          </div>

          {error && <div className="notice notice-crit">{error}</div>}
          {opcoes.error && <div className="notice notice-crit">{opcoes.error}</div>}

          {!item && (
            <label>
              Código
              <input value={codigo} onChange={(event) => setCodigo(event.target.value)} required disabled={loading} />
            </label>
          )}

          {item && (
            <div className="field-readonly">
              <span>Código</span>
              <strong>{item.codigo}</strong>
            </div>
          )}

          <label>
            Descrição
            <input value={descricao} onChange={(event) => setDescricao(event.target.value)} required disabled={loading} />
          </label>

          <label>
            Unidade
            <select
              value={unidadeMedidaCodigoErp}
              onChange={(event) => setUnidadeMedidaCodigoErp(event.target.value)}
              required
              disabled={loading || opcoes.loading}
            >
              <option value="" disabled>
                Selecione uma unidade
              </option>
              {unidadesComSelecionada.map((opcao) => (
                <option key={opcao.codigo} value={opcao.codigo}>
                  {opcao.descricao} ({opcao.codigo})
                </option>
              ))}
            </select>
          </label>

          <label>
            Conta Contábil
            <select
              value={contaContabilCodigoErp}
              onChange={(event) => setContaContabilCodigoErp(event.target.value)}
              required
              disabled={loading || opcoes.loading}
            >
              <option value="" disabled>
                Selecione uma conta contábil
              </option>
              {contasComSelecionada.map((opcao) => (
                <option key={opcao.codigo} value={opcao.codigo}>
                  {opcao.descricao} ({opcao.codigo})
                </option>
              ))}
            </select>
          </label>

          <div className="actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
              Cancelar
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      ) : (
        referenciasFornecedor
      )}
    </div>
  );
}

function incluirSelecionada(opcoes: OpcaoApoio[], codigoSelecionado: string): OpcaoApoio[] {
  if (!codigoSelecionado || opcoes.some((o) => o.codigo === codigoSelecionado)) return opcoes;
  return [...opcoes, { codigo: codigoSelecionado, descricao: codigoSelecionado }];
}
