import { useEffect, useState } from "react";
import { listSuppliers } from "../../procurement/suppliers/supplierEnrichmentApi";
import type { Fornecedor } from "../../procurement/suppliers/linxSupplierContract";
import { SupplierCard } from "../../components/SupplierCard";

/**
 * Visao executiva do Portal +Compras. Busca a lista real de fornecedores
 * (GET /fornecedores) para compor um resumo simples; os demais indicadores
 * do dashboard (pedidos, negociacoes) permanecem mockados ate a entrega
 * dos respectivos dominios.
 */
export function Dashboard() {
  const [suppliers, setSuppliers] = useState<Fornecedor[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    listSuppliers()
      .then((result) => { if (active) setSuppliers(result); })
      .catch(() => { if (active) setError("Nao foi possivel carregar o resumo de fornecedores."); });
    return () => { active = false; };
  }, []);

  const total = suppliers?.length ?? null;

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Dashboard</h1>
        <p>Visao executiva do portal: integracoes, alertas e atividade recente.</p>
      </header>

      <section className="kpi-grid">
        <KpiCard label="Fornecedores cadastrados" value={total === null ? (error ? "--" : "...") : String(total)} />
        <KpiCard label="Pedidos em aberto" value="--" hint="Demo" />
        <KpiCard label="Negociacoes ativas" value="--" hint="Demo" />
        <KpiCard label="Alertas de integracao" value={error ? "1" : "0"} hint={error ?? "Nenhum alerta"} />
      </section>

      {error && <div className="notice notice-crit">{error}</div>}

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Fornecedores</div>
            <h2>Cadastros recentes</h2>
          </div>
        </div>
        {!suppliers && !error && <div className="empty-state">Carregando fornecedores...</div>}
        {suppliers && suppliers.length === 0 && (
          <div className="empty-state">Nenhum fornecedor cadastrado ainda. Utilize o modulo Fornecedores para iniciar um cadastro.</div>
        )}
        {suppliers && suppliers.length > 0 && (
          <div className="supplier-card-list">
            {suppliers.slice(0, 5).map((supplier) => <SupplierCard key={supplier.id} supplier={supplier} />)}
          </div>
        )}
      </section>
    </div>
  );
}

function KpiCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="card kpi-card">
      <div className="section-title">{label}</div>
      <div className="mono-kpi kpi-value">{value}</div>
      {hint && <p className="caption">{hint}</p>}
    </div>
  );
}
