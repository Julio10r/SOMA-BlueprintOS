import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { UnidadeMedidaForm } from "../components/UnidadeMedidaForm";
import { getUnidadeMedida, updateUnidadeMedida } from "../services/unidadesMedidaApi";
import type { UnidadeMedida, UnidadeMedidaUpdateInput } from "../types/unidadeMedidaTypes";

/**
 * Edicao de metadados locais de uma Unidade de Medida. Nao existe pagina de "criacao": Unidade de Medida
 * e um cadastro de apoio integrado do ERP e o +Compras nunca a cria.
 */
export function UnidadeMedidaEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [unidade, setUnidade] = useState<UnidadeMedida | null>(null);
  const [loadingUnidade, setLoadingUnidade] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingUnidade(true);
    getUnidadeMedida(id).then((found) => {
      if (!found) {
        setError("Unidade de medida não encontrada.");
        return;
      }
      setUnidade(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar a unidade de medida."))
      .finally(() => setLoadingUnidade(false));
  }, [id]);

  async function handleSubmit(input: UnidadeMedidaUpdateInput) {
    if (!id) return;
    setSaving(true);
    setError(null);
    try {
      await updateUnidadeMedida(id, input);
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar unidade de medida.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Editar unidade de medida</h1>
        <p>Apenas os metadados locais do +Compras podem ser alterados aqui.</p>
      </header>

      {loadingUnidade ? (
        <div className="empty-state">Carregando unidade de medida...</div>
      ) : unidade ? (
        <UnidadeMedidaForm
          unidade={unidade}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
        />
      ) : (
        error && <div className="notice notice-crit">{error}</div>
      )}
    </div>
  );
}
