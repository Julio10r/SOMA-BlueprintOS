import { FormEvent, useState } from "react";
import type {
  CentroCusto,
  CentroCustoUpdateInput,
  UnidadeAlocacaoParaVinculo,
  UnidadeAlocacaoVinculoResumo
} from "../types/centroCustoTypes";

/**
 * Edicao de Centro de Custo: separacao visual clara entre "Dados do ERP"
 * (somente leitura) e "Dados +Compras" (editaveis), conforme ADR-0020
 * item 2/3. CodigoErp, DescricaoErp e UnidadeNegocioId nunca sao
 * editaveis nesta tela — nao ha nenhum campo de formulario associado a
 * eles.
 *
 * O vinculo com Unidade de Alocacao (O1.9, ADR-0020 item 6) e real: a
 * selecao usa o catalogo real de Unidades de Alocacao (O1.8) e permite
 * escolher, entre as vinculadas, qual e a padrao. E um formulario
 * separado do de metadados +Compras — os dois sao salvos
 * independentemente, cada um com seu proprio estado de loading/erro.
 */
export function CentroCustoForm({
  centroCusto, error, loading, onSubmit, onCancel,
  catalogoUnidadesAlocacao, vinculosAtuais, savingVinculos, errorVinculos, onSubmitVinculos
}: {
  centroCusto: CentroCusto;
  error: string | null;
  loading: boolean;
  onSubmit: (input: CentroCustoUpdateInput) => void;
  onCancel: () => void;
  catalogoUnidadesAlocacao: UnidadeAlocacaoParaVinculo[];
  vinculosAtuais: UnidadeAlocacaoVinculoResumo[];
  savingVinculos: boolean;
  errorVinculos: string | null;
  onSubmitVinculos: (unidadeAlocacaoIds: string[], padraoId: string | null) => void;
}) {
  const [descricaoMaisCompras, setDescricaoMaisCompras] = useState(centroCusto.descricaoMaisCompras ?? "");
  const [ativoNoMaisCompras, setAtivoNoMaisCompras] = useState(centroCusto.ativoNoMaisCompras);

  const [selecionadas, setSelecionadas] = useState<string[]>(vinculosAtuais.map((v) => v.id));
  const [padraoId, setPadraoId] = useState<string | null>(vinculosAtuais.find((v) => v.padrao)?.id ?? null);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({
      descricaoMaisCompras: descricaoMaisCompras.trim() ? descricaoMaisCompras.trim() : undefined,
      ativoNoMaisCompras
    });
  }

  function toggleSelecionada(id: string) {
    setSelecionadas((atual) => {
      const proxima = atual.includes(id) ? atual.filter((x) => x !== id) : [...atual, id];
      if (!proxima.includes(padraoId ?? "")) setPadraoId(null);
      return proxima;
    });
  }

  function handleSubmitVinculos(event: FormEvent) {
    event.preventDefault();
    onSubmitVinculos(selecionadas, padraoId);
  }

  return (
    <>
      <form className="card form-card" onSubmit={handleSubmit}>
        <div className="card-heading">
          <h2>Editar centro de custo</h2>
        </div>

        <div className="notice notice-warn">
          Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        <div className="data-block">
          <div className="section-title">Dados do ERP (somente leitura)</div>
          <div className="data-grid">
            <div className="field-readonly">
              <span>Código Centro de Custo</span>
              <strong>{centroCusto.codigoErp}</strong>
            </div>
            <div className="field-readonly">
              <span>Descrição ERP</span>
              <strong>{centroCusto.descricaoErp}</strong>
            </div>
            <div className="field-readonly">
              <span>Unidade de Negocio</span>
              <strong>{centroCusto.unidadeNegocioId}</strong>
            </div>
          </div>
        </div>

        <div className="data-block">
          <div className="section-title">Dados +Compras (editáveis)</div>

          <label>
            Descrição +Compras
            <input
              value={descricaoMaisCompras}
              onChange={(event) => setDescricaoMaisCompras(event.target.value)}
              placeholder="Opcional - não substitui a Descrição ERP"
              disabled={loading}
            />
          </label>

          <label className="field-readonly">
            <input
              type="checkbox"
              checked={ativoNoMaisCompras}
              onChange={(event) => setAtivoNoMaisCompras(event.target.checked)}
              disabled={loading}
            />
            <strong>Ativo no +Compras</strong>
            <span>Controla apenas o uso deste centro de custo no +Compras; não altera o cadastro no ERP.</span>
          </label>
        </div>

        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? "Salvando..." : "Salvar"}
          </button>
        </div>
      </form>

      <form className="card form-card" onSubmit={handleSubmitVinculos}>
        <div className="card-heading">
          <h2>Unidades de Alocação vinculadas</h2>
        </div>

        <div className="notice notice-warn">
          Selecione as Unidades de Alocação permitidas para este Centro de Custo e, entre elas, qual é a
          padrão. Não é permitido selecionar Unidade de Alocação fora do vínculo configurado (ADR-0020, item 6).
        </div>

        {errorVinculos && <div className="notice notice-crit">{errorVinculos}</div>}

        {catalogoUnidadesAlocacao.length === 0 ? (
          <div className="empty-state">Nenhuma unidade de alocação cadastrada.</div>
        ) : (
          <div className="data-grid">
            {catalogoUnidadesAlocacao.map((unidade) => {
              const marcada = selecionadas.includes(unidade.id);
              return (
                <label key={unidade.id} className="field-readonly">
                  <input
                    type="checkbox"
                    checked={marcada}
                    onChange={() => toggleSelecionada(unidade.id)}
                    disabled={savingVinculos}
                  />
                  <strong>{unidade.nome}</strong>
                  {!unidade.ativo && <span> (inativa)</span>}
                  <label>
                    <input
                      type="radio"
                      name="unidadeAlocacaoPadrao"
                      checked={padraoId === unidade.id}
                      onChange={() => setPadraoId(unidade.id)}
                      disabled={savingVinculos || !marcada}
                    />
                    Padrao
                  </label>
                </label>
              );
            })}
          </div>
        )}

        <div className="actions">
          <button type="submit" className="btn btn-primary" disabled={savingVinculos}>
            {savingVinculos ? "Salvando..." : "Salvar vinculo"}
          </button>
        </div>
      </form>
    </>
  );
}
