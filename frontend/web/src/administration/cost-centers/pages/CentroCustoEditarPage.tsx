import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { CentroCustoForm } from "../components/CentroCustoForm";
import { getCentroCusto, updateCentroCusto } from "../services/centrosCustoApi";
import type { CentroCusto, CentroCustoUpdateInput } from "../types/centroCustoTypes";

/**
 * Edicao de metadados locais de um Centro de Custo. Nao existe pagina de
 * "criacao": Centro de Custo e um dado mestre integrado do ERP e o
 * +Compras nunca cria um centro de custo (ADR-0020, item 3).
 */
export function CentroCustoEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [centroCusto, setCentroCusto] = useState<CentroCusto | null>(null);
  const [loadingCentroCusto, setLoadingCentroCusto] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingCentroCusto(true);
    getCentroCusto(id).then((found) => {
      if (!found) {
        setError("Centro de custo nao encontrado.");
        return;
      }
      setCentroCusto(found);
    }).finally(() => setLoadingCentroCusto(false));
  }, [id]);

  async function handleSubmit(input: CentroCustoUpdateInput) {
    if (!id) return;
    setSaving(true);
    setError(null);
    try {
      await updateCentroCusto(id, input);
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar centro de custo.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Editar centro de custo</h1>
        <p>Apenas os metadados locais do +Compras podem ser alterados aqui.</p>
      </header>

      {loadingCentroCusto ? (
        <div className="empty-state">Carregando centro de custo...</div>
      ) : centroCusto ? (
        <CentroCustoForm
          centroCusto={centroCusto}
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
