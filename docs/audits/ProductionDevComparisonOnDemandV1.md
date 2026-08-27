# ProductionDevComparisonOnDemandV1 — Comparação PROD × DEV Deixa De Ser Passo Automático

Status: accepted
Data: 2026-08-27
Escopo: `agents/DATABASE_CONNECTION_POLICY.md` § 19/19a.

## 1. Motivação

A investigação authoritative-in-production do caso PROG/OP/PED (`docs/audits/LinxProgOpPed-ProductionInvestigation.md`, R2.8) incluiu uma comparação completa PROD × DEV (schema, procedures, dados). Essa comparação foi útil: validou a arquitetura de `ProductionAuthoritativeInvestigationPolicyV1.md` e revelou o único caso de possível drift encontrado até agora (catálogo de produto incompleto em `SOMA_DESENV`).

Essa comparação, porém, não deve virar ritual. Ela gera trabalho, custo e ruído sem benefício quando repetida em toda investigação — a política já estabelece Produção como fonte de verdade para "como é hoje" (§ 17); uma resposta obtida lá não precisa de corroboração em DEV.

## 2. Correção Aplicada

`agents/DATABASE_CONNECTION_POLICY.md` § 19 ganhou um parágrafo explícito e uma nova § 19a:

- Proveniência de evidência (§ 19) responde "de onde veio a evidência", não "DEV também precisa ser consultado".
- § 19a declara a regra final: comparação PROD × DEV é `COMPARE_ON_DEMAND` (só quando houver razão explícita — suspeita concreta de DEV desatualizado, preparação de alteração em objeto existente, reprodução de cenário, investigação de drift explícita, pedido do usuário, ou evidência concreta de que a diferença afeta a tarefa), nunca `COMPARE_BY_DEFAULT`.
- Nenhuma interpretação anterior de "toda investigação deve comparar PROD × DEV", "toda procedure/schema precisa ser comparado entre ambientes" ou "toda análise de dados precisa de drift report" é válida daqui para frente.

## 3. O Que Não Muda

- Produção continua authoritative para o estado atual (§ 17-18), inalterado.
- Proveniência de evidência por ambiente (`CONFIRMED_IN_PRODUCTION`/`CONFIRMED_IN_DEVELOPMENT`) continua obrigatória (§ 19), inalterada.
- Produção indisponível continua sem fallback silencioso para DEV (§ 20), inalterado.
- § 21 (preparar objeto existente em DEV antes de alterá-lo) continua exigindo comparação, mas **escopada** ao objeto que será efetivamente usado — nunca uma auditoria completa. Isso já era a redação de § 21 antes desta correção; nenhuma mudança foi necessária ali.
- O caso PROG/OP/PED e sua comparação PROD × DEV (R2.8) permanecem registrados como histórico em `docs/audits/LinxProgOpPed-ProductionInvestigation.md`, apenas anotados com nota de não-precedente.

## 4. Enforcement

**Status: DOCUMENTED.** Nenhum código neste repositório força automaticamente uma comparação PROD × DEV — a comparação de R2.8 foi uma decisão do Agent/orquestrador seguindo a redação anterior do objetivo da rodada, não um mecanismo técnico. Não havia código para remover; a correção é inteiramente documental (`agents/DATABASE_CONNECTION_POLICY.md` § 19/19a) e não enfraquece nenhuma proteção de Produção existente (§ 6, § 12, § 18, § 20 permanecem intactos).

## 5. Confirmações

- Nenhuma escrita em banco.
- Nenhum código alterado.
- Nenhum histórico de auditoria reescrito — apenas nota de não-precedente adicionada a `docs/audits/LinxProgOpPed-ProductionInvestigation.md`.
- Nenhum push realizado.
