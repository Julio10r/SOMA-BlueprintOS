import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PermissoesResumo } from "../components/PermissoesResumo";
import { usePermissionCatalog } from "../hooks/usePerfis";
import { getPerfil, PerfilAcessoNegadoError } from "../services/perfisApi";
import { statusDoPerfil, type Perfil } from "../types/perfilTypes";

export function PerfilDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acessoNegado, setAcessoNegado] = useState(false);
  const catalogo = usePermissionCatalog();

  useEffect(() => {
    if (!id) return;
    let ativo = true;
    setLoading(true);
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
        if (ativo) setLoading(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Detalhes do perfil</h1>
        <p>Visualização somente leitura das permissões atribuídas a este perfil.</p>
      </header>

      {(acessoNegado || catalogo.acessoNegado) && (
        <section className="card">
          <div className="notice notice-warn">Você não tem permissão para visualizar perfis.</div>
        </section>
      )}
      {error && <div className="notice notice-crit">{error}</div>}
      {loading && !acessoNegado && <div className="empty-state">Carregando perfil...</div>}

      {perfil && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Perfil</div>
              <h2>{perfil.nome}</h2>
            </div>
            <StatusBadge value={statusDoPerfil(perfil)} tone="situacao" />
          </div>
          <p>{perfil.descricao}</p>
          <div className="data-grid">
            <div className="field-readonly">
              <span>Usuários vinculados</span>
              <strong>{perfil.usuariosVinculados}</strong>
            </div>
            <div className="field-readonly">
              <span>Atualizado em</span>
              <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(perfil.atualizadoEm))}</strong>
            </div>
          </div>
          <div className="data-block">
            <div className="section-title">Permissoes</div>
            <PermissoesResumo permissoes={perfil.permissoes} catalogo={catalogo.permissoes} />
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
