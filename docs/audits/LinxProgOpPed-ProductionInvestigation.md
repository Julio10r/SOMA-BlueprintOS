# Linx PROG/OP/PED — Investigacao Authoritative Em Producao

Status: **BLOCKED_ON_CONNECTIVITY**
Data: 2026-08-27
Escopo: investigacao read-only em `linx-production` (`SOMA`, `192.168.0.200`) para o caso PROG/OP/PED. Nenhuma escrita, nenhum EXEC mutavel, nenhuma migration.

## 1. Objetivo Desta Rodada

Repetir em producao, somente leitura, a investigacao anteriormente realizada em `SOMA_DESENV` (`docs/audits/AgentLearningV1-LinxProgOpPed.md`, secao 7.6): validar schema, procedures, dados reais da planilha, resolver o gap do tamanho 34, confirmar regras PROG/OP/PED, comparar PROD x DEV, calcular o Delta e produzir uma estrategia de escrita proposta (nao executada). Por `docs/audits/ProductionAuthoritativeInvestigationPolicyV1.md`, producao responde "como e hoje" e e authoritative sobre DEV.

## 2. Ambiente Authoritative Desta Rodada

| Item | Valor |
|---|---|
| Profile | `linx-production` |
| Servidor | `192.168.0.200` |
| Banco | `SOMA` |
| Ambiente | `Production` |
| Modo | READ-ONLY |
| VPN | Obrigatoria |

## 3. Conectividade — BLOQUEADO

Comando executado (read-only, `SELECT 1` + `SELECT SUSER_SNAME()`, mecanismo existente, nenhuma credencial impressa):

```
dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity
```

Resultado, duas execucoes independentes desta sessao, cada uma com 1 retry automatico interno (`B1ConnectivityValidator`, `RetryDelay=750ms`, unico retry permitido para falha de conectividade — nunca para credencial/permissao/mismatch):

```
ERP Linx SOMA (Production) ........ CONNECTIVITYUNAVAILABLE
  Excecao: Microsoft.Data.SqlClient.SqlException
  Codigo SQL: 0
  Mensagem: A network-related or instance-specific error occurred while establishing a connection to
            SQL Server. The server was not found or was not accessible. (provider: TCP Provider, error: 35)
  Servidor: 192.168.0.200
  Banco: SOMA
```

- **CONNECTION STATUS: `CONNECTIVITYUNAVAILABLE`** (nao `NotConfigured`, nao `PermissionDenied`, nao `EnvironmentMismatch`).
- **Houve recovery apos retry? NAO.** As duas tentativas (cada uma com seu proprio retry interno) falharam de forma identica.
- **Credencial esta configurada?** Sim — `ConnectionStrings:LinxProductionConnection` existe em `dotnet user-secrets` nesta maquina (apenas a chave foi inspecionada, nunca o valor). Isto **descarta** a hipotese de "nao configurado" — o problema e estritamente de alcance de rede ao host `192.168.0.200`.
- **Para efeito de comparacao:** `linx-development` (`SOMA_DESENV`, `192.168.9.98`) e `+Compras` (`MAISCOMPRAS`, `192.168.9.98`) responderam **READY** nas mesmas duas execucoes — ou seja, ha conectividade de rede geral funcionando (a VPN corporativa parece ativa o suficiente para alcancar `192.168.9.98`), mas especificamente `192.168.0.200` (o servidor de producao) nao respondeu.

**Por instrucao explicita desta tarefa, esta sessao PAROU aqui.** Nao foi concluido automaticamente que a VPN esta desconectada (o erro pode ser roteamento/firewall/servidor especifico de producao, nao necessariamente a VPN como um todo), e `SOMA_DESENV` **nao foi usado como substituto** de producao para nenhuma das investigacoes pedidas nesta rodada (schema, procedures, grade, PROG/OP/PED, PO 1741979, Delta, impact analysis) — todas permanecem `PENDING_PRODUCTION_READ_ONLY_VALIDATION`, exatamente como estavam ao final da rodada anterior.

## 4. O Que NAO Foi Feito Nesta Rodada (Consequencia Direta Do Bloqueio)

Nenhuma das secoes abaixo pode ser preenchida com evidencia real de producao nesta sessao:

- Validacao de schema em producao (`PRODUCAO_PROG_PROD`, `PRODUCAO_ORDEM`, `PRODUCAO_ORDEM_COR`, `COMPRAS`, `COMPRAS_PRODUTO`, `PRODUTOS`, `PRODUTOS_TAMANHOS`) — **BLOCKED_ON_CONNECTIVITY**.
- Leitura das 4 procedures em producao e comparacao com DEV — **BLOCKED_ON_CONNECTIVITY**.
- Resolucao do gap do tamanho 34 (`PRODUTOS_TAMANHOS` para `GRADE='36-44'` em producao) — **BLOCKED_ON_CONNECTIVITY**. Classificacao permanece `PENDING_PRODUCTION_VALIDATION` (nao promovida a nenhuma das opcoes A-E da secao 7 da tarefa).
- Revalidacao das regras PROG/OP/PED contra dados reais de producao — **BLOCKED_ON_CONNECTIVITY**. As regras *mecanicas* (como cada tipo e identificado estruturalmente) continuam as mesmas descobertas em DEV por inspecao de codigo das procedures (`CONFIRMED_IN_DEVELOPMENT` — ver secao 6), mas nao foram confrontadas com producao.
- Validacao registro-a-registro da planilha (77 linhas) contra producao — **BLOCKED_ON_CONNECTIVITY**.
- Reinvestigacao do caso `PO 1741979` em producao — **BLOCKED_ON_CONNECTIVITY**. Classificacao: `NEEDS_FUNCTIONAL_VALIDATION` (inalterada).
- Calculo de Delta real — **BLOCKED_ON_CONNECTIVITY**.
- Impact analysis quantitativo — **BLOCKED_ON_CONNECTIVITY**.
- PROD x DEV drift report — **NAO PRODUZIDO** (nao ha dado de PROD para comparar; ver secao 6 para o que e reaproveitavel de DEV).
- Estrategia de escrita proposta / SQL proposto — **NAO PRODUZIDO**. Gerar uma proposta tecnica sem nenhum dado confrontado com producao violaria diretamente o principio desta rodada ("producao e a fonte de verdade... nao executar nada para 'ver o que acontece'... entenda primeiro, proponha depois").

Nada disso foi inventado, extrapolado de DEV, ou assumido como valido para producao.

## 5. Nenhuma Alteracao De Escopo

- `SOMA_DESENV` nao foi consultado nesta rodada (nem para leitura nova, nem como fallback).
- Nenhum `INSERT`/`UPDATE`/`DELETE`/`MERGE`/`TRUNCATE`/`DROP`/`CREATE`/`ALTER`/`GRANT`/`REVOKE`/migration foi executado, em nenhum ambiente.
- Nenhuma procedure mutavel foi chamada (`EXEC`) em nenhum ambiente.
- Nenhuma credencial foi impressa, logada ou commitada.
- Os artefatos de entrada (`downloads/showcase_produtos/*.xlsx`, `*.sql`) continuam intocados e fora do Git.

## 6. Conhecimento Reaproveitado De DEV (Rotulado `CONFIRMED_IN_DEVELOPMENT`, Nao Promovido)

Por transparencia, o conhecimento previamente obtido em `SOMA_DESENV` (`docs/audits/AgentLearningV1-LinxProgOpPed.md`, secao 7.6) permanece valido como evidencia de **desenvolvimento**, agora explicitamente rotulado `CONFIRMED_IN_DEVELOPMENT` (nunca `CONFIRMED_IN_PRODUCTION`) ate que producao seja acessivel:

- Mecanica das 4 procedures (parametros, logica, pre-validacoes) — `CONFIRMED_IN_DEVELOPMENT`.
- Estrutura de PK de 4 colunas em `PRODUCAO_PROG_PROD` (`ENTREGA_INICIAL`) e `COMPRAS_PRODUTO` (`ENTREGA`), e o risco de multiplicidade dai decorrente — `CONFIRMED_IN_DEVELOPMENT`.
- Mecanismo `PRODUTOS.GRADE` + `PRODUTOS_TAMANHOS` (posicoes genericas 1..48) — `CONFIRMED_IN_DEVELOPMENT`.
- Regra positiva de classificacao PROG (verificar existencia em `PRODUCAO_PROG_PROD` antes de chamar a procedure, pois ela nao valida rowcount) — `CONFIRMED_IN_DEVELOPMENT`.
- O achado de que `GRADE='36-44'` em DEV nao inclui o tamanho 34 em nenhuma posicao — `CONFIRMED_IN_DEVELOPMENT`, agora reclassificado (por `docs/audits/ProductionAuthoritativeInvestigationPolicyV1.md`) como `DEVELOPMENT_PRODUCTION_DRIFT_SUSPECTED` + `PENDING_PRODUCTION_VALIDATION` em vez de um problema funcional confirmado — permanece nesse estado, pois nao pudemos confronta-lo com producao nesta rodada.

**Nada disso foi promovido a `CONFIRMED_IN_PRODUCTION`, `CONFIRMED_BY_PRODUCTION_SCHEMA`, `CONFIRMED_BY_PRODUCTION_CODE` ou `CONFIRMED_BY_PRODUCTION_DATA` nesta rodada**, exatamente porque nao houve acesso a producao.

## 7. Knowledge Gaps — Status Ao Final Desta Rodada

| Gap | Status |
|---|---|
| Conectividade `linx-production` | **BLOQUEANTE — `CONNECTIVITYUNAVAILABLE`** apos 2 tentativas independentes (cada uma com retry automatico interno). Nao classificado como "VPN desconectada" — apenas o fato tecnico observado (erro 35, TCP Provider, servidor `192.168.0.200` inalcancavel) e reportado. |
| Gap do tamanho 34 (grade `36-44`) | `PENDING_PRODUCTION_VALIDATION` — nao pode ser resolvido nem reclassificado (A-E da tarefa) sem acesso a producao |
| Regra PROG/OP/PED confrontada com producao | `PENDING_PRODUCTION_VALIDATION` |
| PO 1741979 | `NEEDS_FUNCTIONAL_VALIDATION` (inalterado) |
| Delta / impact analysis reais | `PENDING_PRODUCTION_VALIDATION` |

## 8. Proximos Passos

1. **Acao humana necessaria:** verificar/restabelecer a conectividade de rede (VPN e/ou rota) ate `192.168.0.200` (`SOMA`, producao). O erro observado (`TCP Provider, error 35 — servidor nao encontrado ou nao acessivel`) e consistente com falta de rota/VPN para esse host especifico, mas isso nao foi confirmado como causa unica — pode tambem ser firewall ou o servidor de producao estar temporariamente inacessivel por outro motivo.
2. Assim que `validate-b1-connectivity` retornar `READY` para `ERP Linx SOMA (Production)`, reexecutar esta investigacao (schema, procedures, grade, PROG/OP/PED, PO 1741979, Delta, impact analysis, drift PROD x DEV) usando o mesmo mecanismo read-only (`investigate-linx-prog-op-ped`, a adaptar para aceitar o profile `Production` explicitamente).
3. Nao usar `SOMA_DESENV` para preencher nenhuma das lacunas desta secao enquanto producao nao for alcancavel — isso violaria o principio desta rodada.

## 9. Confirmacoes Finais

- Nenhuma escrita executada em nenhum ambiente.
- Nenhum EXEC de procedure mutavel.
- Nenhuma migration.
- Nenhuma credencial exposta (secret scan a ser executado antes do commit).
- `SOMA_DESENV` nao foi usado como substituto de producao.
- Nenhuma reproducao PROD->DEV foi realizada ou proposta (nao ha dados de PROD para propor reproduzir).
- Nenhum push realizado.
