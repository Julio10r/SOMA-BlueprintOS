import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PermissoesResumo } from "../components/PermissoesResumo";
import { getPerfil } from "../services/perfisMockApi";
import type { Perfil } from "../types/perfilTypes";

export function PerfilDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getPerfil(id).then((found) => {
      if (!found) {
        setError("Perfil nao encontrado.");
        return;
      }
      setPerfil(found);
    }).finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Detalhes do perfil</h1>
        <p>Visualizacao somente leitura das permissoes atribuidas a este perfil.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando perfil...</div>}

      {perfil && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{perfil.unidadeNegocio}</div>
              <h2>{perfil.nome}</h2>
            </div>
            <StatusBadge value={perfil.status} tone="situacao" />
          </div>
          <p>{perfil.descricao}</p>
          <div className="data-grid">
            <div className="field-readonly">
              <span>Usuarios vinculados</span>
              <strong>{perfil.usuariosVinculados}</strong>
            </div>
            <div className="field-readonly">
              <span>Atualizado em</span>
              <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(perfil.atualizadoEm))}</strong>
            </div>
          </div>
          <div className="data-block">
            <div className="section-title">Permissoes</div>
            <PermissoesResumo permissoes={perfil.permissoes} />
          </div>
          <div className="actions">
            <button type="button" className="btn btn-secondary" onClick={() => navigate("..", { relative: "path" })}>
              Voltar
            </button>
            <button type="button" className="btn btn-primary" onClick={() => navigate("editar", { relative: "path" })}>
              Editar
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
