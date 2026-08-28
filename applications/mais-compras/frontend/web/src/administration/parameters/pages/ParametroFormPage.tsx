import { useNavigate, useParams } from "react-router-dom";
import { ParametroForm } from "../components/ParametroForm";
import { useParametros } from "../hooks/useParametros";

export function ParametroFormPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { parametros, loading, criar, atualizar } = useParametros();
  const parametro = id ? parametros.find((p) => p.id === id) : undefined;

  if (id && loading) return <div className="empty-state">Carregando parâmetro...</div>;

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>{parametro ? "Editar parâmetro" : "Novo parâmetro"}</h1>
      </header>
      <section className="card">
        <ParametroForm
          parametro={parametro}
          onSalvar={async (input) => {
            if (parametro) {
              await atualizar(parametro.id, { valor: input.valor, descricao: input.descricao });
            } else {
              await criar(input);
            }
            navigate("..");
          }}
          onCancelar={() => navigate("..")}
        />
      </section>
    </div>
  );
}
