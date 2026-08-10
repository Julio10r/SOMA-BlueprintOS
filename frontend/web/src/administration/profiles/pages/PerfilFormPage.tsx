import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { PerfilForm } from "../components/PerfilForm";
import { createPerfil, getPerfil, updatePerfil } from "../services/perfisMockApi";
import type { Perfil, PerfilInput } from "../types/perfilTypes";

export function PerfilFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  const [loadingPerfil, setLoadingPerfil] = useState(Boolean(id));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingPerfil(true);
    getPerfil(id).then((found) => {
      if (!found) {
        setError("Perfil nao encontrado.");
        return;
      }
      setPerfil(found);
    }).finally(() => setLoadingPerfil(false));
  }, [id]);

  async function handleSubmit(input: PerfilInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) {
        await updatePerfil(id, input);
      } else {
        await createPerfil(input);
      }
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar perfil.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>{id ? "Editar perfil" : "Novo perfil"}</h1>
        <p>Perfis definem, em conjunto, o acesso efetivo de um usuario ao +Compras.</p>
      </header>

      {loadingPerfil ? (
        <div className="empty-state">Carregando perfil...</div>
      ) : (
        <PerfilForm
          perfil={perfil ?? undefined}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
