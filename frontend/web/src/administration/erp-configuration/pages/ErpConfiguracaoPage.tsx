import { useState } from "react";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { UnidadeNegocioSeletor } from "../../identity-providers/components/UnidadeNegocioSeletor";
import { ConfiguracaoErpForm } from "../components/ConfiguracaoErpForm";
import { useConfiguracaoErp } from "../hooks/useConfiguracaoErp";
import { salvarConfiguracaoErp } from "../services/configuracaoErpApi";
import type { ConfiguracaoErpInput } from "../types/configuracaoErpTypes";

export function ErpConfiguracaoPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { configuracao, loading, error, reload, toggleStatus } = useConfiguracaoErp(unidadeNegocioId);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: ConfiguracaoErpInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      await salvarConfiguracaoErp(unidadeNegocioId, input);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Configuração de ERP.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Configuração de ERP</h1>
        <p>Registro de configuração de ERP por Unidade de Negócio. Segredos nunca são exibidos após salvos.</p>
      </header>

      <section className="card">
        <UnidadeNegocioSeletor value={unidadeNegocioId} onChange={setUnidadeNegocioId} />
      </section>

      {unidadeNegocioId && (
        <>
          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Configuração de ERP...</div>
          ) : !configuracao ? (
            <div className="empty-state">Nenhuma Configuração de ERP cadastrada para esta Unidade de Negócio.</div>
          ) : (
            <section className="card">
              <div className="card-heading">
                <h2>{configuracao.sistemaErp}</h2>
                <StatusBadge value={configuracao.status} tone="situacao" />
              </div>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={toggleStatus}>
                  {configuracao.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </section>
          )}

          <ConfiguracaoErpForm configuracao={configuracao} error={formError} loading={saving} onSubmit={handleSubmit} />
        </>
      )}
    </div>
  );
}
