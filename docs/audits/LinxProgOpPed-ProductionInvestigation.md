# Linx PROG/OP/PED — Investigacao Authoritative Em Producao

Status: **BLOCKED_BY_PREREQUISITE — investigacao read-only concluida; caso pausado aguardando regularizacao cadastral de `PRODUTOS.GRADE` (36-44 -> 34-44) por processo operacional externo ao PROG/OP/PED (R2.13; Resume Checkpoint em `AgentLearningV1-LinxProgOpPed.md`)**
Data: 2026-08-27
Escopo: investigacao read-only em `linx-production` (`SOMA`, `192.168.9.200:1433`) para o caso PROG/OP/PED. Nenhuma escrita, nenhum EXEC mutavel, nenhuma migration.

> **Nota de leitura:** este documento tem duas rodadas. A secao "Rodada 1" abaixo (preservada integralmente) documenta a tentativa inicial, bloqueada por um endpoint mal configurado (`192.168.0.200`) — ver a correcao em `docs/audits/LinxProductionEndpointCorrectionV1.md`. A **Rodada 2** (secao 6 em diante) documenta a investigacao completa apos a correcao do endpoint e a confirmacao de `READY` pelo Product Owner.

## Rodada 1 (Historico — Bloqueada Por Endpoint Incorreto)

> **CORRECAO (2026-08-27, etapa posterior):** o diagnostico abaixo concluiu, corretamente com base na evidencia disponivel na epoca, que `192.168.0.200:1433` estava bloqueado/filtrado (`CONNECTIVITYUNAVAILABLE`). Evidencia posterior — uma conexao real e funcional ao `SOMA` fornecida pelo Product Owner — provou que **`192.168.0.200` nunca foi o endpoint SQL real de producao**: o endpoint correto e `192.168.9.200:1433` (`@@SERVERNAME`=`SRV-SOMADB`, TCP, sem instancia nomeada). Ou seja, **nao havia bloqueio de firewall/VPN especifico de porta** — a porta 1433 realmente nao respondia em `192.168.0.200` porque **esse nao e o servidor de producao**. O diagnostico de rede (ping OK, TCP 1433 fechado, TCP 443 aberto) permanece tecnicamente correto para o host `192.168.0.200` testado, mas a **conclusao de causa raiz muda**: nao era "firewall bloqueando a porta certa no host certo", era "testando a porta certa no host errado". Nada abaixo foi apagado — preservado como registro exato do diagnostico feito antes da correcao. Ver `docs/audits/LinxProductionEndpointCorrectionV1.md` para o endpoint corrigido e a nova validacao.

## 1. Objetivo Desta Rodada

Repetir em producao, somente leitura, a investigacao anteriormente realizada em `SOMA_DESENV` (`docs/audits/AgentLearningV1-LinxProgOpPed.md`, secao 7.6): validar schema, procedures, dados reais da planilha, resolver o gap do tamanho 34, confirmar regras PROG/OP/PED, comparar PROD x DEV, calcular o Delta e produzir uma estrategia de escrita proposta (nao executada). Por `docs/audits/ProductionAuthoritativeInvestigationPolicyV1.md`, producao responde "como e hoje" e e authoritative sobre DEV.

> **Nota (2026-08-27, correcao de politica posterior):** a comparacao PROD x DEV feita nesta rodada
> (R2.8) foi util para validar a arquitetura authoritative-in-production e detectar o unico caso de
> possivel drift encontrado ate hoje (catalogo de produto incompleto em DEV). Ela permanece registrada
> como historico. Ela **nao** deve ser lida como precedente de que toda investigacao futura deve repetir
> uma comparacao PROD x DEV — `agents/DATABASE_CONNECTION_POLICY.md` § 19a agora declara essa
> comparacao como `COMPARE_ON_DEMAND` (somente quando houver razao explicita), nao um passo automatico.
> Ver `docs/audits/ProductionDevComparisonOnDemandV1.md`.

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

---

## Rodada 2 — Investigacao Completa Apos Correcao Do Endpoint

### R2.1 Conectividade — READY

Apos a correcao do endpoint (`docs/audits/LinxProductionEndpointCorrectionV1.md`) e a confirmacao do Product Owner de que o secret local `ConnectionStrings:LinxProductionConnection` foi atualizado para `192.168.9.200`, a validacao read-only foi reexecutada:

```
ERP Linx SOMA (Production) ........ READY
  Servidor: 192.168.9.200
  Banco: SOMA
  Identidade efetiva: SOMALABS
```

**CONNECTION STATUS: READY.** Identidade efetiva `SOMALABS` — assim como `ti.n8n` em Development, parece ser uma identidade de servico/integracao, nao necessariamente individual; registrado como observacao, nao como bloqueio (mesmo criterio aplicado anteriormente a Development).

### R2.2 Schema Em Producao — IDENTICO Ao DEV

Investigacao read-only (`investigate-linx-prog-op-ped schema --env=production`) das 5 tabelas (`PRODUCAO_PROG_PROD`, `PRODUCAO_ORDEM`, `PRODUCAO_ORDEM_COR`, `COMPRAS`, `COMPRAS_PRODUTO`): colunas, tipos, nulabilidade, chaves primarias e indices comparados byte-a-byte com o dump equivalente de `SOMA_DESENV` (sessao anterior). **Diff: vazio em todas as 5 tabelas.**

| Objeto | DEV | PROD | Status |
|---|---|---|---|
| `PRODUCAO_PROG_PROD` (colunas/PK/indices) | capturado | capturado | **IDENTICAL** |
| `PRODUCAO_ORDEM` (colunas/PK/indices) | capturado | capturado | **IDENTICAL** |
| `PRODUCAO_ORDEM_COR` (colunas/PK/indices) | capturado | capturado | **IDENTICAL** |
| `COMPRAS` (colunas/PK/indices) | capturado | capturado | **IDENTICAL** |
| `COMPRAS_PRODUTO` (colunas/PK/indices) | capturado | capturado | **IDENTICAL** |

O risco de multiplicidade `ENTREGA_INICIAL`/`ENTREGA` identificado na rodada DEV (`docs/audits/AgentLearningV1-LinxProgOpPed.md` secao 7.6.1) e portanto **CONFIRMED_BY_PRODUCTION_SCHEMA** — as mesmas chaves primarias de 4 colunas existem em producao.

### R2.3 Procedures Em Producao — IDENTICAS Ao DEV

Definicao completa (`OBJECT_DEFINITION`) e parametros (`sys.parameters`) das 4 procedures comparados byte-a-byte com DEV. **Diff: vazio nas 4.**

| Procedure | DEV | PROD | Status |
|---|---|---|---|
| `LX_ANM_GERA_OS_ALTERACAO_PCP` | capturada | capturada | **IDENTICAL** |
| `LX_ANM_AJUSTA_PROGRAMACAO_PROD` | capturada | capturada | **IDENTICAL** |
| `LX_MOVIMENTA_COMPRAS_PA` | capturada | capturada | **IDENTICAL** |
| `LX_RECALCULO_RESERVA_MATERIAIS` | capturada | capturada | **IDENTICAL** |

Toda a mecanica descrita em `docs/audits/AgentLearningV1-LinxProgOpPed.md` secao 7.6 (comportamento de `LX_ANM_AJUSTA_PROGRAMACAO_PROD` nao verificar rowcount, pre-validacao de saldo em `LX_ANM_GERA_OS_ALTERACAO_PCP`, etc.) e portanto **CONFIRMED_BY_PRODUCTION_CODE** — nao apenas `CONFIRMED_IN_DEVELOPMENT`.

### R2.4 Gap Do Tamanho 34 — RESOLVIDO (classificacao B + D da tarefa)

Investigacao dedicada (`investigate-linx-prog-op-ped grade` e `grade-detail --env=production`):

1. **`PRODUTOS.GRADE` e `PRODUTOS_TAMANHOS` para os 39 produtos da planilha em producao:** `GRADE='36-44'` em 100% dos casos (identico a DEV), com `TAMANHO_1..5 = 36,38,40,42,44` — **tamanho 34 continua ausente desta grade em producao**. Isto descarta a hipotese A (`DEVELOPMENT_DRIFT_CONFIRMED`): producao replica exatamente o que foi observado em DEV.

2. **Diferenca real encontrada em relacao a DEV:** em producao, **os 39 produtos existem em `PRODUTOS`** (em DEV, apenas 30 de 39 existiam — 9 nao encontrados). Isto e um **drift real DEV->PROD**, mas na direcao oposta a suspeitada: DEV tinha um catalogo de produtos **incompleto**, nao producao.

3. **Achado decisivo — existe uma grade alternativa que inclui o tamanho 34:** a busca por todas as grades cadastradas que contêm `'34'` em qualquer posicao (`TAMANHO_1..8`) encontrou, entre outras, a grade **`"36 - 44 - 34"`**, com `TAMANHO_1..6 = 36,38,40,42,44,34` — ou seja, o Linx **ja possui um mecanismo comprovado** para acomodar exatamente esta situacao (uma grade de 6 posicoes que estende `36-44` com o tamanho 34 na 6a posicao). Tambem existem outras grades correlatas cadastradas havendo `34` (`34-44`, `34-42`, `34-46`, etc.), cada uma com um layout de posicoes diferente.

4. **Prova quantitativa definitiva (nao suposicao) de que a operacao pretendida e um rebalanceamento de grade, nao uma alteracao de volume:** para as 77 linhas da planilha, comparando `COMPRAS_PRODUTO.CO1..CO5` (quantidade atual real em producao, tamanhos 36-44) contra `Q_36..Q_44` (planilha) e `Q_34` (planilha):

   ```
   ATUAL(36+38+40+42+44) == SOLICITADO(36+38+40+42+44) + Q_34   —   verdadeiro em 77 de 77 linhas (100%)
   ```

   Exemplo real (`PO 1741628`, produto `15.29433`): atual `(26,44,40,20,6)` soma `136`; solicitado `(27,42,36,17,6)` soma `128`; `Q_34=8`; `128+8=136`. **Nenhuma unidade e criada nem destruida** — a planilha pede para retirar uma pequena quantidade de cada tamanho existente (36-44) e realocar exatamente essa quantidade para uma nova posicao de tamanho 34, mantendo o total de pecas por produto/cor/programacao **identico** ao que ja esta comprado/programado em producao.

**Classificacao final do Gap 34: `PRODUCTION_DATA_CONFIRMED` (opcao B) + `OTHER_CONFIRMED_BEHAVIOR` (opcao D).** Producao confirma a mesma configuracao de DEV (nao e drift de cadastro incompleto) **e** o modelo Linx comprovadamente suporta o cenario via grades alternativas ja cadastradas que incluem o tamanho 34. **Nao e mais um problema tecnico nem uma duvida sobre "o Linx consegue representar isso" — o Linx consegue, e ha pelo menos 6 grades candidatas cadastradas que incluem o tamanho 34 combinado com 36-44 de formas ligeiramente diferentes** (`"36 - 44 - 34"`, `34-44`, `34-42`, `34-46`, `34-44 TP`, `34-48`, etc.).

**Residual, estreito, genuinamente para o Product Owner (nao redutivel por schema/codigo/dado):** qual das grades cadastradas com tamanho 34 deve ser usada como a nova `PRODUTOS.GRADE` destes 39 produtos, e se essa mudanca de cadastro deve ser feita antes de qualquer escrita de quantidade. Esta pergunta e puramente de catalogacao/negocio (qual codigo de grade e o certo para esta colecao), nao uma duvida tecnica sobre o mecanismo.

### R2.5 Regra PROG / OP / PED — Confirmada Com Dados Reais De Producao

Cruzamento completo (`investigate-linx-prog-op-ped crossref --env=production`) das 77 linhas contra `PRODUCAO_PROG_PROD`, `PRODUCAO_ORDEM`+`PRODUCAO_ORDEM_COR` e `COMPRAS`+`COMPRAS_PRODUTO`:

| Metrica | Resultado |
|---|---|
| Linhas com match em `PRODUCAO_PROG_PROD` | **77/77** |
| Linhas com match em OP (`PRODUCAO_ORDEM`+`PRODUCAO_ORDEM_COR`) | **0/77** |
| Linhas com match em PED (`COMPRAS`+`COMPRAS_PRODUTO`, por programacao) | **77/77** |
| Linhas sem nenhuma correspondencia (`NAO_ENCONTRADO`) | **0/77** |

**Refinamento importante da regra positiva (secao 7.6.4 do relatorio DEV):** a regra anterior tratava a existencia em `PRODUCAO_PROG_PROD` como suficiente para classificar `PROG`. Os dados reais de producao mostram que isso e **incompleto**: todas as 77 linhas existem simultaneamente em `PRODUCAO_PROG_PROD` **e** em `COMPRAS`/`COMPRAS_PRODUTO` — ou seja, `PRODUCAO_PROG_PROD` guarda o planejamento/programacao de producao (que continua existindo mesmo depois que um Pedido de Compra real e emitido), e nao e, por si so, prova de que a linha deveria ser tratada como "PROG" para fins deste ajuste. A regra correta, alinhada a construcao original do SQL historico (`UNION ALL` de OP e PED, com `PROG` apenas quando nenhum dos dois bate), e uma **prioridade**: **OP > PED > PROG**. Aplicando essa prioridade aos dados reais: **77 linhas = PED, 0 = OP, 0 = PROG, 0 = NAO_ENCONTRADO.**

| Tipo | Contagem (dados reais de producao) |
|---|---|
| PED | **77** |
| OP | 0 |
| PROG | 0 |
| NAO_ENCONTRADO | 0 |
| AMBIGUO | 0 |

### R2.6 PO 1741979 — CONFIRMED_VALID

Consulta read-only a `COMPRAS`+`COMPRAS_PRODUTO` para `PEDIDO=1741979` e `PRODUTO=15.29765` em producao. Resultado real:

```
PEDIDO=1741979; PRODUTO=15.29765; COR=09204; QTDE_ORIGINAL=162; CO1..CO5=(31,67,39,22,3)
PEDIDO=1741979; PRODUTO=15.29765; COR=5465;  QTDE_ORIGINAL=206; CO1..CO5=(39,85,49,29,4)
```

**Classificacao: `CONFIRMED_VALID`.** Mesmo `PEDIDO`, mesmo `PRODUTO`, cores diferentes (`09204`/`5465`), quantidades diferentes por cor — esta e a estrutura normal de um Pedido de Compra com multiplos itens de linha (uma linha por combinacao produto+cor), nao uma inconsistencia de dados. A consulta tambem revelou, como contexto adicional (nao solicitado mas relevante), que o mesmo produto tem **outros 3 pedidos distintos** (`1741976`, `1741977`, `1741978`) para programacoes diferentes (`PA_ATC_MF_INV27_IMP_FE`, `PA_FRA_MF_INV27_IMP_FE`, `PA_MOST_MF_INV27_FE_IMP`) — consistente com o mesmo produto/cor sendo comprado para canais de distribuicao distintos (atacado, um canal adicional "FRA", e uma programacao de mostruario/amostra).

### R2.7 Delta Real — Calculado

Ver R2.4 para a prova quantitativa central. Resumo agregado (`investigate-linx-prog-op-ped delta --env=production`, 77/77 linhas processadas, nenhuma `NAO_ENCONTRADO`):

| Metrica | Valor |
|---|---|
| Linhas processadas | 77 |
| `ZERO_DELTA` (tamanhos 36-44 identicos, sem Q_34) | 0 |
| `CHANGE_REQUIRED` | 77 |
| `NAO_ENCONTRADO` | 0 |
| Total solicitado (tamanhos 36,38,40,42,44) | 14.411 unidades |
| Total atual em producao (tamanhos 36,38,40,42,44) | 15.240 unidades |
| Delta liquido (36-44) | **-829** |
| Total unidades no tamanho 34 (planilha) | **829** |
| Linhas com Q_34 != 0 | 77/77 |

**O delta liquido negativo de 829 nos tamanhos 36-44 e exatamente compensado pelas 829 unidades do tamanho 34** — confirmando, em nivel agregado e por linha (R2.4), que nenhuma unidade e adicionada ou removida do total: e uma realocacao interna da grade.

### R2.8 PROD x DEV Drift Report (Consolidado)

| Item | DEV (`SOMA_DESENV`) | PROD (`SOMA`) | Status |
|---|---|---|---|
| Schema das 5 tabelas (colunas/PK/indices) | capturado (sessao anterior) | capturado (esta sessao) | **IDENTICAL** |
| Definicao das 4 procedures | capturada | capturada | **IDENTICAL** |
| `PRODUTOS.GRADE` dos produtos da planilha | `36-44` (30/39 produtos encontrados) | `36-44` (**39/39 produtos encontrados**) | **DRIFT_DETECTED** — DEV tinha catalogo de produto incompleto (9 produtos ausentes); PROD tem o catalogo completo |
| `PRODUTOS_TAMANHOS` para `GRADE='36-44'` | `TAMANHO_1..5=36,38,40,42,44`, sem 34 | `TAMANHO_1..5=36,38,40,42,44`, sem 34 | **IDENTICAL** |
| Dados PROG/OP/PED (77 linhas da planilha) | 0/77 encontrados em qualquer tabela | 77/77 encontrados (77 PED, 0 OP, 0 PROG) | **PROD_ONLY** — os dados operacionais desta planilha simplesmente nao existem em DEV, exatamente como suspeitado na rodada anterior |
| PO 1741979 | nao encontrado em DEV | encontrado, `CONFIRMED_VALID` | **PROD_ONLY** |
| Delta real (36-44 vs 34) | nao calculavel (`PENDING_PRODUCTION_VALIDATION`) | calculado: -829/+829, rebalanceamento comprovado | **PROD_ONLY** (calculo so possivel com dados de producao) |

### R2.9 Knowledge Provenance — Promovido A `CONFIRMED_IN_PRODUCTION`

Todo o conhecimento abaixo, anteriormente rotulado `CONFIRMED_IN_DEVELOPMENT` (`docs/audits/AgentLearningV1-LinxProgOpPed.md` secao 7.6), foi confrontado com producao e confirmado **identico** — promovido:

- Schema das 5 tabelas (colunas, PK, indices), incluindo o risco de multiplicidade `ENTREGA_INICIAL`/`ENTREGA` — `CONFIRMED_BY_PRODUCTION_SCHEMA`.
- Definicao e comportamento das 4 procedures (incluindo `LX_ANM_AJUSTA_PROGRAMACAO_PROD` nao verificar rowcount do `UPDATE`) — `CONFIRMED_BY_PRODUCTION_CODE`.
- Mecanismo `PRODUTOS.GRADE` + `PRODUTOS_TAMANHOS` (posicoes genericas ate 48, uma grade por codigo) — `CONFIRMED_BY_PRODUCTION_SCHEMA` + `CONFIRMED_BY_PRODUCTION_DATA`.
- Regra de classificacao PROG/OP/PED com prioridade `OP > PED > PROG` — `CONFIRMED_BY_PRODUCTION_DATA` (refinada nesta rodada; a versao anterior, apenas "existe em `PRODUCAO_PROG_PROD`", ficou provada **incompleta** pelos dados reais).
- Existencia de grades alternativas cadastradas que incluem o tamanho 34 (`"36 - 44 - 34"` e outras) — `CONFIRMED_BY_PRODUCTION_DATA`.
- Natureza de rebalanceamento de grade da operacao representada pela planilha (total inalterado, apenas realocacao entre tamanhos) — `CONFIRMED_BY_PRODUCTION_DATA` (prova quantitativa em 77/77 linhas).

**Nada foi promovido por inferencia ou extrapolacao de DEV** — cada item acima foi obtido por uma consulta read-only real contra `SOMA` nesta sessao.

### R2.10 Conhecimento Incorporado Ao Agent Linx (Modelo, Nao O Caso)

Generalizavel (persistido como conhecimento reutilizavel do dominio Linx, nao como fato desta planilha):

- Prioridade de classificacao de ajuste de grade Linx: **OP (existe Ordem de Producao) > PED (existe Pedido de Compra) > PROG (existe apenas registro de programacao em `PRODUCAO_PROG_PROD`) > NAO_ENCONTRADO**.
- A existencia isolada de um registro em `PRODUCAO_PROG_PROD` **nao** implica que a linha deva ser tratada como "apenas programacao" — um Pedido de Compra real pode coexistir com o registro de programacao.
- O Linx representa grade via um campo `GRADE` (codigo) no cadastro de produto e uma tabela `PRODUTOS_TAMANHOS` (chave `GRADE`) que mapeia posicoes 1..48 a tamanhos fisicos reais — **para adicionar um tamanho a uma linha de produto, o codigo de grade do produto precisa ser alterado para um codigo que inclua esse tamanho**, nao apenas escrever na posicao numerica.
- Antes de propor qualquer ajuste de grade, e valido e util verificar se o total de pecas solicitado bate com o total atual — isso distingue uma operacao de **rebalanceamento** (total constante) de uma operacao de **alteracao de volume** (total muda), que tem implicacoes de negocio e de aprovacao muito diferentes.

**Nao generalizado / mantido como particularidade desta execucao:** os 39 produtos especificos, a grade `36-44` especifica, os numeros 829/14.411/15.240, o PO `1741979`, e as programacoes especificas (`PA_ATC_MF_INV27_IMP_JAS` etc.) — tudo isso e evidencia do caso, nao regra do modelo Linx.

## R2.11 Estrategia Tecnica — Nao Proposta Ainda (Gap Residual Bloqueante)

> **Correcao de aprendizado (nova etapa, ver `docs/audits/AgentLearningV1-LinxProgOpPed.md` secao 7.11.6):** a conclusao abaixo — de que a solucao passa por trocar `PRODUTOS.GRADE` destes produtos para um codigo cadastrado que inclua o rotulo visual "34" — foi construida pela interpretacao do **rotulo visual** do tamanho, nao pela semantica **posicional** da grade Linx (regra funcional ensinada pelo Product Owner: `TAM_1` da planilha = posicao 1 da estrutura Linx, independente do rotulo fisico que estiver cadastrado naquela posicao para aquele produto). **Esta conclusao nao deve mais ser mantida como a regra geral do modelo.** O texto original abaixo permanece integralmente para historico. A aplicacao estrita da regra posicional a este caso especifico tambem nao produz uma resposta trivial (ver `AgentLearningV1-LinxProgOpPed.md` 7.11.6 para o detalhamento: mapear a 1a coluna de quantidade da planilha, `Q_34`, para a posicao 1 da grade `36-44` faria a 6a coluna, `Q_44` — nao-zero nas 77 linhas — cair fora das 5 posicoes cadastradas, o que tambem nao bate com a premissa de rebalanceamento legitimo). A pergunta objetiva ao Product Owner foi revisada nessa mesma secao 7.11.6: o que falta confirmar e a correspondencia exata coluna-da-planilha <-> posicao-numerica-Linx, nao qual rotulo de grade cadastrado usar.

Apesar do progresso substancial (schema/procedures/mecanismo de grade/regra PROG-OP-PED/Delta todos confirmados com dados reais de producao), **uma decisao de negocio ainda bloqueia a proposta de solucao final**: qual codigo de grade cadastrado (`"36 - 44 - 34"`, `34-44`, ou outro) deve ser atribuido a estes 39 produtos para acomodar o tamanho 34. Gerar SQL sem essa decisao seria adivinhar um valor de catalogo, violando o principio desta tarefa.

**Pergunta objetiva final ao Product Owner (texto original, ver correcao acima):** confirmado que a operacao e um rebalanceamento de grade (total de pecas inalterado, comprovado em 77/77 linhas) e que o Linx ja possui grades cadastradas que incluem o tamanho 34 combinado com 36-44 (`"36 - 44 - 34"` entre outras) — **qual codigo de grade deve ser usado como o novo `PRODUTOS.GRADE` destes produtos?** Se a resposta for `"36 - 44 - 34"` (a correspondencia mais direta com a grade atual), o Agent pode entao projetar a estrategia tecnica completa (mudanca de cadastro de grade + redistribuicao de quantidade + procedures aplicaveis) na proxima etapa.

### R2.13 Resposta do Product Owner/compradora responsavel (2026-08-27) — pre-requisito confirmado, execucao bloqueada

A duvida de correspondencia coluna-planilha <-> posicao-Linx (`AgentLearningV1-LinxProgOpPed.md` 7.11.6) foi resolvida pelo Product Owner apos consulta a compradora responsavel: **os produtos desta planilha precisam ter `PRODUTOS.GRADE` alterado de `'36-44'` para `'34-44'`**. A divergencia de grade detectada pelo Agent nesta rodada **era um pre-requisito cadastral real**, nao erro de interpretacao nem confusao rotulo-visual/posicional.

**A alteracao de `PRODUTOS.GRADE` fica fora do escopo do ajuste PROG/OP/PED.** Segundo o Product Owner, essa mudanca especifica exige processo proprio (autorizacoes, liberacao/validacao de Auditoria, participacao do time do CD, ajuste de saldo de estoque, demais controles operacionais) — o Agent nao deve propor SQL, ActionProposal, DRY_RUN nem tentar automatizar essa troca de grade dentro deste caso.

**Status final desta rodada: `BLOCKED_BY_PREREQUISITE`** (motivo `PRODUCT_GRADE_REGISTRATION_MISMATCH`). Ver "Resume Checkpoint" em `docs/audits/AgentLearningV1-LinxProgOpPed.md` para os passos de retomada quando a regularizacao cadastral for concluida — o conhecimento canonico ja confirmado nesta investigacao (chave funcional, classificacao PROG/PED/OP, grade posicional, delta vs quantidade final, as 4 procedures) permanece valido e nao precisa ser reconstruido do zero.

## R2.12 Confirmacoes Finais Desta Rodada

- Nenhuma escrita executada em producao ou em qualquer outro ambiente.
- Nenhum EXEC de procedure mutavel — apenas `SELECT`, `OBJECT_DEFINITION`, `sys.parameters`, `INFORMATION_SCHEMA`.
- Nenhuma migration.
- Nenhuma credencial exposta.
- `LIVE_EXECUTION` permanece desabilitado.
- Nenhuma reproducao PROD->DEV foi realizada.
- Nenhum push realizado.
