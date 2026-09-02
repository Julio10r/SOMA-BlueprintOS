import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ContaContabilForm } from "../components/ContaContabilForm";
import { getContaContabil, updateContaContabil } from "../services/contasContabeisApi";
import type { ContaContabil, ContaContabilUpdateInput } from "../types/contaContabilTypes";

/**
 * Edicao de metadados locais de uma Conta Contabil. Nao existe pagina de "criacao": Conta Contabil e um
 * cadastro de apoio integrado do ERP e o +Compras nunca a cria.
 */
export function ContaContabilEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [conta, setConta] = useState<ContaContabil | null>(null);
  const [loadingConta, setLoadingConta] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingConta(true);
    getContaContabil(id).then((found) => {
      if (!found) {
        setError("Conta contábil não encontrada.");
        return;
      }
      setConta(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar a conta contábil."))
      .finally(() => setLoadingConta(false));
  }, [id]);

  async function handleSubmit(input: ContaContabilUpdateInput) {
    if (!id) return;
    setSaving(true);
    setError(null);
    try {
      await updateContaContabil(id, input);
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar conta contábil.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Editar conta contábil</h1>
        <p>Apenas os metadados locais do +Compras podem ser alterados aqui.</p>
      </header>

      {loadingConta ? (
        <div className="empty-state">Carregando conta contábil...</div>
      ) : conta ? (
        <ContaContabilForm
          conta={conta}
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
