import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { FilialForm } from "../components/FilialForm";
import { getFilial, updateFilial } from "../services/filiaisMockApi";
import type { Filial, FilialUpdateInput } from "../types/filialTypes";

/**
 * Edicao de metadados locais de uma Filial. Nao existe pagina de
 * "criacao": Filial e um dado mestre integrado do ERP e o +Compras nunca
 * cria uma filial (ADR-0020, item 3).
 */
export function FilialEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [filial, setFilial] = useState<Filial | null>(null);
  const [loadingFilial, setLoadingFilial] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingFilial(true);
    getFilial(id).then((found) => {
      if (!found) {
        setError("Filial nao encontrada.");
        return;
      }
      setFilial(found);
    }).finally(() => setLoadingFilial(false));
  }, [id]);

  async function handleSubmit(input: FilialUpdateInput) {
    if (!id) return;
    setSaving(true);
    setError(null);
    try {
      await updateFilial(id, input);
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar filial.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Editar filial</h1>
        <p>Apenas os metadados locais do +Compras podem ser alterados aqui.</p>
      </header>

      {loadingFilial ? (
        <div className="empty-state">Carregando filial...</div>
      ) : filial ? (
        <FilialForm
          filial={filial}
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
