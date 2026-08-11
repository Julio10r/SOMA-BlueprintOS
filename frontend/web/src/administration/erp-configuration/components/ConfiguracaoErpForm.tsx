import { FormEvent, useState } from "react";
import type { ConfiguracaoErp, ConfiguracaoErpInput } from "../types/configuracaoErpTypes";

/**
 * Formulario 1:1 de Configuracao de ERP. Campo "Parametros de conexao" NUNCA e pre-preenchido com o
 * valor real — a API nunca o devolve (apenas `parametrosConfigurados: boolean`).
 */
export function ConfiguracaoErpForm({ configuracao, error, loading, onSubmit }: {
  configuracao: ConfiguracaoErp | null;
  error: string | null;
  loading: boolean;
  onSubmit: (input: ConfiguracaoErpInput) => void;
}) {
  const [sistemaErp, setSistemaErp] = useState(configuracao?.sistemaErp ?? "");
  const [parametrosConexao, setParametrosConexao] = useState("");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ sistemaErp, parametrosConexao: parametrosConexao || undefined });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{configuracao ? "Editar Configuracao de ERP" : "Configurar ERP"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Sistema ERP
        <input value={sistemaErp} onChange={(event) => setSistemaErp(event.target.value)} required disabled={loading} />
      </label>

      <label>
        Parametros de conexao
        {configuracao?.parametrosConfigurados && <span className="notice notice-info">Ja configurado</span>}
        <input
          type="password"
          value={parametrosConexao}
          onChange={(event) => setParametrosConexao(event.target.value)}
          placeholder={configuracao ? "Deixe vazio para manter o valor atual" : ""}
          disabled={loading}
          autoComplete="new-password"
        />
      </label>

      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
