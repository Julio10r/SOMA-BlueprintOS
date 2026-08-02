# Sincronizacao de Fornecedores ERP SOMA

## Objetivo

Ler fornecedores do ERP `SOMA_DESENV` e sincronizar registros no banco da aplicacao `MaisCompras` de forma paginada, rastreavel e resiliente a erros parciais.

## Arquitetura

O SQL do ERP permanece isolado na Infrastructure:

```text
SOMA_DESENV
  ↓
SomaFornecedorReader
  ↓
FornecedorErpIntegracaoDto / FornecedorCanonico
  ↓
SincronizarFornecedoresErpUseCase
  ↓
Fornecedor + SincronizacaoFornecedor + ErroSincronizacaoFornecedor
  ↓
Entity Framework Core / MaisCompras
```

Componentes principais:

- `IFornecedorErpReader`: contrato de leitura ERP com paginação `skip`/`take`.
- `SomaFornecedorReader`: implementacao SOMA com `OFFSET/FETCH`.
- `SincronizarFornecedoresErpUseCase`: orquestra lotes, upsert, métricas, logs e erros parciais.
- `SincronizacaoFornecedor`: historico da execucao.
- `ErroSincronizacaoFornecedor`: erros por fornecedor, sem dados sensiveis.

## Endpoint

```http
GET /api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=500
```

`limite` define o tamanho do lote. O endpoint e a rota foram preservados.

Resposta:

```json
{
  "execucaoId": "12345678-1234-1234-1234-123456789abc",
  "status": "Parcial",
  "inicio": "2026-08-02T10:00:00Z",
  "fim": "2026-08-02T10:00:05Z",
  "consultados": 1000,
  "incluidos": 100,
  "atualizados": 850,
  "semAlteracao": 49,
  "erros": 1,
  "duracaoMs": 5000,
  "businessUnit": "DEFAULT",
  "erpSistema": "SOMA_DESENV",
  "correlationId": "..."
}
```

Status possiveis:

- `Sucesso`: nenhum erro registrado.
- `Parcial`: ao menos um fornecedor falhou, mas outros foram processados.
- `Erro`: todos os fornecedores consultados falharam.

## Persistencia

Migration:

- `202608020001_B213FornecedorErpSyncHardening`

Tabelas:

- `SincronizacoesFornecedores`: sistema origem, BU, inicio/fim, status, totais e duracao.
- `ErrosSincronizacoesFornecedores`: execucao, identificacao tecnica do fornecedor, mensagem sanitizada, stack trace resumida e data/hora.

## Regras Preservadas

- `NomeFantasia` continua protegido e so e atualizado quando a origem e `ERP`.
- `OrigemInformacao = ERP` e vinculo `BusinessUnit`/`ErpSistema`/`ErpFornecedorId` continuam registrados.
- Alteracoes manuais feitas no +Compras nao sao sobrescritas por fluxos externos fora da rotina ERP.
- `BusinessUnit` e mantida em cada fornecedor sincronizado.

## Logs

A rotina usa `ILogger` padrao ASP.NET Core para:

- inicio da sincronizacao;
- lote processado;
- erro parcial por fornecedor;
- fim da sincronizacao com totais e duracao.

## Validacao

Com VPN ativa e connection strings configuradas:

```bash
dotnet build backend/BlueprintOS.sln
dotnet test backend/BlueprintOS.sln
curl "http://localhost:5262/api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=500"
```

Validar no banco `MaisCompras`:

- fornecedores incluidos/atualizados;
- execucao criada em `SincronizacoesFornecedores`;
- erros parciais em `ErrosSincronizacoesFornecedores`, quando aplicavel.

## Limitacoes Conhecidas

- Testes reais de integracao dependem de VPN e secrets locais.
- A rotina ainda e acionada via endpoint manual; agendamento operacional fica para sprint futura.
- O ambiente de sandbox pode bloquear `dotnet test` por named pipes do MSBuild.
