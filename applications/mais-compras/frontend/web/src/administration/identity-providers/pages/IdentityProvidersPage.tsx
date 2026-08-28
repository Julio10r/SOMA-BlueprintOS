import { useState } from "react";
import { IdentityProviderForm } from "../components/IdentityProviderForm";
import { IdentityProviderTable } from "../components/IdentityProviderTable";
import { UnidadeNegocioSeletor } from "../components/UnidadeNegocioSeletor";
import { useIdentityProviders } from "../hooks/useIdentityProviders";
import { createIdentityProvider, updateIdentityProvider } from "../services/identityProvidersApi";
import type { IdentityProvider, IdentityProviderInput } from "../types/identityProviderTypes";

export function IdentityProvidersPage() {
  const [unidadeNegocioId, setUnidadeNegocioId] = useState<string | null>(null);
  const { providers, loading, error, reload, toggleStatus } = useIdentityProviders(unidadeNegocioId);
  const [editando, setEditando] = useState<IdentityProvider | null>(null);
  const [criando, setCriando] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(input: IdentityProviderInput) {
    if (!unidadeNegocioId) return;
    setSaving(true);
    setFormError(null);
    try {
      if (editando) {
        await updateIdentityProvider(unidadeNegocioId, editando.id, input);
      } else {
        await createIdentityProvider(unidadeNegocioId, input);
      }
      setEditando(null);
      setCriando(false);
      await reload();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Falha ao salvar Identity Provider.");
    } finally {
      setSaving(false);
    }
  }

  const mostrarFormulario = criando || Boolean(editando);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Identity Providers</h1>
        <p>Configuração de Identity Providers por Unidade de Negócio. Segredos nunca são exibidos após salvos.</p>
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
            <h2>Identity Providers cadastrados</h2>
            <button type="button" className="btn btn-primary" onClick={() => { setCriando(true); setEditando(null); }}>
              Novo Identity Provider
            </button>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}

          {loading ? (
            <div className="empty-state">Carregando Identity Providers...</div>
          ) : (
            <IdentityProviderTable
              providers={providers}
              onEditar={(provider) => { setEditando(provider); setCriando(false); }}
              onToggleStatus={toggleStatus}
            />
          )}
        </section>
      )}

      {mostrarFormulario && (
        <IdentityProviderForm
          provider={editando ?? undefined}
          error={formError}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => { setEditando(null); setCriando(false); setFormError(null); }}
        />
      )}
    </div>
  );
}
