import { FormEvent, useState } from "react";
import type { UnidadeNegocio, UnidadeNegocioCriarInput, UnidadeNegocioEditarInput } from "../types/unidadeNegocioTypes";

/**
 * Cadastro/edicao de Unidade de Negocio. Slug e definido apenas na criacao e nunca e editavel — em
 * modo edicao o campo Slug e exibido como somente leitura.
 */
export function UnidadeNegocioForm({ unidadeNegocio, error, loading, onSubmit, onCancel }: {
  unidadeNegocio?: UnidadeNegocio;
  error: string | null;
  loading: boolean;
  onSubmit: (input: UnidadeNegocioCriarInput | UnidadeNegocioEditarInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(unidadeNegocio?.nome ?? "");
  const [slug, setSlug] = useState(unidadeNegocio?.slug ?? "");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit(unidadeNegocio ? { nome } : { nome, slug });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{unidadeNegocio ? "Editar Unidade de Negocio" : "Nova Unidade de Negocio"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} required disabled={loading} />
      </label>

      {unidadeNegocio ? (
        <label>
          Slug
          <input value={slug} disabled readOnly />
        </label>
      ) : (
        <label>
          Slug
          <input value={slug} onChange={(event) => setSlug(event.target.value)} required disabled={loading} />
        </label>
      )}

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
