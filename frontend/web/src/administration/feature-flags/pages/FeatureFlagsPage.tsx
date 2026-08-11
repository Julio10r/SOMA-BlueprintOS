import { useState } from "react";
import { useUnidadesNegocio } from "../../business-units/hooks/useUnidadesNegocio";
import { FeatureFlagForm } from "../components/FeatureFlagForm";
import { FeatureFlagTable } from "../components/FeatureFlagTable";
import { useFeatureFlags } from "../hooks/useFeatureFlags";

export function FeatureFlagsPage() {
  const { flags, loading, error, criar, alterarStatus } = useFeatureFlags();
  const { unidadesNegocio } = useUnidadesNegocio();
  const [mostrarForm, setMostrarForm] = useState(false);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao do Sistema</div>
        <h1>Feature Flags</h1>
        <p>Habilite ou desabilite funcionalidades ja implementadas, por Unidade de Negocio, sem novo deploy.</p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Feature Flags</div>
            <h2>Flags cadastradas</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => setMostrarForm((v) => !v)}>
            Nova Feature Flag
          </button>
        </div>

        {mostrarForm && (
          <FeatureFlagForm
            onSalvar={async (input) => {
              await criar(input);
              setMostrarForm(false);
            }}
            onCancelar={() => setMostrarForm(false)}
          />
        )}

        {error && <div className="notice notice-crit">{error}</div>}

        {loading ? (
          <div className="empty-state">Carregando Feature Flags...</div>
        ) : (
          <FeatureFlagTable
            flags={flags}
            unidadesNegocio={unidadesNegocio}
            onAlterarStatus={(flagId, unidadeNegocioId, ativa) => {
              void alterarStatus(flagId, unidadeNegocioId, ativa);
            }}
          />
        )}
      </section>
    </div>
  );
}
