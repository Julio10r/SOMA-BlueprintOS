import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmToggleAtivoUsuarioModal } from "../components/ConfirmToggleAtivoUsuarioModal";
import { UsuarioTable } from "../components/UsuarioTable";
import { useUsuarios } from "../hooks/useUsuarios";
import type { Usuario } from "../types/userTypes";

/**
 * Listagem de Usuarios (Gestao de Usuarios). A partir da O1.6, consome a API real
 * (`administracao/usuarios`), substituindo o mock em memoria da fundacao visual
 * (Sprint O1.3.2) — mesmo padrao de integracao de `administration/profiles` (O1.5).
 *
 * Usuarios nunca sao excluidos fisicamente: permanecem auditaveis e apenas
 * transitam entre Ativo/Inativo, mesmo padrao de Filiais, Centros de Custo
 * e Unidades de Alocacao.
 */
export function UsuariosPage() {
  const navigate = useNavigate();
  const { usuarios, loading, error, acessoNegado, toggleAtivo } = useUsuarios();
  const [usuarioParaAlternar, setUsuarioParaAlternar] = useState<Usuario | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [erroToggle, setErroToggle] = useState<string | null>(null);

  async function confirmarToggleAtivo() {
    if (!usuarioParaAlternar) return;
    setSalvando(true);
    setErroToggle(null);
    try {
      await toggleAtivo(usuarioParaAlternar);
      setUsuarioParaAlternar(null);
    } catch (err) {
      setErroToggle(err instanceof Error ? err.message : "Falha ao alterar o status do usuario.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Gestao de Usuarios</h1>
        <p>Usuarios recebem acesso ao +Compras por meio de Perfis e Centros de Custo. Nunca ha permissao individual.</p>
      </header>

      {acessoNegado ? (
        <section className="card">
          <div className="notice notice-crit">Voce nao tem permissao para acessar a Gestao de Usuarios.</div>
        </section>
      ) : (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Usuarios</div>
              <h2>Usuarios cadastrados</h2>
            </div>
            <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
              Novo usuario
            </button>
          </div>

          {error ? (
            <div className="notice notice-crit">{error}</div>
          ) : loading ? (
            <div className="empty-state">Carregando usuarios...</div>
          ) : (
            <UsuarioTable
              usuarios={usuarios}
              onVisualizar={(usuario) => navigate(usuario.id)}
              onEditar={(usuario) => navigate(`${usuario.id}/editar`)}
              onToggleAtivo={(usuario) => {
                setErroToggle(null);
                setUsuarioParaAlternar(usuario);
              }}
            />
          )}
        </section>
      )}

      {usuarioParaAlternar && (
        <ConfirmToggleAtivoUsuarioModal
          usuario={usuarioParaAlternar}
          error={erroToggle}
          loading={salvando}
          onConfirm={confirmarToggleAtivo}
          onCancel={() => setUsuarioParaAlternar(null)}
        />
      )}
    </div>
  );
}
