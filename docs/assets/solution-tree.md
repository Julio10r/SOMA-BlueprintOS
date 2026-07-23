# Árvore da Solução

Estrutura real de diretórios e projetos do repositório (ignorando `bin`, `obj`,
`.git` e `node_modules`):

```
SOMA-BlueprintOS
├── .ai/
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
│   ├── decisions/
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
│   ├── reports/
│   ├── tasks/
│   │   └── README.md
│   ├── workorders/
│   │   └── A7 - Documentation System.md
│   ├── AI_BEHAVIOR.md
│   ├── AI_TEAM.md
│   ├── ARCHITECTURE.md
│   ├── CLAUDE.md
│   ├── CURRENT_SPRINT.md
│   ├── DECISIONS.md
│   ├── DEVELOPMENT_WORKFLOW.md
│   ├── DOCUMENTATION_STRATEGY.md
│   ├── PRESENTATION_WORKFLOW.md
│   ├── PROJECT.md
│   ├── PROJECT_PHILOSOPHY.md
│   ├── PROJECT_SCOPE.md
│   ├── PROJECT_VISION.md
│   ├── ROADMAP.md
│   ├── STANDARDS.md
│   └── WORKFLOW.md
├── .github/
│   └── workflows/
├── .vscode/
├── agents/
│   ├── memory/
│   ├── orchestrator/
│   ├── planner/
│   ├── prompts/
│   └── specialists/
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
│   ├── tools/
│   │   └── docgen-runner/
│   └── BlueprintOS.sln
├── database/
│   ├── docs/
│   ├── migrations/
│   ├── scripts/
│   └── seed/
├── dist/
│   ├── client/
│   │   ├── ClientGuide.html
│   │   ├── ClientGuide.md
│   │   └── ClientGuide.pdf
│   ├── engineering/
│   │   ├── EngineeringGuide.html
│   │   ├── EngineeringGuide.md
│   │   └── EngineeringGuide.pdf
│   ├── executive/
│   │   ├── ExecutiveReport.html
│   │   ├── ExecutiveReport.md
│   │   └── ExecutiveReport.pdf
│   └── .DS_Store
├── docs/
│   ├── adr/
│   ├── AI Factory/
│   │   ├── Agents/
│   │   ├── Architecture/
│   │   ├── Core/
│   │   ├── Examples/
│   │   ├── Memory/
│   │   ├── Prompts/
│   │   ├── 00 - AI Factory.md
│   │   ├── 01 - AI Orchestrator.md
│   │   ├── 02 - AI Team.md
│   │   ├── 03 - Task Protocol.md
│   │   ├── 04 - Memory System.md
│   │   └── 05 - Automation Roadmap.md
│   ├── api/
│   ├── architecture/
│   ├── client/
│   │   ├── API.md
│   │   ├── Changelog.md
│   │   ├── FAQ.md
│   │   ├── FunctionalGuide.md
│   │   ├── ProductOverview.md
│   │   └── UserGuide.md
│   ├── decisions/
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
│   ├── diagrams/
│   ├── engineering/
│   │   ├── Mermaid/
│   │   ├── Agents.md
│   │   ├── APIs.md
│   │   ├── Architecture.md
│   │   ├── Database.md
│   │   ├── Decisions.md
│   │   ├── Deploy.md
│   │   └── Runbooks.md
│   ├── executive/
│   │   ├── BlueprintOS_Executive_Blueprint.html
│   │   ├── BlueprintOS_Executive_Blueprint.md
│   │   ├── BlueprintOS_Executive_Blueprint.pdf
│   │   ├── Dashboard.md
│   │   ├── KPIs.md
│   │   ├── Releases.md
│   │   ├── Roadmap.md
│   │   └── SprintStatus.md
│   ├── presentations/
│   │   ├── Engineering Handbook Design Mapping.md
│   │   ├── Engineering Handbook QA.md
│   │   ├── Engineering Handbook Storyboard.md
│   │   ├── Engineering Handbook.md
│   │   ├── Engineering Handbook.pdf
│   │   ├── Engineering Handbook.pptx
│   │   ├── Executive Report Design Mapping.md
│   │   ├── Executive Report QA.md
│   │   ├── Executive Report Storyboard.md
│   │   ├── Executive Report.md
│   │   ├── Executive Report.pdf
│   │   ├── Executive Report.pptx
│   │   ├── Executive Review.md
│   │   ├── Product Blueprint Design Mapping.md
│   │   ├── Product Blueprint QA.md
│   │   ├── Product Blueprint Storyboard.md
│   │   ├── Product Blueprint.md
│   │   ├── Product Blueprint.pdf
│   │   ├── Product Blueprint.pptx
│   │   └── Template Master.pptx
│   ├── sprints/
│   ├── templates/
│   │   ├── ADR.md
│   │   ├── API.md
│   │   ├── Feature.md
│   │   ├── RFC.md
│   │   ├── Sprint.md
│   │   ├── Task.md
│   │   └── Workflow.md
│   ├── .DS_Store
│   ├── DocumentationHealth.md
│   ├── Engineering Handbook.md
│   ├── Executive Report.md
│   ├── INDEX.md
│   └── Product Blueprint.md
├── frontend/
│   ├── mobile/
│   ├── shared/
│   └── web/
├── infrastructure/
│   ├── docker/
│   │   ├── .dockerignore
│   │   ├── .env.docker
│   │   ├── .env.docker.example
│   │   ├── docker-compose.override.yml
│   │   └── docker-compose.yml
│   ├── kubernetes/
│   ├── monitoring/
│   ├── nginx/
│   └── terraform/
├── integrations/
├── scripts/
├── shared/
│   ├── constants/
│   ├── contracts/
│   ├── events/
│   └── libraries/
├── workers/
├── .DS_Store
├── .editorconfig
├── .env.example
├── .gitattributes
├── .gitignore
├── LICENSE
├── Makefile
└── README.md
```
