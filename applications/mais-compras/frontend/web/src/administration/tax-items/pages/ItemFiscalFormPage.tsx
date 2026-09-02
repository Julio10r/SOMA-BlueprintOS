import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ItemFiscalForm } from "../components/ItemFiscalForm";
import { ReferenciasFornecedorTab } from "../components/ReferenciasFornecedorTab";
import { useFornecedoresAtivos } from "../hooks/useFornecedoresAtivos";
import { useItemFiscalOpcoesDeApoio } from "../hooks/useItemFiscalOpcoesDeApoio";
import { useReferenciasFornecedor } from "../hooks/useReferenciasFornecedor";
import { createItemFiscal, getItemFiscal, updateItemFiscal } from "../services/itensFiscaisApi";
import type { ItemFiscal, ItemFiscalCreateInput, ItemFiscalUpdateInput } from "../types/itemFiscalTypes";

/**
 * Cadastro/edição de Item Fiscal (B3 - Bloco 3, Discovery homologado). Uma única página cobre os dois
 * modos, mesmo padrão de `UnidadeAlocacaoFormPage.tsx` — presença de `:id` na rota decide o modo.
 */
export function ItemFiscalFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const opcoes = useItemFiscalOpcoesDeApoio();
  const referenciasFornecedor = useReferenciasFornecedor(id);
  const opcoesFornecedor = useFornecedoresAtivos();
  const [item, setItem] = useState<ItemFiscal | null>(null);
  const [loadingItem, setLoadingItem] = useState(Boolean(id));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoadingItem(true);
    getItemFiscal(id).then((found) => {
      if (!found) {
        setError("Item fiscal não encontrado.");
        return;
      }
      setItem(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar o item fiscal."))
      .finally(() => setLoadingItem(false));
  }, [id]);

  async function handleSubmit(input: ItemFiscalCreateInput | ItemFiscalUpdateInput) {
    setSaving(true);
    setError(null);
    try {
      if (id) {
        await updateItemFiscal(id, input as ItemFiscalUpdateInput);
      } else {
        await createItemFiscal(input as ItemFiscalCreateInput);
      }
      navigate("..", { relative: "path" });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar item fiscal.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Cadastros</div>
        <h1>{id ? "Editar item fiscal" : "Novo item fiscal"}</h1>
        <p>Unidade e Conta Contábil são obrigatórias e selecionadas entre os cadastros de apoio ativos do Linx.</p>
      </header>

      {loadingItem ? (
        <div className="empty-state">Carregando item fiscal...</div>
      ) : (
        <ItemFiscalForm
          item={item ?? undefined}
          opcoes={opcoes}
          error={error}
          loading={saving}
          onSubmit={handleSubmit}
          onCancel={() => navigate("..", { relative: "path" })}
          referenciasFornecedor={
            item && (
              <ReferenciasFornecedorTab
                referencias={referenciasFornecedor.referencias}
                opcoesFornecedor={opcoesFornecedor}
                loading={referenciasFornecedor.loading}
                error={referenciasFornecedor.error}
                onIncluir={(fornecedorId, codigoItemFornecedor) =>
                  referenciasFornecedor.incluir({ fornecedorId, codigoItemFornecedor })
                }
                onAtualizar={(refId, codigoItemFornecedor) =>
                  referenciasFornecedor.atualizar(refId, { codigoItemFornecedor })
                }
                onRemover={(refId) => referenciasFornecedor.remover(refId)}
              />
            )
          }
        />
      )}
    </div>
  );
}
