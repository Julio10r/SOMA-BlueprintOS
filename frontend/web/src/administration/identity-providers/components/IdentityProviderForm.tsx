import { FormEvent, KeyboardEvent, useState } from "react";
import type { IdentityProvider, IdentityProviderInput } from "../types/identityProviderTypes";

const TIPOS = ["MicrosoftEntraId", "OtpEmail"];

/**
 * Cadastro/edicao de Identity Provider. O campo "Parametros de configuracao" NUNCA e pre-preenchido com
 * o segredo real na edicao — a API nunca o devolve (apenas `parametrosConfigurados: boolean`). Em modo
 * edicao com segredo ja salvo, exibe apenas o indicador "Ja configurado" e mantem o campo vazio; deixar
 * vazio ao salvar preserva o segredo existente no backend.
 */
export function IdentityProviderForm({ provider, error, loading, onSubmit, onCancel }: {
  provider?: IdentityProvider;
  error: string | null;
  loading: boolean;
  onSubmit: (input: IdentityProviderInput) => void;
  onCancel: () => void;
}) {
  const [tipo, setTipo] = useState(provider?.tipo ?? TIPOS[0]);
  const [dominios, setDominios] = useState<string[]>(provider?.dominiosAutorizados ?? []);
  const [novoDominio, setNovoDominio] = useState("");
  const [parametros, setParametros] = useState("");

  function adicionarDominio() {
    const valor = novoDominio.trim();
    if (valor && !dominios.includes(valor)) setDominios([...dominios, valor]);
    setNovoDominio("");
  }

  function handleDominioKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter" || event.key === ",") {
      event.preventDefault();
      adicionarDominio();
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ tipo, dominiosAutorizados: dominios, parametros: parametros || undefined });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{provider ? "Editar Identity Provider" : "Novo Identity Provider"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Tipo
        <select value={tipo} onChange={(event) => setTipo(event.target.value)} disabled={loading}>
          {TIPOS.map((opcao) => (
            <option key={opcao} value={opcao}>
              {opcao}
            </option>
          ))}
        </select>
      </label>

      <label>
        Dominios autorizados
        <input
          value={novoDominio}
          onChange={(event) => setNovoDominio(event.target.value)}
          onKeyDown={handleDominioKeyDown}
          onBlur={adicionarDominio}
          placeholder="dominio.com.br e pressione Enter"
          disabled={loading}
        />
      </label>
      <ul className="tag-list">
        {dominios.map((dominio) => (
          <li key={dominio} className="tag">
            {dominio}
            <button type="button" onClick={() => setDominios(dominios.filter((item) => item !== dominio))} disabled={loading}>
              ×
            </button>
          </li>
        ))}
      </ul>

      <label>
        Parametros de configuracao
        {provider?.parametrosConfigurados && <span className="notice notice-info">Ja configurado</span>}
        <input
          type="password"
          value={parametros}
          onChange={(event) => setParametros(event.target.value)}
          placeholder={provider ? "Deixe vazio para manter o valor atual" : ""}
          disabled={loading}
          autoComplete="new-password"
        />
      </label>

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
