import { useState } from "react";
import { UnidadeNegocioSeletor } from "../../identity-providers/components/UnidadeNegocioSeletor";
import { RegraWorkflowForm } from "../components/RegraWorkflowForm";
import { RegraWorkflowTable } from "../components/RegraWorkflowTable";
import { useRegrasWorkflow } from "../hooks/useRegrasWorkflow";
import { createRegraWorkflow, updateRegraWorkflow } from "../services/regrasWorkflowApi";
import type { RegraWorkflow, RegraWorkflowInput } from "../types/regraWorkflowTypes";

export function RegrasWorkflowPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { regras, loading, error, reload, toggleStatus } = useRegrasWorkflow(unidadeNegocioId);
  const [editando, setEditando] = useState<RegraWorkflow | null>(null);
  const [criando, setCriando] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: RegraWorkflowInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      if (editando) {
        await updateRegraWorkflow(unidadeNegocioId, editando.id, input);
      } else {
        await createRegraWorkflow(unidadeNegocioId, input);
      }
      setEditando(null);
      setCriando(false);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Regra de Workflow.");
    } finally {
      setSaving(false);
    }
  }

  const mostrarFormulario = criando || Boolean(editando);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Regras de Workflow</h1>
        <p>Cadastro de Regras de Workflow por Unidade de Negocio. Nenhum motor de execucao e acionado nesta sprint.</p>
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
            <h2>Regras de Workflow cadastradas</h2>
            <button type="button" className="btn btn-primary" onClick={() => { setCriando(true); setEditando(null); }}>
              Nova Regra de Workflow
            </button>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Regras de Workflow...</div>
          ) : (
            <RegraWorkflowTable
              regras={regras}
              onEditar={(regra) => { setEditando(regra); setCriando(false); }}
              onToggleStatus={toggleStatus}
            />
          )}
        </section>
      )}

      {mostrarFormulario && (
        <RegraWorkflowForm
          regra={editando ?? undefined}
          error={formError}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => { setEditando(null); setCriando(false); setFormError(null); }}
        />
      )}
    </div>
  );
}
