# Ingestão de Conhecimento Linx — Fornecedor/CNPJ

## Metadados

- Status: Concluído — ingestão completa das 28 unidades de conhecimento reutilizável identificadas no snapshot temporário
- Tipo: Governança de conhecimento — consolidação do snapshot temporário `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` no mecanismo canônico de conhecimento dos Agents Especialistas Linx
- Data desta rodada: 2026-08-27 (segunda rodada, completa a rodada anterior de mesma data)
- Regras seguidas: `agents/EXECUTION_POLICY.md`, `agents/AGENT_CONTRACT.md`, `agents/linx-erp-specialist-agent/agent.yaml`, `agents/linx-database-specialist-agent/agent.yaml`
- Nenhum acesso a ambiente externo (PROD/DEV/SQL Server/WISE/API) foi realizado nesta tarefa — consolidação estritamente offline de conhecimento já descoberto.

---

## 0. Mudança de política de proveniência (correção desta rodada)

A rodada anterior exigia **"dupla proveniência"**: uma unidade só era ingerida se aparecesse tanto no snapshot temporário quanto em um dos dois documentos de auditoria canônicos (`docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md`, `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`). Isso descartou 8 seções inteiras do snapshot (§2.2, §6.2/6.3 parcialmente, §8, §9, §10, §12, §13-A a §13-D), incluindo decisões já aprovadas pelo Product Owner e descobertas de código-fonte VFP real.

**Correção de política, adotada nesta rodada**: a dupla proveniência artificial não é mais exigida. Se o conhecimento se origina legitimamente de (a) análise de código VFP (`.SCX`/`.SCT`/`.PRG`/`.VCX`), (b) discovery de procedure/artefato técnico de banco, (c) decisão funcional do Product Owner, ou (d) discovery documentado no próprio snapshot temporário — **o próprio snapshot é fonte de proveniência legítima e suficiente**. Nenhuma unidade é descartada só por não ter espelho em dois documentos.

Para preservar rastreabilidade sem reintroduzir ambiguidade, cada unidade agora carrega um campo `sourceType` (obrigatório, validado pelo gerador) com um dos valores:

- `VFP_CODE_DISCOVERY` — descoberta por leitura direta de código-fonte VFP real (tela/framework).
- `DATABASE_PROCEDURE_DISCOVERY` — descoberta por leitura de schema/procedure/trigger via SQL Server (`sys.*`/`OBJECT_DEFINITION`).
- `PRODUCT_OWNER_DECISION` — decisão funcional confirmada pelo Product Owner.
- `ARCHITECTURAL_DECISION` — decisão de arquitetura do domínio +Compras (fronteira, contratos, taxonomias).
- `TEMP_DISCOVERY_SNAPSHOT` — síntese/generalização/template cuja evidência primária é o próprio snapshot (metodologia, contrato consolidado, lista de desconhecidos, gap de infraestrutura), sem espelho literal em outro documento.

Mais um campo auxiliar `source_ref` aponta para a seção exata do snapshot (ex.: `#13-A`), complementando (nunca substituindo) o campo `fonte` já existente, que continua sendo a referência de rastreabilidade primária (arquivo + seção/linha). Gaps técnicos que afetam o comportamento futuro dos Agents (não regra de negócio) recebem um campo auxiliar `gapType` ∈ {`ARCHITECTURE_GAP`, `IMPLEMENTATION_CONSTRAINT`, `KNOWN_LIMITATION`}. Decisões do PO preservam `proveniencia: "Validado"` (nunca rebaixadas a `Descoberto`/`Inferido`) — nenhuma unidade nasce `Aprovado`, conforme a máquina de estados real de `LinxKnowledgeEntry`.

---

## 1. Mecanismo canônico encontrado

A arquitetura Agents v1 já possui um mecanismo canônico de conhecimento Linx persistido e versionado, **anterior a esta tarefa**:

- `LinxKnowledgeEntry` (`applications/mais-compras/backend/src/BlueprintOS.Domain/Knowledge/Linx/LinxKnowledgeEntry.cs`) — entrada versionada (nunca sobrescrita in-place; nova versão = nova linha, encadeada por `EntradaAnteriorId`/`VersaoRaizId`), com máquina de estados de proveniência `Descoberto → Inferido → Validado → Aprovado` (`LinxConhecimentoProveniencia.cs`).
- `ILinxKnowledgeRepository`/`LinxKnowledgeRepository` (`Application/Knowledge/Linx/Contracts/ILinxKnowledgeRepository.cs`, `Infrastructure/Knowledge/Linx/LinxKnowledgeRepository.cs`) — persistência via EF Core/SQL Server, migration `20260811230715_AddLinxKnowledgeO1135`.
- `RegistrarConhecimentoUseCase`/`BuscarConhecimentoUseCase` (`Application/Knowledge/Linx/LinxKnowledgeUseCases.cs`) — únicos pontos de escrita de conhecimento (adicionar versão nova, nunca editar conteúdo existente).
- Consumido em runtime por `LinxErpSpecialistAgent`/`LinxDatabaseSpecialistAgent` (`Application/Knowledge/Linx/LinxSpecialistAgents.cs`), injetados via `IBuscarConhecimentoUseCase`.

**Limitação real e documentada** (registrada na rodada anterior e ainda vigente): esse mecanismo depende de um banco SQL Server real conectado via EF Core para persistir qualquer `LinxKnowledgeEntry`. Não existe seeder/`HasData`/rotina de carga em lote no repositório para popular a partir de um arquivo estático — cada entrada nasce por uma chamada de aplicação real (`RegistrarConhecimentoUseCase.ExecuteAsync`), com `ator`/RBAC. Como esta tarefa é **estritamente offline**, não é seguro nem possível gravar `LinxKnowledgeEntry` reais nesta rodada. Esta limitação está, ela própria, agora capturada como unidade de conhecimento (`linx-gap-infraestrutura-persistencia-linxknowledgeentry`, `sourceType: TEMP_DISCOVERY_SNAPSHOT`, `gapType: IMPLEMENTATION_CONSTRAINT`).

O handbook canônico do módulo Knowledge (`.ai/context/knowledge.md`) reconhece essa lacuna e define o fallback oficial já em uso por outros conhecimentos Linx/WISE — persistência em `.ai/context/`/`agents/knowledge/` linkada a partir do handbook, mesmo padrão já usado por `linx-wise-daily-integration.md`/`wise-knowledge.md`. Esta ingestão continua seguindo esse mesmo mecanismo já certificado, sem criar um segundo sistema paralelo de memória.

---

## 2. O que foi ingerido

### 2.1 — Fonte estruturada (entrada, editável)

`agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json` — array de **28 unidades de conhecimento** (16 da rodada anterior + 12 novas desta rodada) com chave estável (kebab-case). Cada unidade preserva: especialista responsável (`LinxErpSpecialist`/`LinxDatabaseSpecialist`), categoria, assunto, entidade/tabela/procedure Linx, campos, conteúdo, proveniência (`Descoberto`/`Inferido`/`Validado`), confiança, `sourceType`, `source_ref` (quando aplicável), `gapType` (quando aplicável), fonte rastreável, restrições/observações e tags.

### 2.2 — Artefato canônico gerado (saída, nunca editado à mão)

`agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md` — Markdown com uma âncora `<!-- linx-knowledge-unit: <key> -->` por unidade, gerado por `tools/agents/generate-linx-fornecedor-knowledge.js`. O gerador agora também: valida que todo `sourceType` está em um conjunto fechado de 5 valores; exige `fonte` e/ou `source_ref` em toda unidade; e renderiza `Tipo de origem (sourceType)`, `Classificação de gap (gapType)` e `Referência de origem (source_ref)` quando presentes. **Idempotência comprovada**: rodar o gerador duas vezes seguidas produz hash SHA-256 idêntico — verificado manualmente (`61ab5f7755effc54ffed60de131169c69f22fdbcdee6da3ab954972c98b29bfa` nas duas execuções) e no teste automatizado.

### 2.3 — 12 novas unidades desta rodada

1. `linx-especializacao-papel-fornecedores-clientes-filiais` — colunas de negócio de `FORNECEDORES`/`CLIENTES_ATACADO`/`FILIAIS` (§2.2). `DATABASE_PROCEDURE_DISCOVERY`.
2. `linx-mapa-conceitual-efeitos-colaterais-escrita` — template reutilizável de mapa de efeitos colaterais (§6.2). `TEMP_DISCOVERY_SNAPSHOT`.
3. `linx-principio-escrita-nunca-so-local` — princípio de que escrita Linx nunca é só local (§6.3). `TEMP_DISCOVERY_SNAPSHOT`.
4. `linx-contrato-futuro-adapter-linx-fornecedor` — contrato classificado OBRIGATÓRIO/RECOMENDADO/NÃO-ENTRA do futuro Adapter (§8). `TEMP_DISCOVERY_SNAPSHOT`.
5. `linx-desconhecidos-preservados-adapter-fornecedor` — lista consolidada de desconhecidos que bloqueiam o Adapter (§9). `TEMP_DISCOVERY_SNAPSHOT`, `gapType: KNOWN_LIMITATION`.
6. `linx-playbook-discovery-reutilizavel-14-passos` — playbook de discovery Linx passo a passo (§10). `TEMP_DISCOVERY_SNAPSHOT`.
7. `linx-gap-infraestrutura-persistencia-linxknowledgeentry` — ausência de infraestrutura local para persistir `LinxKnowledgeEntry` (§12). `TEMP_DISCOVERY_SNAPSHOT`, `gapType: IMPLEMENTATION_CONSTRAINT`.
8. `linx-metodologia-discovery-codigo-fonte-tela-vfp` — playbook de descoberta via código real da tela (`TRANSACOES → SCX/SCT/PRG`) (§13-A). `VFP_CODE_DISCOVERY`.
9. `linx-achados-tela-vfp-fornecedor-001016g1` — regras de identidade/duplicidade confirmadas na tela real `001016G1` (§13-A.1). `VFP_CODE_DISCOVERY`.
10. `linx-framework-persistencia-l-salva` — mecanismo genérico de transação/persistência `lx_class.vcx::l_salva` (§13-B). `VFP_CODE_DISCOVERY`.
11. `linx-decisoes-po-fronteira-bu-erp-fornecedor` — decisões aprovadas pelo PO sobre fronteira BU↔ERP/identidade/multiuso (§13-C). `PRODUCT_OWNER_DECISION`.
12. `linx-idempotencia-convergencia-create-update-fornecedor` — idempotência de escrita e convergência CREATE→UPDATE (§13-D). `PRODUCT_OWNER_DECISION`.

As 16 unidades da rodada anterior foram revalidadas e retroativamente rotuladas com `sourceType` (`DATABASE_PROCEDURE_DISCOVERY` para as 11 unidades de schema/trigger/procedure; `ARCHITECTURAL_DECISION` para as 5 unidades de arquitetura +Compras) — nenhum conteúdo textual dessas 16 foi alterado, apenas o campo estrutural novo foi adicionado.

---

## 3. Agents que consomem o conhecimento

`linx-erp-specialist-agent/agent.yaml` e `linx-database-specialist-agent/agent.yaml` continuam apontando para a mesma fonte compartilhada (`context_paths`), sem duplicação física — inalterado nesta rodada, verificado novamente pelo teste automatizado.

---

## 4. Tabela de comparação sistemática (todas as seções do snapshot)

| Seção do snapshot | Conhecimento | Unidade estruturada correspondente | Proveniência (`sourceType`) | Status |
|---|---|---|---|---|
| Cabeçalho (STATUS/FINALIDADE) | Metadado sobre o próprio arquivo e seu propósito temporário | — | — | NON_KNOWLEDGE_DOCUMENTATION |
| "Como ler este documento" | Legenda de mapeamento de campos para `LinxKnowledgeEntry` | Refletido no schema/gerador (`fonte`, `proveniencia`, `confianca`, `sourceType`, `source_ref`) | — | NON_KNOWLEDGE_DOCUMENTATION |
| §1.1 | `LX_CADE`/`LX_CADE_COLUNA`/`ANM_BUSCA_INSTRUCAO` (ferramentas read-only de discovery) | `linx-ferramentas-discovery-schema-readonly` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §1.2 | `LX_SEQUENCIAL`/tabela `SEQUENCIAIS` | `linx-procedure-lx-sequencial` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §1.3 | Metodologia de discovery estrutural para escrita futura | `linx-metodologia-investigacao-escrita-futura` + `linx-playbook-discovery-reutilizavel-14-passos` (§10, detalhado) | DATABASE_PROCEDURE_DISCOVERY / TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §2.1 | `CADASTRO_CLI_FOR` como entidade-base multiuso | `linx-tabela-mestre-cadastro-cli-for` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §2.2 | Especializações `FORNECEDORES`/`CLIENTES_ATACADO`/`FILIAIS` | `linx-especializacao-papel-fornecedores-clientes-filiais` (NOVA) | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §2.3 | Endereço triplicado/contato/CNAE único/ausência de QSA | `linx-enderecos-triplicados-cadastro-cli-for` + `arquitetura-nao-persistir-qsa-cnae-secundario` | DATABASE_PROCEDURE_DISCOVERY / ARCHITECTURAL_DECISION | COVERED |
| §2.4 | Geração de `CLIFOR`/`COD_CLIFOR` via sequencial; `NOME_CLIFOR` por sanitização | `linx-nome-clifor-nao-vem-de-sequencial` + `linx-procedure-lx-sequencial` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §2.5 | Anomalia: sequencial de cliente usado para papel de fornecedor | `linx-sequenciais-concorrentes-fornecedor` + `linx-procedure-p-rsv-integracao-cadastro-fornecedor` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §3.1–3.5 | As 5 procedures de integração analisadas (síntese individual) | `linx-procedure-p-rsv-integracao-cadastro-fornecedor` (detalhe) + `linx-padrao-nivel-1-cinco-implementacoes-cadastro` (síntese das 5) | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §4 | Padrão recorrente por nível de confiança (Nível 1/2/3) | `linx-padrao-nivel-1-cinco-implementacoes-cadastro` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §5 | Tabela consolidada de exceções/anomalias | `linx-sequenciais-concorrentes-fornecedor` + `linx-procedure-p-rsv-integracao-cadastro-fornecedor` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §6.1 | 11 triggers ativas em `CADASTRO_CLI_FOR` | `linx-triggers-cadastro-cli-for` | DATABASE_PROCEDURE_DISCOVERY | COVERED |
| §6.2 | Mapa conceitual reutilizável (template) | `linx-mapa-conceitual-efeitos-colaterais-escrita` (NOVA) | TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §6.3 | Princípio: escrita Linx nunca é só local | `linx-principio-escrita-nunca-so-local` (NOVA) | TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §7 | Fronteira de camadas Provider→Adapter→Domínio→Adapter Linx; `DocumentoFiscal` canônico | `arquitetura-fronteira-adapter-linx` + `arquitetura-documento-fiscal-canonico` | ARCHITECTURAL_DECISION | COVERED |
| §8 | Contrato futuro do Adapter Linx (OBRIGATÓRIO/RECOMENDADO/AINDA DESCONHECIDO/NÃO ENTRA) | `linx-contrato-futuro-adapter-linx-fornecedor` (NOVA) | TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §9 | Desconhecidos preservados explicitamente (rotina manual, filas ETL, critério de duplicidade, etc.) | `linx-desconhecidos-preservados-adapter-fornecedor` (NOVA) | TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §10 | Playbook de discovery Linx reutilizável (14 passos) | `linx-playbook-discovery-reutilizavel-14-passos` (NOVA) | TEMP_DISCOVERY_SNAPSHOT | COVERED |
| §11 | Nota narrativa sobre trajetória de proveniência vinda do PO | Refletida na convenção de proveniência do schema (não é fato Linx isolado) | — | NON_KNOWLEDGE_DOCUMENTATION |
| §12 | GAP de infraestrutura local para `LinxKnowledgeEntry` + sequência de ação futura | `linx-gap-infraestrutura-persistencia-linxknowledgeentry` (NOVA); a "sequência de ação futura" em si é processo, não conhecimento Linx | TEMP_DISCOVERY_SNAPSHOT | COVERED (fato) / NON_KNOWLEDGE_DOCUMENTATION (checklist de processo) |
| §13-A | Metodologia de descoberta via código-fonte real da tela VFP | `linx-metodologia-discovery-codigo-fonte-tela-vfp` (NOVA) | VFP_CODE_DISCOVERY | COVERED |
| §13-A.1 | Achados de identidade/duplicidade confirmados na tela `001016G1` | `linx-achados-tela-vfp-fornecedor-001016g1` (NOVA) | VFP_CODE_DISCOVERY | COVERED |
| §13-B | Mecanismo genérico `lx_class.vcx::l_salva` | `linx-framework-persistencia-l-salva` (NOVA) | VFP_CODE_DISCOVERY | COVERED |
| §13-C | Decisões do PO — fronteira BU/ERP, identidade, multiuso | `linx-decisoes-po-fronteira-bu-erp-fornecedor` (NOVA) | PRODUCT_OWNER_DECISION | COVERED |
| §13-D | Idempotência/convergência CREATE→UPDATE (decisão do PO) | `linx-idempotencia-convergencia-create-update-fornecedor` (NOVA) | PRODUCT_OWNER_DECISION | COVERED |
| §13 (numeração ADR) | Nota de processo sobre número provisório de ADR (0020) | — | — | NON_KNOWLEDGE_DOCUMENTATION |

**Nenhuma linha permanece `STILL_MISSING`.**

---

## 5. Testes executados

`tools/agents/generate-linx-fornecedor-knowledge.test.js` — estendido nesta rodada com:

- Validação de que toda unidade tem `sourceType` em um conjunto fechado de 5 valores.
- Cobertura mínima de uma unidade real para cada um dos 5 `sourceType`.
- Existência de ao menos uma unidade cuja única proveniência é o snapshot temporário (cenário que a política antiga rejeitava).
- Presença de `gapType` classificando corretamente gaps técnicos (`ARCHITECTURE_GAP`/`IMPLEMENTATION_CONSTRAINT`/`KNOWN_LIMITATION`).
- Decisões do PO preservadas com `proveniencia: Validado` (nunca rebaixadas).
- Descobertas VFP preservando grau de confiança.
- Artefato gerado expõe `sourceType`/`source_ref` de forma recuperável.
- Mantidos: idempotência real (hash SHA-256 duas execuções), chaves únicas, `render()` puro, recuperação de termos-chave, recuperação por cada Agent, ausência de duplicação física entre Agents, referência de `knowledge.md`.

### Resultado da execução completa

```
$ node tools/agents/generate-linx-fornecedor-knowledge.js
Gerado .../linx-fornecedor-knowledge.generated.md com 28 unidade(s) de conhecimento. Conteúdo atualizado.
$ node tools/agents/generate-linx-fornecedor-knowledge.js
Gerado .../linx-fornecedor-knowledge.generated.md com 28 unidade(s) de conhecimento. Idempotente: conteúdo idêntico à execução anterior.
(hash SHA-256 idêntico nas duas execuções: 61ab5f7755effc54ffed60de131169c69f22fdbcdee6da3ab954972c98b29bfa)

$ node tools/agents/validate-agent-manifests.js
PASS: 8 Agent Contract v1.1 manifests validated
PASS: IDs unique: agent-factory, echo-agent, knowledge-agent, linx-database-specialist-agent, linx-erp-specialist-agent, security-lgpd-agent, showcase-agent, wise-agent
PASS: capability ownership, delegation, gap and credential policies valid
PASS: Agent references and required paths exist
PASS: no bypass, privilege escalation or secret values detected

$ node tools/agents/*.test.js (todos, incluindo o atualizado)
PASS: Agent Factory v2 lifecycle, audit and safety tests
OK — generate-linx-fornecedor-knowledge.test.js: 28 unidades, idempotência e proveniência verificadas.
PASS: JS orchestrator -> governed-plan CLI -> .NET GovernedPlanBridge end-to-end offline bridge test
PASS: Governed Orchestrator v1 context, routing, cross-cutting and safety tests
PASS: Runtime Registry v1 discovery, routing, gaps, conflicts and safety tests
PASS: Showcase Agent offline safety invariants
PASS: 7 negative Agent Contract v1.1 validation scenarios rejected
PASS: WISE Agent offline safety invariants
```

Agents v1: **PASS, 0 ERROR, 0 ACTION_REQUIRED.**

---

## 6. Status final do snapshot temporário

`agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` — **todo conhecimento reutilizável identificado na revisão completa (tabela da seção 4) tem unidade estruturada correspondente `COVERED`**. Nenhuma seção permanece `STILL_MISSING`.

**Status: `TEMP_SNAPSHOT = SUPERSEDED`** (superseded pelo conteúdo canônico em `agents/knowledge/linx-fornecedor-cnpj/`).

**Ação explicitamente NÃO executada nesta rodada**: a movimentação física do arquivo para `.empty/legacy/agents/ai-factory/` fica para uma próxima etapa — não foi movida agora, por instrução explícita desta tarefa. O arquivo permanece no local atual, git-rastreado, até essa etapa futura.

---

## 7. Arquivos criados/modificados nesta rodada

Modificados:
- `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json` (16 → 28 unidades; `sourceType` retroativo nas 16 originais)
- `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md` (regenerado, idempotente)
- `tools/agents/generate-linx-fornecedor-knowledge.js` (validação de `sourceType`/`fonte`/`source_ref`; renderização de `sourceType`/`gapType`/`source_ref`)
- `tools/agents/generate-linx-fornecedor-knowledge.test.js` (cobertura da nova política de proveniência, sourceTypes, gapType, decisões do PO, descobertas VFP)
- `docs/repository/LinxKnowledgeFornecedor-Ingestion.md` (este relatório)

Não modificados: `agents/linx-erp-specialist-agent/agent.yaml`, `agents/linx-database-specialist-agent/agent.yaml`, `.ai/context/knowledge.md` (já apontavam corretamente para o artefato compartilhado desde a rodada anterior — nenhuma mudança de referência necessária). `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` permanece no lugar (ver seção 6).
