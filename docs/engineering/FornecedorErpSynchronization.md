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

`limite` define o **teto TOTAL de fornecedores processados nesta execucao** (clamped entre 1 e 5000), nao o tamanho de pagina. A leitura paginada continua acontecendo internamente (ate 500 registros por pagina contra o ERP), mas o loop externo para assim que o total consultado atinge `limite`, mesmo que o ERP tenha mais registros disponiveis.

> **Correcao de 02/08/2026:** antes desta correcao, `limite` era usado apenas como tamanho de pagina dentro de um loop que so parava quando o ERP retornava uma pagina vazia — ou seja, `limite=50` acabava varrendo a tabela inteira de fornecedores do ERP. Ver `.ai/memory/completed_sprints.md` (Sprint B2.1.3) para o historico completo.

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

## Tratamento de Erros Parciais

Quando `SaveChangesAsync` falha para um fornecedor especifico (ex.: violacao de indice unico de CNPJ), o loop de sincronizacao:

1. Captura a excecao no `catch` do use case, sem interromper o processamento dos demais fornecedores do lote.
2. Chama `context.ChangeTracker.Clear()` **antes** de seguir adiante. Sem essa limpeza, a entidade que falhou continua rastreada como `Added`/`Modified` pelo `DbContext`, e o proximo `SaveChangesAsync` — inclusive o final, ao persistir o registro `SincronizacaoFornecedor` — tenta salva-la de novo e repete o mesmo erro, agora fora do bloco de tratamento, derrubando a execucao inteira com HTTP 500.
3. Registra o erro em `ErrosSincronizacoesFornecedores`, com identificacao tecnica do fornecedor e mensagem sanitizada (sem CNPJ/dados sensiveis).
4. Continua a sincronizacao dos fornecedores restantes normalmente.

A execucao finaliza com `Status = "Parcial"` quando ha ao menos um erro e ao menos um sucesso, e `SincronizacoesFornecedores` e sempre gravada ao final, independente de erros parciais terem ocorrido.

> Esse comportamento so se manifesta contra um SQL Server real com indices unicos aplicados — o provider EF Core InMemory usado nos testes unitarios nao impõe essas restricoes por padrao. O teste `Execute_Should_Finish_As_Parcial_And_Persist_Execucao_When_Individual_SaveChanges_Fails` cobre esse cenario com um repositorio de teste que simula a falha real de `SaveChangesAsync`.

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

Com VPN ativa e connection strings configuradas via User Secrets (`dotnet run`):

```bash
dotnet build backend/BlueprintOS.sln
dotnet test backend/BlueprintOS.sln
curl "http://localhost:5262/api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=500"
```

Validar no banco `MaisCompras`:

- fornecedores incluidos/atualizados;
- execucao criada em `SincronizacoesFornecedores`;
- erros parciais em `ErrosSincronizacoesFornecedores`, quando aplicavel.

### Validacao real executada em 02/08/2026

A rotina foi validada de ponta a ponta rodando a API em Docker (`docker compose up -d api`) contra o ERP corporativo `SOMA_DESENV` e o banco `MaisCompras`, via VPN:

```bash
curl -H "X-Development-User-Id: 00000000-0000-0000-0000-000000000001" \
     -H "X-Development-Role: Buyer" \
     "http://localhost:8080/api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=50"
```

Resultado real: `200 OK`, `{"status":"Parcial","consultados":50,"incluidos":48,"atualizados":1,"erros":1}` — exatamente 50 fornecedores consultados, respeitando o teto. Confirmado via `sqlcmd` direto no `MaisCompras`: o registro de execucao foi gravado em `SincronizacoesFornecedores` com os mesmos totais, e o erro parcial foi gravado em `ErrosSincronizacoesFornecedores`, vinculado pela FK correta.

## Limitacoes Conhecidas

- Testes reais de integracao dependem de VPN e secrets locais (User Secrets para `dotnet run`).
- A rotina ainda e acionada via endpoint manual; agendamento operacional fica para sprint futura.
- O ambiente de desenvolvimento nao usa Docker nem SQL Server local (ver ADR-0019); a API sempre aponta para o banco corporativo via `ConnectionStrings`.
