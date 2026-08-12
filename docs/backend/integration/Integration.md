# Integration

Integrações externas do BlueprintOS com sistemas corporativos (entregável #40 — mapeamento inicial de integrações, Gate Final da Onda 1). O ERP real é `SOMA_DESENV` (Linx), consultado sempre em modo somente leitura para os domínios de Filial e Centro de Custo, e em leitura+escrita para Fornecedores — ver [Procurement.md](../procurement/Procurement.md) para o domínio de negócio que consome cada integração.

## Fornecedores (leitura + escrita)

## API de sincronização

```
POST /api/fornecedores/sincronizar          -> sincroniza um fornecedor com o ERP
POST /api/fornecedores/sincronizar/lote     -> sincroniza um lote de fornecedores
GET  /api/fornecedores/sincronizar-erp      -> executa e audita a sincronização ERP → +Compras
```

## Contratos

- `IErpFornecedorAdapter` — adaptador desacoplado por BU, nunca acesso direto ao ERP a partir de Application/Domain.
- `IFornecedorErpReader` / `SomaFornecedorReader` — leitura paginada (`OFFSET/FETCH`) do ERP.

## Filial e Centro de Custo (somente leitura, O1.7)

Dados mestres do ERP; o +Compras nunca cria/exclui Filial ou Centro de Custo, apenas anexa metadados locais (`FilialMetadado`/`CentroCustoMetadado`).

- `IFilialErpReader` / `SomaFilialReader` — leitura real de filiais em `SOMA_DESENV`.
- `ICentroCustoErpReader` / `SomaCentroCustoReader` — leitura real de centros de custo em `SOMA_DESENV`.
- Endpoints: `GET /api/administracao/filiais` (`FiliaisController`), `GET /api/administracao/centros-custo` (`CentrosCustoController`) — combinam a leitura ERP com os metadados locais.
- Dívida técnica registrada (DEB-06): `SomaFilialReader`/`SomaCentroCustoReader`/`LinxSchemaDiscoveryReader` duplicam o helper de conexão a `SOMA_DESENV` — candidata a extração futura, não bloqueante.

## Descoberta de esquema para Conhecimento Linx (somente leitura, O1.13.5)

- `LinxSchemaDiscoveryReader` — leitura read-only do esquema de `SOMA_DESENV`, usada como fonte de descoberta para os Agents `LinxErpSpecialistAgent`/`LinxDatabaseSpecialistAgent`. Comprovadamente incapaz de escrita por teste de reflexão sobre o contrato (O1.13.5).

## BrasilAPI (leitura, externa não-ERP)

- `ICnpjConsultaProvider` / `BrasilApiCnpjProvider` — consulta pública de CNPJ para sugestão revisável de enriquecimento cadastral de Fornecedor; nunca atualiza o ERP automaticamente.

## Documentos detalhados

- [Estrutura do Fornecedor no ERP](./B21.2-EstruturaFornecedorERP.md) — mapeamento canônico ERP → +Compras.
- [Sincronização ERP de Fornecedores](./FornecedorErpSynchronization.md) — orquestração em lotes, histórico de execução e erros parciais persistidos.
- [Sincronização de Fornecedores](./FornecedorSynchronization.md) — contrato canônico bidirecional, regra temporal, inativação, idempotência e auditoria.

Outras integrações (n8n, plataforma jurídica, dados de risco, notas fiscais) permanecem planejadas — ver o catálogo estratégico em `.ai/work-orders/backlog/fase-g/` e `fase-d/`.
