# Árvore da Solução

Estrutura real de diretórios e projetos do repositório, restrita ao que é
versionável: arquivos rastreados pelo Git, mais os diretórios vazios
explicitamente reservados para fases futuras do roadmap. Arquivos ignorados,
não rastreados ou pessoais (ex.: `.myNotes`, `.DS_Store`, `bin/`, `obj/`,
`node_modules/`, logs, artefatos temporários) não aparecem.

```
SOMA-BlueprintOS
├── .ai/
│   ├── content/
│   │   ├── client/
│   │   ├── engineering/
│   │   └── executive/
│   ├── context/
│   │   ├── agents.md
│   │   ├── architecture.md
│   │   ├── coding-standards.md
│   │   ├── definition-of-done.md
│   │   ├── git-workflow.md
│   │   ├── knowledge.md
│   │   ├── memory.md
│   │   ├── observability.md
│   │   ├── planner.md
│   │   ├── README.md
│   │   ├── runtime.md
│   │   ├── security.md
│   │   ├── tech-stack.md
│   │   └── testing.md
│   ├── memory/
│   │   ├── architecture.md
│   │   ├── completed_sprints.md
│   │   ├── decisions.md
│   │   ├── known_issues.md
│   │   └── patterns.md
│   ├── prompts/
│   │   ├── claude.md
│   │   ├── codex.md
│   │   ├── new-agent.md
│   │   ├── new-api.md
│   │   ├── new-database.md
│   │   ├── refactor.md
│   │   ├── review.md
│   │   └── tests.md
│   ├── sources/
│   │   └── COMPRAS_INDIRETAS_SOURCES.md
│   ├── templates/
│   │   ├── AUDIT_TEMPLATE.md
│   │   ├── EPIC_TEMPLATE.md
│   │   ├── HOTFIX_TEMPLATE.md
│   │   ├── README.md
│   │   ├── REFACTOR_TEMPLATE.md
│   │   ├── RELEASE_TEMPLATE.md
│   │   ├── SPIKE_TEMPLATE.md
│   │   └── WORK_ORDER_TEMPLATE.md
│   ├── work-orders/                 # único local canônico de Work Orders
│   │   ├── active/                  # em execução/aguardando validação
│   │   │   └── PortalMaisComprasFrontend.md
│   │   ├── backlog/                 # catálogo estratégico (fases A–H), planejado/parcial
│   │   │   ├── fase-a/
│   │   │   ├── fase-b/
│   │   │   ├── fase-c/
│   │   │   ├── fase-d/
│   │   │   ├── fase-e/
│   │   │   ├── fase-f/
│   │   │   ├── fase-g/
│   │   │   ├── fase-h/
│   │   │   ├── DEPENDENCY_MAP.md
│   │   │   ├── README.md
│   │   │   └── WORK_ORDER_TEMPLATE.md
│   │   ├── completed/                # concluídos, com evidência
│   │   │   ├── A1-arquitetura-base.md
│   │   │   ├── A2-ai-runtime.md
│   │   │   ├── A3-agent-framework.md
│   │   │   ├── A4-workflow-e-observabilidade-fundamental.md
│   │   │   ├── A7-sistema-de-documentacao.md
│   │   │   ├── A10-GovernanceAndWorkOrderFoundation.md
│   │   │   ├── A13-PrimeiroVerticalSliceMaisCompras.md
│   │   │   ├── B1-cadastro-e-perfil-de-fornecedores.md
│   │   │   ├── B2-catalogo-de-materiais-e-servicos.md
│   │   │   ├── B2.1-ValidacaoOperacionalESincronizacaoDeFornecedoresComERP.md
│   │   │   ├── B2.1.1-CompletarMapeamentoCanonicoErpMaisCompras.md
│   │   │   ├── B2.1.2-AlinhamentoEstruturalErpLinxMaisCompras.md
│   │   │   └── B2.2-EnriquecimentoCadastralDeFornecedoresPorCnpj.md
│   │   └── README.md
│   ├── AI_AUTONOMY_POLICY.md
│   ├── AI_BEHAVIOR.md
│   ├── AI_TEAM.md
│   ├── ARCHITECTURE.md
│   ├── BACKLOG.md
│   ├── CLAUDE.md
│   ├── CURRENT_SPRINT.md
│   ├── DECISIONS.md
│   ├── DEVELOPMENT_WORKFLOW.md
│   ├── DOCUMENTATION_STRATEGY.md
│   ├── DOCUMENTATION_UPDATE_COMMAND.md
│   ├── ENGINEERING_BLUEPRINT.md
│   ├── PRESENTATION_WORKFLOW.md
│   ├── PROJECT.md
│   ├── PROJECT_PHILOSOPHY.md
│   ├── PROJECT_SCOPE.md
│   ├── PROJECT_STATE.md
│   ├── PROJECT_VISION.md
│   ├── ROADMAP.md
│   ├── STANDARDS.md
│   ├── VISION.md
│   └── WORKFLOW.md
├── backend/
│   ├── src/
│   │   ├── BlueprintOS.Api/
│   │   ├── BlueprintOS.Application/
│   │   ├── BlueprintOS.Core/
│   │   ├── BlueprintOS.Domain/
│   │   ├── BlueprintOS.Infrastructure/
│   │   └── BlueprintOS.Shared/
│   ├── tests/
│   │   ├── BlueprintOS.IntegrationTests/
│   │   └── BlueprintOS.UnitTests/
│   └── BlueprintOS.sln
├── docs/                          # documentação técnica — como o sistema funciona
│   ├── architecture/
│   │   ├── Architecture.md
│   │   └── Decisions.md           # referencia .ai/DECISIONS.md, não duplica ADRs
│   ├── backend/
│   │   ├── integration/
│   │   │   ├── B21.2-EstruturaFornecedorERP.md
│   │   │   ├── FornecedorErpSynchronization.md
│   │   │   ├── FornecedorSynchronization.md
│   │   │   └── Integration.md
│   │   ├── orchestration/
│   │   │   └── Orchestration.md
│   │   ├── procurement/
│   │   │   ├── FornecedorCnpjEnrichment.md
│   │   │   └── Procurement.md
│   │   └── shared/
│   │       └── Shared.md
│   ├── frontend/
│   │   └── Frontend.md
│   ├── database/
│   │   └── Database.md
│   ├── agents/
│   │   ├── ai-factory/            # fundamentos internos da AI Factory (arquitetura-alvo)
│   │   └── Agents.md
│   ├── operations/
│   │   ├── Operations.md
│   │   └── Runbooks.md
│   ├── testing/
│   │   └── Testing.md
│   ├── releases/
│   │   └── Release-Notes.md
│   ├── assets/
│   │   ├── agents.mmd
│   │   ├── architecture.mmd
│   │   ├── dependencies.mmd
│   │   └── solution-tree.md
│   ├── audits/                    # histórico de auditorias pontuais, não documentação viva
│   │   ├── architecture-review-2026-07-30.md
│   │   ├── B-Series-Reconciliation.md
│   │   ├── repository-cleanup-step-01.md
│   │   ├── repository-cleanup-step-02.md
│   │   └── repository-cleanup-step-03.md
│   ├── demo/
│   │   ├── portal-maiscompras-build.html
│   │   └── PortalMaisComprasDemo.md
│   ├── executive/                 # Executive Blueprint — fonte autoral; html/pdf publicados em dist/executive/
│   │   └── BlueprintOS_Executive_Blueprint.md
│   ├── Executive Report.md
│   ├── Product Blueprint.md
│   └── README.md                  # índice técnico
├── resources/                     # institucional/marca — fora do fluxo técnico
│   ├── design-system/
│   │   ├── assets/
│   │   ├── fonts/
│   │   ├── icons/
│   │   ├── presentations/
│   │   ├── preview/
│   │   ├── templates/
│   │   ├── ui_kits/
│   │   ├── colors_and_type.css
│   │   ├── fonts.css
│   │   ├── INDEX.md
│   │   ├── README.md
│   │   └── SKILL.md
│   └── presentations/
│       ├── +COMPRAS Strategic Roadmap QA.md
│       ├── +COMPRAS Strategic Roadmap.md
│       ├── +COMPRAS Strategic Roadmap.pdf
│       ├── +COMPRAS Strategic Roadmap.pptx
│       ├── Roadmap Gerencial - BlueprintOS.pptx
│       ├── Roadmap Gerencial - BlueprintOS.pptx.inspect.ndjson
│       ├── Roadmap Gerencial - Design Mapping.md
│       ├── Roadmap Gerencial - Executive Review.md
│       ├── Roadmap Gerencial - QA.md
│       ├── Roadmap Gerencial - Storyboard.md
│       └── ROADMAP_UPDATE.md
├── frontend/
│   └── web/
│       ├── dist/
│       ├── src/
│       ├── .env.example
│       ├── index.html
│       ├── package-lock.json
│       ├── package.json
│       ├── tsconfig.json
│       ├── tsconfig.tsbuildinfo
│       └── vite.config.ts
├── infrastructure/
│   ├── docker/
│   │   └── .env.example
│   ├── kubernetes/
│   ├── monitoring/
│   ├── nginx/
│   └── terraform/
├── mcp/
│   └── design-system/
│       └── README.md
├── scripts/
│   ├── health-check.sh
│   ├── start-dev.sh
│   └── stop-dev.sh
├── .editorconfig
├── .env.example
├── .gitattributes
├── .gitignore
├── CHANGELOG.md
├── LICENSE
└── README.md
```
