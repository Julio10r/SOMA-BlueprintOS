Sprint: B1

Status:
CONCLUÍDA

Objetivo:
Persistência de Fornecedores do +Compras

Estado:
Implementação concluída: CRUD de fornecedores preparado com EF Core/SQL Server, migration versionada, repository, casos de uso e endpoints REST em `/fornecedores`. A migration não foi aplicada por restrição explícita desta sprint.

Validação de conectividade:
- `MaisComprasConnection`: sucesso por conexão aberta e `SELECT 1`.
- `ErpConnection` (`SOMA_DESENV`): sucesso por conexão aberta e `SELECT 1`.
- Nenhuma migration, DDL ou escrita foi executada em qualquer banco.

Limites de identidade:
- O Entra ID não será implementado nesta sprint.
- Qualquer identidade temporária será restrita a `Development` e desacoplada por contrato, conforme [ADR-0011](./DECISIONS.md#adr-0011-identidade-temporária-de-desenvolvimento-para-antecipar-a-persistência-de-fornecedores).
