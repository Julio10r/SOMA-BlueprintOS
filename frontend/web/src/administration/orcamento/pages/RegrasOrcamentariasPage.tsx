import { useState } from "react";
import { UnidadeNegocioSeletor } from "../../identity-providers/components/UnidadeNegocioSeletor";
import { RegraOrcamentariaForm } from "../components/RegraOrcamentariaForm";
import { RegraOrcamentariaTable } from "../components/RegraOrcamentariaTable";
import { useRegrasOrcamentarias } from "../hooks/useRegrasOrcamentarias";
import { createRegraOrcamentaria, updateRegraOrcamentaria } from "../services/regrasOrcamentariasApi";
import type { RegraOrcamentaria, RegraOrcamentariaInput } from "../types/regraOrcamentariaTypes";

export function RegrasOrcamentariasPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { regras, loading, error, reload, toggleStatus } = useRegrasOrcamentarias(unidadeNegocioId);
  const [editando, setEditando] = useState<RegraOrcamentaria | null>(null);
  const [criando, setCriando] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: RegraOrcamentariaInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      if (editando) {
        await updateRegraOrcamentaria(unidadeNegocioId, editando.id, input);
      } else {
        await createRegraOrcamentaria(unidadeNegocioId, input);
      }
      setEditando(null);
      setCriando(false);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Regra Orcamentaria.");
    } finally {
      setSaving(false);
    }
  }

  const mostrarFormulario = criando || Boolean(editando);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Regras Orcamentarias</h1>
        <p>Cadastro de Regras Orcamentarias por Unidade de Negocio. Apenas o cadastro: nenhuma reserva contabil, consumo real ou bloqueio operacional acontece nesta sprint.</p>
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
            <h2>Regras Orcamentarias cadastradas</h2>
            <button type="button" className="btn btn-primary" onClick={() => { setCriando(true); setEditando(null); }}>
              Nova Regra Orcamentaria
            </button>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Regras Orcamentarias...</div>
          ) : (
            <RegraOrcamentariaTable
              regras={regras}
              onEditar={(regra) => { setEditando(regra); setCriando(false); }}
              onToggleStatus={toggleStatus}
            />
          )}
        </section>
      )}

      {mostrarFormulario && (
        <RegraOrcamentariaForm
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
