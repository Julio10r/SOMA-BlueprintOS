# Procurement

Vertical slice de **Fornecedores**: cadastro, descoberta no ERP, consulta/enriquecimento de CNPJ e sincronização bidirecional com o ERP (SOMA_DESENV → +Compras). Entidades e casos de uso reais vivem em Domain/Application; persistência via `BlueprintOSDbContext` (EF Core + SQL Server, ver [Database.md](../../database/Database.md)) em Infrastructure; API própria (`/fornecedores`, `/api/fornecedores/...`).

## Contratos

- `IFornecedorUseCases`
- `IConsultarCnpjFornecedorUseCase`
- `IFornecedorEnriquecimentoUseCases`
- `ISincronizarFornecedorUseCase`
- `ISincronizarFornecedoresErpUseCase`
- `IFornecedorDiscoveryUseCase`
- `IErpFornecedorAdapter` (contrato de integração — ver [Integration.md](../integration/Integration.md) para o adaptador concreto do ERP)

## Classes de domínio

- `Fornecedor`
- `Cnpj`
- `ScoreFornecedor`
- `FornecedorCanonico`
- `FornecedorUseCases`
- `SincronizarFornecedorUseCase`

## API

```
GET  /fornecedores                          -> busca/listagem de fornecedores
POST /fornecedores                          -> cadastro de fornecedor
GET  /fornecedores/{id}                     -> detalhe do fornecedor
PUT  /fornecedores/{id}                     -> atualização do fornecedor
POST /fornecedores/consulta-cnpj            -> consulta de CNPJ (BrasilAPI)
POST /fornecedores/{id}/enriquecimento-cnpj -> análise de divergências de CNPJ
POST /fornecedores/{id}/enriquecimento-cnpj/aprovar  -> aprova o enriquecimento
POST /fornecedores/{id}/enriquecimento-cnpj/rejeitar -> rejeita o enriquecimento

POST /api/fornecedores/descobrir            -> descoberta de fornecedores no ERP
GET  /api/fornecedores/descobertas          -> lista descobertas registradas
```

Os controllers delegam a casos de uso Application, reutilizando contratos de domínio; a API permanece fina, sem regra de negócio própria.

## Documentos detalhados

- [Consulta e Enriquecimento de CNPJ](./FornecedorCnpjEnrichment.md) — fluxo de sugestão revisável a partir de provedor externo (BrasilAPI), sem atualização automática de ERP.

Para sincronização e estrutura de dados com o ERP, ver [docs/backend/integration/Integration.md](../integration/Integration.md).
