import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { listCentrosCusto } from "../../cost-centers/services/centrosCustoApi";
import type { CentroCusto } from "../../cost-centers/types/centroCustoTypes";
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
  const [centrosCustoDisponiveis, setCentrosCustoDisponiveis] = useState<CentroCusto[]>([]);
  const [loadingUsuario, setLoadingUsuario] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoadingUsuario(true);
    Promise.all([
      id ? getUsuario(id) : Promise.resolve(null),
      listPerfis().catch(() => []),
      listCentrosCusto().catch(() => [])
    ])
      .then(([foundUsuario, perfis, centrosCusto]) => {
        if (id && !foundUsuario) {
          setError("Usuário não encontrado.");
          return;
        }
        setUsuario(foundUsuario);
        setPerfisDisponiveis(perfis);
        setCentrosCustoDisponiveis(centrosCusto);
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
      setError(err instanceof Error ? err.message : "Falha ao salvar usuário.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>{id ? "Editar usuário" : "Novo usuário"}</h1>
        <p>Vincule Perfis e Centros de Custo para definir o acesso efetivo do usuário ao +Compras.</p>
      </header>

      {loadingUsuario ? (
        <div className="empty-state">Carregando usuário...</div>
      ) : (
        <UsuarioForm
          usuario={usuario ?? undefined}
          perfisDisponiveis={perfisDisponiveis}
          centrosCustoDisponiveis={centrosCustoDisponiveis}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      )}
    </div>
  );
}
