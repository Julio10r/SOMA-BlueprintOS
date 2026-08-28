# Ingestão de Conhecimento Linx — Fornecedor/CNPJ

## Metadados

- Status: Concluído (ingestão parcial e deliberadamente escopada — ver seção "Gaps")
- Tipo: Governança de conhecimento — consolidação do snapshot temporário `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` no mecanismo canônico de conhecimento dos Agents Especialistas Linx
- Data: 2026-08-27
- Regras seguidas: `agents/EXECUTION_POLICY.md`, `agents/AGENT_CONTRACT.md`, `agents/linx-erp-specialist-agent/agent.yaml`, `agents/linx-database-specialist-agent/agent.yaml`
- Nenhum acesso a ambiente externo (PROD/DEV/SQL Server/WISE/API) foi realizado nesta tarefa — consolidação estritamente offline de conhecimento já descoberto.

---

## 1. Mecanismo canônico encontrado

A arquitetura Agents v1 já possui um mecanismo canônico de conhecimento Linx persistido e versionado, **anterior a esta tarefa**:

- `LinxKnowledgeEntry` (`applications/mais-compras/backend/src/BlueprintOS.Domain/Knowledge/Linx/LinxKnowledgeEntry.cs`) — entrada versionada (nunca sobrescrita in-place; nova versão = nova linha, encadeada por `EntradaAnteriorId`/`VersaoRaizId`), com máquina de estados de proveniência `Descoberto → Inferido → Validado → Aprovado` (`LinxConhecimentoProveniencia.cs`).
- `ILinxKnowledgeRepository`/`LinxKnowledgeRepository` (`Application/Knowledge/Linx/Contracts/ILinxKnowledgeRepository.cs`, `Infrastructure/Knowledge/Linx/LinxKnowledgeRepository.cs`) — persistência via EF Core/SQL Server, migration `20260811230715_AddLinxKnowledgeO1135`.
- `RegistrarConhecimentoUseCase`/`BuscarConhecimentoUseCase` (`Application/Knowledge/Linx/LinxKnowledgeUseCases.cs`) — únicos pontos de escrita de conhecimento (adicionar versão nova, nunca editar conteúdo existente).
- Consumido em runtime por `LinxErpSpecialistAgent`/`LinxDatabaseSpecialistAgent` (`Application/Knowledge/Linx/LinxSpecialistAgents.cs`), injetados via `IBuscarConhecimentoUseCase`.

**Limitação real e documentada, confirmada nesta tarefa**: esse mecanismo depende de um banco SQL Server real conectado via EF Core (a aplicação `.NET`/DbContext) para persistir qualquer `LinxKnowledgeEntry`. Não existe seeder/`HasData`/rotina de carga em lote no repositório para popular `LinxKnowledgeEntry` a partir de um arquivo estático — cada entrada nasce por uma chamada de aplicação real (`RegistrarConhecimentoUseCase.ExecuteAsync`), com `ator`/RBAC. Como esta tarefa é **estritamente offline** (proibição explícita de tocar SOMA_DESENV/PROD/qualquer SQL Server) e não há como rodar a aplicação `.NET` contra um banco real aqui, **não é seguro nem possível gravar `LinxKnowledgeEntry` reais nesta rodada** sem violar a regra de "nenhum ambiente externo".

O próprio handbook canônico do módulo Knowledge (`.ai/context/knowledge.md`) já reconhece essa lacuna e define o fallback oficial já em uso por outros conhecimentos Linx/WISE:

> "Enquanto o módulo Knowledge persistente/versionado não expõe uma rotina de ingestão automática para este tipo de runbook operacional, conhecimentos canônicos validados podem ser persistidos em `.ai/context/` e linkados a partir deste handbook."

Esse é exatamente o padrão já usado por `linx-wise-daily-integration.md` e `wise-knowledge.md` — ambos já referenciados em `context_paths` de `linx-erp-specialist-agent`/`linx-database-specialist-agent`. Esta ingestão **segue esse mesmo mecanismo já certificado**, evoluindo-o (conforme exigido pela tarefa) de um Markdown solto para uma **fonte estruturada + gerador determinístico**, em vez de criar um segundo sistema paralelo de memória.

---

## 2. O que foi ingerido

### 2.1 — Fonte estruturada (entrada, editável)

`agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json` — array de **16 unidades de conhecimento** com chave estável (kebab-case), cada uma preservando: especialista responsável (`LinxErpSpecialist`/`LinxDatabaseSpecialist`), categoria, assunto, entidade/tabela/procedure Linx, campos, conteúdo, proveniência (`Descoberto`/`Inferido`), confiança, fonte rastreável, restrições/observações e tags.

### 2.2 — Artefato canônico gerado (saída, nunca editado à mão)

`agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md` — Markdown com uma âncora `<!-- linx-knowledge-unit: <key> --> `por unidade, gerado por:

`tools/agents/generate-linx-fornecedor-knowledge.js` — script determinístico: lê o JSON, valida chaves únicas (falha explícita em duplicata — nunca ingestão silenciosa duplicada) e escreve o Markdown. **Idempotência comprovada**: rodar o gerador duas vezes seguidas produz hash SHA-256 idêntico (verificado no teste automatizado, seção 5).

### 2.3 — Unidades ingeridas (resumo)

1. `linx-tabela-mestre-cadastro-cli-for` — schema de `CADASTRO_CLI_FOR`.
2. `linx-enderecos-triplicados-cadastro-cli-for` — endereço triplicado (principal/cobrança/entrega).
3. `linx-triggers-cadastro-cli-for` — as 11 triggers ativas e seus efeitos colaterais.
4. `linx-fks-cadastro-cli-for` — acoplamento via >90 FKs de entrada.
5. `linx-procedure-lx-sequencial` — mecanismo `LX_SEQUENCIAL`/`SEQUENCIAIS`.
6. `linx-sequenciais-concorrentes-fornecedor` — anomalia dos dois sequenciais concorrentes.
7. `linx-nome-clifor-nao-vem-de-sequencial` — geração de `NOME_CLIFOR` por sanitização de string.
8. `linx-procedure-p-rsv-integracao-cadastro-fornecedor` — a procedure de integração SAP/marketplace.
9. `linx-padrao-nivel-1-cinco-implementacoes-cadastro` — padrão recorrente entre as 5 implementações lidas.
10. `linx-ferramentas-discovery-schema-readonly` — `LX_CADE`/`LX_CADE_COLUNA`/`ANM_BUSCA_INSTRUCAO`.
11. `linx-metodologia-investigacao-escrita-futura` — regra de metodologia para investigação futura.
12. `arquitetura-fronteira-adapter-linx` — fronteira do futuro Adapter Linx.
13. `arquitetura-documento-fiscal-canonico` — `DocumentoFiscal` como Value Object único.
14. `arquitetura-taxonomia-erros-cnpj` — taxonomia de erros tipada.
15. `arquitetura-proveniencia-hibrida-consulta-cnpj` — modelo de proveniência híbrido.
16. `arquitetura-nao-persistir-qsa-cnae-secundario` — decisão de minimização de dados (QSA/CNAE).

---

## 3. Agents que consomem o conhecimento

`linx-erp-specialist-agent/agent.yaml` e `linx-database-specialist-agent/agent.yaml` foram atualizados (`implementation.context_paths`) para referenciar **a mesma fonte compartilhada**:

```yaml
context_paths:
  - ...
  - agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md
```

Nenhum conteúdo foi duplicado fisicamente entre os dois Agents — ambos apontam para o mesmo arquivo gerado (verificado no teste: exatamente uma ocorrência do caminho em cada manifesto, e exatamente um arquivo `.generated.md` no diretório de conhecimento).

---

## 4. Proveniência

Cada unidade de conhecimento carrega um campo `fonte` que aponta para uma seção específica de um dos dois documentos de auditoria originais:

- `docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md`
- `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`

Nenhuma unidade depende do snapshot temporário como fonte — o teste automatizado confirma isso explicitamente (nenhuma `fonte` referencia `agents/docs/ai-factory/temp/`, e o artefato gerado nunca cita esse caminho). Isso responde "de onde sabemos isso?" sempre pelos dois documentos de auditoria canônicos, nunca pelo snapshot.

---

## 5. Testes executados

`tools/agents/generate-linx-fornecedor-knowledge.test.js` (novo, padrão dos demais `tools/agents/*.test.js`):

- Fonte estruturada tem unidades reais (≥10).
- Chaves de conhecimento únicas (sem duplicação).
- **Idempotência real**: gerador executado duas vezes via `execFileSync`, hash SHA-256 comparado — idêntico.
- `render(source)` (função pura) é byte-idêntico ao artefato escrito em disco.
- Recuperação dos termos do snapshot original: `LX_SEQUENCIAL`, `CADASTRO_CLI_FOR`, `LX_CADE`, `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`, `NOME_CLIFOR`.
- Recuperação pelo Linx ERP Specialist (contexto funcional/arquitetura: fronteira do Adapter Linx, `DocumentoFiscal`) e referência em `agent.yaml`.
- Recuperação pelo Linx Database Specialist (schema/trigger: "11 triggers", `LXI_CADASTRO_CLI_FOR`) e referência em `agent.yaml`.
- Ausência de duplicação física entre os dois Agents (uma referência cada; um único arquivo `.generated.md`).
- Proveniência: toda unidade rastreável a um dos dois documentos de auditoria; nenhuma ao snapshot temporário.
- Nenhum fallback para o snapshot temporário no artefato gerado.
- `knowledge.md` referencia o artefato canônico (mecanismo real, não TODO).

### Resultado da execução completa

```
$ node tools/agents/validate-agent-manifests.js
PASS: 8 Agent Contract v1.1 manifests validated
PASS: IDs unique: agent-factory, echo-agent, knowledge-agent, linx-database-specialist-agent, linx-erp-specialist-agent, security-lgpd-agent, showcase-agent, wise-agent
PASS: capability ownership, delegation, gap and credential policies valid
PASS: Agent references and required paths exist
PASS: no bypass, privilege escalation or secret values detected

$ node tools/agents/*.test.js (todos, incluindo o novo)
PASS: Agent Factory v2 lifecycle, audit and safety tests
OK — generate-linx-fornecedor-knowledge.test.js: 16 unidades, idempotência e proveniência verificadas.
PASS: JS orchestrator -> governed-plan CLI -> .NET GovernedPlanBridge end-to-end offline bridge test
PASS: Governed Orchestrator v1 context, routing, cross-cutting and safety tests
PASS: Runtime Registry v1 discovery, routing, gaps, conflicts and safety tests
PASS: Showcase Agent offline safety invariants
PASS: 7 negative Agent Contract v1.1 validation scenarios rejected
PASS: WISE Agent offline safety invariants
```

Agents v1: **PASS, 0 ERROR, 0 ACTION_REQUIRED.**

---

## 6. Gaps restantes (conhecimento do snapshot ainda NÃO coberto)

O snapshot temporário (`agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md`, 459 linhas) tem seções com conhecimento único **ainda não ingerido** no mecanismo canônico desta rodada:

- **§2.2** — especializações `FORNECEDORES`/`CLIENTES_ATACADO`/`FILIAIS` a partir de `CADASTRO_CLI_FOR`.
- **§6.2/6.3** — mapa conceitual reutilizável de efeitos colaterais e princípio central consolidado (parcialmente coberto pela unidade `linx-metodologia-investigacao-escrita-futura`, mas não literalmente).
- **§8/§9** — contrato completo do Adapter Linx e lista consolidada de "desconhecidos preservados" (parcialmente coberto por `arquitetura-fronteira-adapter-linx`, mas não a lista completa).
- **§10** — playbook de discovery Linx reutilizável passo a passo.
- **§12** — GAP da fundação O1.13.5 (identificador real e sequência de ação futura).
- **§13-A** — descoberta via código-fonte real da tela Visual Linx (`TRANSACOES → SCX/SCT/PRG`, achados do formulário `001016G1`).
- **§13-B** — mecanismo genérico de persistência do framework Linx (`lx_class.vcx::l_salva`).
- **§13-C/§13-D** — decisões de produto já **aprovadas pelo Product Owner** sobre domínio Fornecedor/BU/ERP e convergência CREATE→UPDATE com reconsulta obrigatória.

**Motivo de não ingerir essas seções nesta rodada**: as seções §13-A/13-B/13-C/13-D em particular descrevem descobertas (código-fonte VFP da tela, decisões do PO) que **não têm evidência espelhada nos dois documentos de auditoria canônicos** (`docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md`, `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`) lidos nesta tarefa — investigar/confirmar essa correspondência para manter a regra de proveniência dupla (nunca só o snapshot) exigiria nova leitura/validação que não coube no escopo desta rodada.

**Status do snapshot temporário**: **STILL_REQUIRED** — não deve ser classificado como `SUPERSEDED_BY_CANONICAL_KNOWLEDGE` nem movido para `.empty/legacy/agents/ai-factory/` enquanto essas seções não forem ingeridas (ou explicitamente descartadas por decisão humana) no mecanismo canônico.

---

## 7. Arquivos criados/modificados

Criados:
- `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json`
- `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md`
- `tools/agents/generate-linx-fornecedor-knowledge.js`
- `tools/agents/generate-linx-fornecedor-knowledge.test.js`
- `docs/repository/LinxKnowledgeFornecedor-Ingestion.md` (este relatório)

Modificados:
- `agents/linx-erp-specialist-agent/agent.yaml` (novo `context_paths`)
- `agents/linx-database-specialist-agent/agent.yaml` (novo `context_paths`)
- `.ai/context/knowledge.md` (novo item na lista de conhecimento operacional persistido)

Não modificado: `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` (permanece `STILL_REQUIRED`, ver seção 6).
