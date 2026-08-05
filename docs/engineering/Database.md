# Banco de Dados

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## Banco de dados

O backend possui um `DbContext` real: `BlueprintOSDbContext`
(`backend/src/BlueprintOS.Infrastructure/Persistence/`), usando Entity Framework Core
com SQL Server. Ele persiste o domínio de Fornecedores (cadastro, descoberta, sincronização
com o ERP e histórico de consulta de CNPJ), com migrations reais aplicadas nesse mesmo
projeto (`Persistence/Migrations/`).

O banco é sempre externo — bancos corporativos `MAISCOMPRAS`/`SOMA_DESENV`, acessados via
VPN — nunca um SQL Server local ou em container. Não há pasta `database/` na raiz do
repositório nem scripts/seeds de banco separados; a persistência dos demais módulos
(ex.: `Documentation`, `Knowledge`) permanece em memória ou em arquivos Markdown.
Este documento será atualizado conforme novos módulos passarem a persistir dados.
