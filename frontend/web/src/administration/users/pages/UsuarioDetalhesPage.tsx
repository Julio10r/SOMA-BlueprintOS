import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { listCentrosCusto } from "../../cost-centers/services/centrosCustoApi";
import type { CentroCusto } from "../../cost-centers/types/centroCustoTypes";
import { listPerfis } from "../../profiles/services/perfisApi";
import type { Perfil } from "../../profiles/types/perfilTypes";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PerfisResumo } from "../components/PerfisResumo";
import { getUsuario } from "../services/usuariosApi";
import { statusDoUsuario, type Usuario } from "../types/userTypes";

export function UsuarioDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [perfis, setPerfis] = useState<Perfil[]>([]);
  const [centrosCusto, setCentrosCusto] = useState<CentroCusto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    Promise.all([getUsuario(id), listPerfis().catch(() => []), listCentrosCusto().catch(() => [])])
      .then(([found, todosPerfis, todosCentrosCusto]) => {
        if (!found) {
          setError("Usuario nao encontrado.");
          return;
        }
        setUsuario(found);
        setPerfis(todosPerfis);
        setCentrosCusto(todosCentrosCusto);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const centrosCustoVinculados = usuario
    ? centrosCusto.filter((centroCusto) => usuario.centrosCusto.includes(centroCusto.id))
    : [];

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Detalhes do usuario</h1>
        <p>Visualizacao somente leitura do acesso deste usuario ao +Compras.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando usuario...</div>}

      {usuario && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Usuario</div>
              <h2>{usuario.nome}</h2>
            </div>
            <StatusBadge value={statusDoUsuario(usuario)} tone="situacao" />
          </div>
          <p>{usuario.email}</p>
          <div className="data-grid">
            <div className="field-readonly">
              <span>Atualizado em</span>
              <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(usuario.atualizadoEm))}</strong>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Perfis e permissoes herdadas</div>
            <PerfisResumo perfis={usuario.perfis} catalogoPerfis={perfis} />
          </div>

          <div className="data-block">
            <div className="section-title">Centros de Custo</div>
            {usuario.todosCentrosCusto ? (
              <div className="notice notice-warn">Acesso a todos os Centros de Custo.</div>
            ) : centrosCustoVinculados.length === 0 ? (
              <div className="empty-state">Nenhum Centro de Custo vinculado a este usuario.</div>
            ) : (
              <div className="data-grid">
                {centrosCustoVinculados.map((centroCusto) => (
                  <div className="field-readonly" key={centroCusto.id}>
                    <span>{centroCusto.codigoErp}</span>
                    <strong>{centroCusto.descricaoMaisCompras || centroCusto.descricaoErp}</strong>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="data-block">
            <div className="section-title">Filiais</div>
            <div className="empty-state">Vinculo com Filiais sera preparado em etapa futura.</div>
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
