import { useState } from "react";
import { UnidadeNegocioSeletor } from "../../identity-providers/components/UnidadeNegocioSeletor";
import { AlcadaAprovacaoForm } from "../components/AlcadaAprovacaoForm";
import { AlcadaAprovacaoTable } from "../components/AlcadaAprovacaoTable";
import { useAlcadasAprovacao } from "../hooks/useAlcadasAprovacao";
import { createAlcadaAprovacao, updateAlcadaAprovacao } from "../services/alcadasAprovacaoApi";
import type { AlcadaAprovacao, AlcadaAprovacaoInput } from "../types/alcadaAprovacaoTypes";

export function AlcadasAprovacaoPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { alcadas, loading, error, reload, toggleStatus } = useAlcadasAprovacao(unidadeNegocioId);
  const [editando, setEditando] = useState<AlcadaAprovacao | null>(null);
  const [criando, setCriando] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: AlcadaAprovacaoInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      if (editando) {
        await updateAlcadaAprovacao(unidadeNegocioId, editando.id, input);
      } else {
        await createAlcadaAprovacao(unidadeNegocioId, input);
      }
      setEditando(null);
      setCriando(false);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Alçada de Aprovação.");
    } finally {
      setSaving(false);
    }
  }

  const mostrarFormulario = criando || Boolean(editando);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Alçadas de Aprovação</h1>
        <p>Cadastro de Alçadas de Aprovação por Unidade de Negócio. Nenhum motor de avaliação/execução é acionado nesta sprint.</p>
      </header>

      <section className="card">
        <UnidadeNegocioSeletor
          value={unidadeNegocioId}
          onChange={(id) => {
            setUnidadeNegocioId(id);
            setEditando(null);
            setCriando(false);
          }}
        />
      </section>

      {unidadeNegocioId && (
        <section className="card">
          <div className="card-heading">
            <h2>Alçadas de Aprovação cadastradas</h2>
            <button type="button" className="btn btn-primary" onClick={() => { setCriando(true); setEditando(null); }}>
              Nova Alçada de Aprovação
            </button>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Alçadas de Aprovação...</div>
          ) : (
            <AlcadaAprovacaoTable
              alcadas={alcadas}
              onEditar={(alcada) => { setEditando(alcada); setCriando(false); }}
              onToggleStatus={toggleStatus}
            />
          )}
        </section>
      )}

      {mostrarFormulario && unidadeNegocioId && (
        <AlcadaAprovacaoForm
          unidadeNegocioId={unidadeNegocioId}
          alcada={editando ?? undefined}
          error={formError}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => { setEditando(null); setCriando(false); setFormError(null); }}
        />
      )}
    </div>
  );
}
