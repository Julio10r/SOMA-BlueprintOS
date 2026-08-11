import { useState } from "react";
import type { FeatureFlagCriarInput } from "../types/featureFlagTypes";

export function FeatureFlagForm({ onSalvar, onCancelar }: {
  onSalvar: (input: FeatureFlagCriarInput) => Promise<void>;
  onCancelar: () => void;
}) {
  const [nome, setNome] = useState("");
  const [descricao, setDescricao] = useState("");
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setErro(null);
    setSalvando(true);
    try {
      await onSalvar({ nome, descricao });
    } catch (err) {
      setErro(err instanceof Error ? err.message : "Falha ao criar Feature Flag.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <form className="form-stack" onSubmit={handleSubmit}>
      {erro && <div className="notice notice-crit">{erro}</div>}
      <label>
        Nome da flag
        <input value={nome} onChange={(e) => setNome(e.target.value)} required />
      </label>
      <label>
        Descricao
        <input value={descricao} onChange={(e) => setDescricao(e.target.value)} required />
      </label>
      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={salvando}>
          {salvando ? "Salvando..." : "Criar"}
        </button>
        <button type="button" className="btn btn-secondary" onClick={onCancelar} disabled={salvando}>
          Cancelar
        </button>
      </div>
    </form>
  );
}
