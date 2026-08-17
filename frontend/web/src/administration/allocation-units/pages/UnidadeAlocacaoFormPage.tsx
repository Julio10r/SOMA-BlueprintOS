import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { UnidadeAlocacaoForm } from "../components/UnidadeAlocacaoForm";
import {
  createUnidadeAlocacao,
  getUnidadeAlocacao,
  updateUnidadeAlocacao
} from "../services/unidadesAlocacaoApi";
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
        setError("Unidade de alocação não encontrada.");
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
      setError(err instanceof Error ? err.message : "Falha ao salvar unidade de alocação.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>{id ? "Editar unidade de alocação" : "Nova unidade de alocação"}</h1>
        <p>Unidades de Alocação pertencem exclusivamente ao +Compras e não são integradas do ERP.</p>
      </header>

      {loadingUnidadeAlocacao ? (
        <div className="empty-state">Carregando unidade de alocação...</div>
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
