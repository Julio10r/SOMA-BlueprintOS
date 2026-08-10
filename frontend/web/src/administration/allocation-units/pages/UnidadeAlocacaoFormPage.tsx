import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { UnidadeAlocacaoForm } from "../components/UnidadeAlocacaoForm";
import {
  createUnidadeAlocacao,
  getUnidadeAlocacao,
  updateUnidadeAlocacao
} from "../services/unidadesAlocacaoMockApi";
import type { UnidadeAlocacao, UnidadeAlocacaoInput } from "../types/unidadeAlocacaoTypes";

export function UnidadeAlocacaoFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [unidadeAlocacao, setUnidadeAlocacao] = useState<UnidadeAlocacao | null>(null);
  const [loadingUnidadeAlocacao, setLoadingUnidadeAlocacao] = useState(Boolean(id));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingUnidadeAlocacao(true);
    getUnidadeAlocacao(id).then((found) => {
      if (!found) {
        setError("Unidade de alocacao nao encontrada.");
        return;
      }
      setUnidadeAlocacao(found);
    }).finally(() => setLoadingUnidadeAlocacao(false));
  }, [id]);

  async function handleSubmit(input: UnidadeAlocacaoInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) {
        await updateUnidadeAlocacao(id, input);
      } else {
        await createUnidadeAlocacao(input);
      }
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar unidade de alocacao.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>{id ? "Editar unidade de alocacao" : "Nova unidade de alocacao"}</h1>
        <p>Unidades de Alocacao pertencem exclusivamente ao +Compras e nao sao integradas do ERP.</p>
      </header>

      {loadingUnidadeAlocacao ? (
        <div className="empty-state">Carregando unidade de alocacao...</div>
      ) : (
        <UnidadeAlocacaoForm
          unidadeAlocacao={unidadeAlocacao ?? undefined}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
