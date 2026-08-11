import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { listPerfis } from "../../profiles/services/perfisApi";
import type { Perfil } from "../../profiles/types/perfilTypes";
import { UsuarioForm } from "../components/UsuarioForm";
import { createUsuario, getUsuario, updateUsuario } from "../services/usuariosApi";
import type { Usuario, UsuarioInput } from "../types/userTypes";

export function UsuarioFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [perfisDisponiveis, setPerfisDisponiveis] = useState<Perfil[]>([]);
  const [loadingUsuario, setLoadingUsuario] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoadingUsuario(true);
    Promise.all([id ? getUsuario(id) : Promise.resolve(null), listPerfis().catch(() => [])])
      .then(([foundUsuario, perfis]) => {
        if (id && !foundUsuario) {
          setError("Usuario nao encontrado.");
          return;
        }
        setUsuario(foundUsuario);
        setPerfisDisponiveis(perfis);
      })
      .finally(() => setLoadingUsuario(false));
  }, [id]);

  async function handleSubmit(input: UsuarioInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) {
        await updateUsuario(id, input);
      } else {
        await createUsuario(input);
      }
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar usuario.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>{id ? "Editar usuario" : "Novo usuario"}</h1>
        <p>Vincule Perfis e Centros de Custo para definir o acesso efetivo do usuario ao +Compras.</p>
      </header>

      {loadingUsuario ? (
        <div className="empty-state">Carregando usuario...</div>
      ) : (
        <UsuarioForm
          usuario={usuario ?? undefined}
          perfisDisponiveis={perfisDisponiveis}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
