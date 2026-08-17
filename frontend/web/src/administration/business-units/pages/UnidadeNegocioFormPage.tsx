import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { UnidadeNegocioForm } from "../components/UnidadeNegocioForm";
import { createUnidadeNegocio, listUnidadesNegocio, updateUnidadeNegocio } from "../services/unidadesNegocioApi";
import type { UnidadeNegocio, UnidadeNegocioCriarInput, UnidadeNegocioEditarInput } from "../types/unidadeNegocioTypes";

export function UnidadeNegocioFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [unidadeNegocio, setUnidadeNegocio] = useState<UnidadeNegocio | null>(null);
  const [loadingUnidadeNegocio, setLoadingUnidadeNegocio] = useState(Boolean(id));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingUnidadeNegocio(true);
    listUnidadesNegocio()
      .then((todas) => {
        const encontrada = todas.find((u) => u.id === id);
        if (!encontrada) {
          setError("Unidade de Negócio não encontrada.");
          return;
        }
        setUnidadeNegocio(encontrada);
      })
      .finally(() => setLoadingUnidadeNegocio(false));
  }, [id]);

  async function handleSubmit(input: UnidadeNegocioCriarInput | UnidadeNegocioEditarInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) {
        await updateUnidadeNegocio(id, input as UnidadeNegocioEditarInput);
      } else {
        await createUnidadeNegocio(input as UnidadeNegocioCriarInput);
      }
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar unidade de negócio.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>{id ? "Editar unidade de negócio" : "Nova unidade de negócio"}</h1>
      </header>

      {loadingUnidadeNegocio ? (
        <div className="empty-state">Carregando unidade de negócio...</div>
      ) : (
        <UnidadeNegocioForm
          unidadeNegocio={unidadeNegocio ?? undefined}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
