import { useState } from "react";
import { UnidadeNegocioSeletor } from "../../identity-providers/components/UnidadeNegocioSeletor";
import { ConfiguracaoNotificacaoForm } from "../components/ConfiguracaoNotificacaoForm";
import { useConfiguracaoNotificacao } from "../hooks/useConfiguracaoNotificacao";
import { salvarConfiguracaoNotificacao } from "../services/configuracaoNotificacaoApi";
import type { ConfiguracaoNotificacaoInput } from "../types/configuracaoNotificacaoTypes";

export function ConfiguracaoNotificacaoPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { configuracao, loading, error, reload } = useConfiguracaoNotificacao(unidadeNegocioId);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: ConfiguracaoNotificacaoInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      await salvarConfiguracaoNotificacao(unidadeNegocioId, input);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Configuração de Notificações.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Configuração de Notificações</h1>
        <p>Registro de configuração do canal de e-mail por Unidade de Negócio. Sem envio real de e-mail nesta sprint.</p>
      </header>

      <section className="card">
        <UnidadeNegocioSeletor value={unidadeNegocioId} onChange={setUnidadeNegocioId} />
      </section>

      {unidadeNegocioId && (
        <>
          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Configuração de Notificações...</div>
          ) : !configuracao ? (
            <div className="empty-state">Nenhuma Configuração de Notificações cadastrada para esta Unidade de Negócio.</div>
          ) : null}

          {!loading && (
            <ConfiguracaoNotificacaoForm configuracao={configuracao} error={formError} loading={saving} onSubmit={handleSubmit} />
          )}
        </>
      )}
    </div>
  );
}
