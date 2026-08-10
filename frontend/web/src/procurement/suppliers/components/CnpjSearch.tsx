import type { FormEvent } from "react";

/**
 * Input + botao de consulta de CNPJ/CPF. Puramente controlado pelo pai:
 * nao possui estado proprio nem chama a API diretamente (ver
 * procurement/suppliers/supplierEnrichmentApi.ts para as chamadas reais).
 */
export function CnpjSearch({ value, onChange, onSubmit, loading, error }: {
  value: string;
  onChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
  loading: boolean;
  error?: string | null;
}) {
  return (
    <form className="card form-card" onSubmit={onSubmit}>
      <label htmlFor="cnpjCpf">Cnpj_Cpf</label>
      <div className="input-row">
        <input
          id="cnpjCpf"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="12345678000195"
          maxLength={18}
        />
        <button className="btn btn-primary" disabled={loading} type="submit">
          <SearchIcon /> Consultar CNPJ
        </button>
      </div>
      {error && <div className="notice notice-crit">{error}</div>}
    </form>
  );
}

function SearchIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.35-4.35" /></svg>;
}
