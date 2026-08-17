import type { FormEvent } from "react";

/**
 * Card compacto de consulta de CNPJ/CPF (padrao "lookup card" do design
 * system, ver resources/design-system/preview/component-lookup-tabs.html).
 * Substitui o antigo `.form-card` full-width: a consulta e uma interacao
 * simples e deve ocupar o espaco equivalente a ela, nao uma secao inteira da
 * tela. Puramente controlado pelo pai: nao possui estado proprio nem chama a
 * API diretamente (ver procurement/suppliers/supplierEnrichmentApi.ts).
 */
export function CnpjSearch({ value, onChange, onSubmit, loading, error }: {
  value: string;
  onChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
  loading: boolean;
  error?: string | null;
}) {
  return (
    <form className="lookup-card" onSubmit={onSubmit}>
      <div className="icon-h">
        <SearchIcon />
      </div>
      <h2>Consultar fornecedor</h2>
      <p className="sub">Localize um fornecedor pelo CNPJ, CPF ou documento alfanumerico do ERP.</p>
      <div className="lookup-field">
        <label htmlFor="cnpjCpf">CNPJ/CPF</label>
        <input
          id="cnpjCpf"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="12345678000195"
          maxLength={18}
        />
        <span className="hint">Consulta e somente leitura — nenhum fornecedor e criado nesta etapa.</span>
      </div>
      <button className="btn btn-primary lookup-submit" disabled={loading} type="submit">
        <SearchIcon /> Consultar CNPJ
      </button>
      {error && <div className="notice notice-crit">{error}</div>}
    </form>
  );
}

function SearchIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.35-4.35" /></svg>;
}
