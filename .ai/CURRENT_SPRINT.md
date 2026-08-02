# B2.1.3 - Endurecimento da Integracao ERP de Fornecedores

Status:
Concluida em codigo.

Objetivo:
Transformar a sincronizacao de fornecedores ERP SOMA -> +Compras em rotina operacional rastreavel, paginada e resiliente a erros parciais.

Entregas:

- `IFornecedorErpReader` evoluido para leitura paginada por `skip`/`take`.
- `SomaFornecedorReader` usando `OFFSET/FETCH`, sem carregar todos os fornecedores em memoria.
- `SincronizarFornecedoresErpUseCase` processando em lotes configuraveis pelo parametro `limite`.
- Historico de execucao persistido em `SincronizacoesFornecedores`.
- Erros parciais persistidos em `ErrosSincronizacoesFornecedores`.
- Retorno detalhado do endpoint `GET /api/fornecedores/sincronizar-erp`.
- Logs estruturados de inicio, lote processado, erro parcial e fim da sincronizacao.
- Testes unitarios criados/atualizados para sem registros, novo fornecedor, alterado, sem alteracao, erro parcial, multiplos lotes e totais.

Validacao:

- `dotnet build backend/BlueprintOS.sln`: aprovado, 0 erros e 0 avisos.
- `dotnet test backend/BlueprintOS.sln`: bloqueado no sandbox por `System.Net.Sockets.SocketException (13): Permission denied` em named pipes do MSBuild; tentativa escalonada rejeitada pelo revisor automatico por limite de uso.
- Validacao real do endpoint e dados em `MaisCompras` permanece dependente de API local, VPN e connection strings corporativas.

Documentacao:

- `docs/engineering/FornecedorErpSynchronization.md`
