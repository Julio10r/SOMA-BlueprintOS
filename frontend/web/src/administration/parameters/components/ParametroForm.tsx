import { useState } from "react";
import type { Parametro } from "../types/parametroTypes";

export function ParametroForm({ parametro, onSalvar, onCancelar }: {
  parametro?: Parametro;
  onSalvar: (input: { chave: string; valor: string; descricao: string; unidadeNegocioId?: string }) => Promise<void>;
  onCancelar: () => void;
}) {
  const [chave, setChave] = useState(parametro?.chave ?? "");
  const [valor, setValor] = useState(parametro?.valor ?? "");
  const [descricao, setDescricao] = useState(parametro?.descricao ?? "");
  const [unidadeNegocioId, setUnidadeNegocioId] = useState(parametro?.unidadeNegocioId ?? "");
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setErro(null);
    setSalvando(true);
    try {
      await onSalvar({ chave, valor, descricao, unidadeNegocioId: unidadeNegocioId || undefined });
    } catch (err) {
      setErro(err instanceof Error ? err.message : "Falha ao salvar Parametro.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <form className="form-stack" onSubmit={handleSubmit}>
      {erro && <div className="notice notice-crit">{erro}</div>}
      <label>
        Chave
        <input
          value={chave}
          onChange={(e) => setChave(e.target.value)}
          disabled={Boolean(parametro)}
          required
        />
      </label>
      <label>
        Valor
        <input value={valor} onChange={(e) => setValor(e.target.value)} required />
      </label>
      <label>
        Descricao
        <input value={descricao} onChange={(e) => setDescricao(e.target.value)} required />
      </label>
      <label>
        Unidade de Negocio (Id) — deixe vazio para parametro Global
        <input
          value={unidadeNegocioId}
          onChange={(e) => setUnidadeNegocioId(e.target.value)}
          disabled={Boolean(parametro)}
        />
      </label>
      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={salvando}>
          {salvando ? "Salvando..." : "Salvar"}
        </button>
        <button type="button" className="btn btn-secondary" onClick={onCancelar} disabled={salvando}>
          Cancelar
        </button>
      </div>
    </form>
  );
}
