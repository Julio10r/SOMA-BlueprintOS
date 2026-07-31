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
- Migration B2: `202607300002_B2FornecedorDiscovery`.

### ERP SOMA_DESENV

`ErpFornecedorDiscoveryRepository` usa `ErpConnection` somente para leitura de metadados e dados
operacionais. O adaptador rejeita qualquer banco cujo catálogo não seja `SOMA_DESENV` e não possui
migrations ou comandos de escrita no ERP. A validação operacional depende de conectividade com o SQL Server.
