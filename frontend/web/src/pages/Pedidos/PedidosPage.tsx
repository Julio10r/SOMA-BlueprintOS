import { StatusBadge } from "../../components/StatusBadge";

type PedidoMock = {
  id: string;
  fornecedor: string;
  categoria: string;
  valor: string;
  status: "Pendente" | "Aceito" | "Rejeitado";
  atualizadoEm: string;
};

const pedidosMock: PedidoMock[] = [
  { id: "PC-2026-0341", fornecedor: "Textil Ipiranga LTDA", categoria: "Tecidos", valor: "R$ 128.400,00", status: "Pendente", atualizadoEm: "31/07/2026" },
  { id: "PC-2026-0340", fornecedor: "Aviamentos Sul Comercio", categoria: "Aviamentos", valor: "R$ 42.900,00", status: "Aceito", atualizadoEm: "30/07/2026" },
  { id: "PC-2026-0339", fornecedor: "Malharia Boa Vista", categoria: "Malhas", valor: "R$ 76.150,00", status: "Aceito", atualizadoEm: "29/07/2026" },
  { id: "PC-2026-0338", fornecedor: "Embalagens Fenix", categoria: "Embalagens", valor: "R$ 12.300,00", status: "Rejeitado", atualizadoEm: "28/07/2026" }
];

/**
 * Tela demonstrativa (sem chamadas de API). O dominio de Pedidos ainda nao
 * possui backend integrado; esta pagina existe apenas para dar contexto
 * visual e navegacao no portal, conforme docs/product/PortalMapa.md.
 */
export function PedidosPage() {
  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">+Compras</div>
        <h1>Pedidos</h1>
        <p>Acompanhamento de pedidos de compra em aberto e concluidos.</p>
      </header>

      <div className="notice notice-warn">
        <strong>Em desenvolvimento:</strong> esta tela exibe dados de demonstracao. A integracao com o dominio de Pedidos ainda nao foi entregue.
      </div>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Resumo</div>
            <h2>Pedidos recentes</h2>
          </div>
          <span className="badge">{pedidosMock.length} pedidos</span>
        </div>
        <table className="divergence-table">
          <thead>
            <tr><th>Pedido</th><th>Fornecedor</th><th>Categoria</th><th>Valor</th><th>Status</th><th>Atualizado em</th></tr>
          </thead>
          <tbody>
            {pedidosMock.map((pedido) => (
              <tr key={pedido.id}>
                <td className="mono">{pedido.id}</td>
                <td>{pedido.fornecedor}</td>
                <td>{pedido.categoria}</td>
                <td>{pedido.valor}</td>
                <td><StatusBadge value={pedido.status} tone="decisao" /></td>
                <td>{pedido.atualizadoEm}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
