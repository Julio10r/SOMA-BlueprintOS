# Banco de Dados

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 21:04:12 UTC
- **Última atualização:** 2026-07-30

---

## Banco de dados

O `BlueprintOSDbContext` usa exclusivamente `MaisComprasConnection` para persistência da aplicação.

### +Compras

- `Fornecedores`: agregado persistente entregue na B1.
- `FornecedoresDescobertos`: resultados da descoberta B2, vinculados a `TemporaryUserId`.
- `FornecedoresSincronizacoes`: histórico de tentativas, direção, status, correlação e erro sanitizado.
- `Fornecedores`: vínculo externo opcional por `BusinessUnit`, `ErpSistema` e `ErpFornecedorId`, com status/origem da sincronização.
- Migration B2: `202607300002_B2FornecedorDiscovery`.
- Migration B2.1: `202607310001_B21FornecedorSynchronization` (criada; aplicação pendente de autorização operacional).

### ERP SOMA_DESENV

`ErpFornecedorDiscoveryRepository` usa `ErpConnection` para leitura de metadados e dados operacionais.
`SomaDesenvolErpFornecedorAdapter` usa a mesma fronteira exclusivamente com `SOMA_DESENV` para
consulta/criação/atualização controlada; schema e tabela são configuráveis na Infrastructure.
O ERP não recebe migrations. A conectividade real foi confirmada em 31/07/2026; a homologação do
mapeamento de escrita ainda está pendente.
