import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { CentroCustoForm } from "../components/CentroCustoForm";
import {
  getCentroCusto,
  listUnidadesAlocacaoParaVinculo,
  listVinculosUnidadeAlocacao,
  substituirVinculosUnidadeAlocacao,
  updateCentroCusto
} from "../services/centrosCustoApi";
import type {
  CentroCusto,
  CentroCustoUpdateInput,
  UnidadeAlocacaoParaVinculo,
  UnidadeAlocacaoVinculoResumo
} from "../types/centroCustoTypes";

/**
 * Edicao de metadados locais de um Centro de Custo e do vinculo real com Unidades de Alocacao (O1.9). Nao
 * existe pagina de "criacao": Centro de Custo e um dado mestre integrado do ERP e o +Compras nunca cria um
 * centro de custo (ADR-0020, item 3).
 */
export function CentroCustoEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [centroCusto, setCentroCusto] = useState<CentroCusto | null>(null);
  const [loadingCentroCusto, setLoadingCentroCusto] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [catalogoUnidadesAlocacao, setCatalogoUnidadesAlocacao] = useState<UnidadeAlocacaoParaVinculo[]>([]);
  const [vinculosAtuais, setVinculosAtuais] = useState<UnidadeAlocacaoVinculoResumo[]>([]);
  const [savingVinculos, setSavingVinculos] = useState(false);
  const [errorVinculos, setErrorVinculos] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    if (!id) return;
    setLoadingCentroCusto(true);
    try {
      const [centro, catalogo, vinculos] = await Promise.all([
        getCentroCusto(id),
        listUnidadesAlocacaoParaVinculo(),
        listVinculosUnidadeAlocacao(id)
      ]);
      if (!centro) {
        setError("Centro de custo nao encontrado.");
        return;
      }
      setCentroCusto(centro);
      setCatalogoUnidadesAlocacao(catalogo);
      setVinculosAtuais(vinculos);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar centro de custo.");
    } finally {
      setLoadingCentroCusto(false);
    }
  }, [id]);

  useEffect(() => {
    carregar();
  }, [carregar]);

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

  async function handleSubmitVinculos(unidadeAlocacaoIds: string[], padraoId: string | null) {
    if (!id) return;
    setSavingVinculos(true);
    setErrorVinculos(null);
    try {
      const atualizados = await substituirVinculosUnidadeAlocacao(id, unidadeAlocacaoIds, padraoId);
      setVinculosAtuais(atualizados);
    } catch (err) {
      setErrorVinculos(err instanceof Error ? err.message : "Falha ao salvar unidades de alocacao vinculadas.");
    } finally {
      setSavingVinculos(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Editar centro de custo</h1>
        <p>Apenas os metadados locais do +Compras e o vinculo com Unidades de Alocacao podem ser alterados aqui.</p>
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
          catalogoUnidadesAlocacao={catalogoUnidadesAlocacao}
          vinculosAtuais={vinculosAtuais}
          savingVinculos={savingVinculos}
          errorVinculos={errorVinculos}
          onSubmitVinculos={handleSubmitVinculos}
        />
      ) : (
        error && <div className="notice notice-crit">{error}</div>
      )}
    </div>
  );
}
