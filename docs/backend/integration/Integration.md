# Integration

Integrações externas do BlueprintOS com sistemas corporativos. Hoje, a única integração real implementada é com o ERP (SOMA_DESENV / Linx) para o domínio de Fornecedores — ver [Procurement.md](../procurement/Procurement.md) para o domínio de negócio que consome esta integração.

## API de sincronização

```
POST /api/fornecedores/sincronizar          -> sincroniza um fornecedor com o ERP
POST /api/fornecedores/sincronizar/lote     -> sincroniza um lote de fornecedores
GET  /api/fornecedores/sincronizar-erp      -> executa e audita a sincronização ERP → +Compras
```

## Contratos

- `IErpFornecedorAdapter` — adaptador desacoplado por BU, nunca acesso direto ao ERP a partir de Application/Domain.
- `IFornecedorErpReader` / `SomaFornecedorReader` — leitura paginada (`OFFSET/FETCH`) do ERP.

## Documentos detalhados

- [Estrutura do Fornecedor no ERP](./B21.2-EstruturaFornecedorERP.md) — mapeamento canônico ERP → +Compras.
- [Sincronização ERP de Fornecedores](./FornecedorErpSynchronization.md) — orquestração em lotes, histórico de execução e erros parciais persistidos.
- [Sincronização de Fornecedores](./FornecedorSynchronization.md) — contrato canônico bidirecional, regra temporal, inativação, idempotência e auditoria.

Outras integrações (n8n, plataforma jurídica, dados de risco, notas fiscais) permanecem planejadas — ver o catálogo estratégico em `.ai/work-orders/backlog/fase-g/` e `fase-d/`.
