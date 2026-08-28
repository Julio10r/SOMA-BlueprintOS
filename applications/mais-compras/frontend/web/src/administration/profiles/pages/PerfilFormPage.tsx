import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { PerfilForm } from "../components/PerfilForm";
import { usePermissionCatalog } from "../hooks/usePerfis";
import { createPerfil, getPerfil, PerfilAcessoNegadoError, updatePerfil } from "../services/perfisApi";
import type { Perfil, PerfilInput } from "../types/perfilTypes";

export function PerfilFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  const [loadingPerfil, setLoadingPerfil] = useState(Boolean(id));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [acessoNegado, setAcessoNegado] = useState(false);
  const catalogo = usePermissionCatalog();

  useEffect(() => {
    if (!id) return;
    let ativo = true;
    setLoadingPerfil(true);
    (async () => {
      try {
        const encontrado = await getPerfil(id);
        if (!ativo) return;
        if (!encontrado) setError("Perfil não encontrado.");
        else setPerfil(encontrado);
      } catch (err) {
        if (!ativo) return;
        if (err instanceof PerfilAcessoNegadoError) setAcessoNegado(true);
        else setError(err instanceof Error ? err.message : "Falha ao carregar perfil.");
      } finally {
        if (ativo) setLoadingPerfil(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, [id]);

  async function handleSubmit(input: PerfilInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) await updatePerfil(id, input);
      else await createPerfil(input);
      navigate("..", { relative: "path" });
    } catch (err) {
      if (err instanceof PerfilAcessoNegadoError) setAcessoNegado(true);
      else setError(err instanceof Error ? err.message : "Falha ao salvar perfil.");
    } finally {
      setSaving(false);
    }
  }

  const carregando = loadingPerfil || catalogo.loading;
  const negado = acessoNegado || catalogo.acessoNegado;

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>{id ? "Editar perfil" : "Novo perfil"}</h1>
        <p>Perfis definem, em conjunto, o acesso efetivo de um usuário ao +Compras.</p>
      </header>

      {negado ? (
        <section className="card">
          <div className="notice notice-warn">
            Voce nao tem permissao para gerenciar perfis.
          </div>
        </section>
      ) : carregando ? (
        <div className="empty-state">Carregando perfil...</div>
      ) : (
        <PerfilForm
          perfil={perfil ?? undefined}
          permissoes={catalogo.permissoes}
          error={error ?? catalogo.error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
