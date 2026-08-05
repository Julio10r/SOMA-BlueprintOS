# Documentação Técnica — SOMA BlueprintOS

`docs/` é a única fonte canônica da documentação técnica permanente do projeto: descreve **como o sistema funciona**. Escrita por humanos (ou por IA em nome de humanos), nunca gerada automaticamente.

`.ai/` descreve **como o projeto está** — estado, sprint atual, roadmap, backlog, decisões (ADRs) e memória operacional da IA. Esses dois papéis nunca se misturam: se uma informação já existe em `.ai/`, `docs/` referencia por link, nunca copia.

## Regra de atualização

**Toda alteração que modifica permanentemente o comportamento do sistema deve atualizar `docs/`** — nova arquitetura, nova API, nova integração, novo agente, alteração de schema de banco. Bugfix simples, ajuste visual, texto ou refactoring interno sem impacto arquitetural não exigem atualização.

Regra prática: se uma mudança tornar uma frase existente em `docs/` **falsa**, a documentação precisa ser atualizada. Caso contrário, não.

## Mapa das áreas

| Área | Conteúdo |
|---|---|
| [architecture/](./architecture/Architecture.md) | Arquitetura do sistema, camadas, módulos internos, diagramas; [decisões-chave](./architecture/Decisions.md) referenciando `.ai/DECISIONS.md` |
| [backend/procurement/](./backend/procurement/Procurement.md) | Domínio de Fornecedores: cadastro, descoberta, enriquecimento de CNPJ |
| [backend/integration/](./backend/integration/Integration.md) | Integrações externas (ERP) |
| [backend/orchestration/](./backend/orchestration/Orchestration.md) | Como o backend coordena fluxos entre API, casos de uso e agentes/estratégias |
| [backend/shared/](./backend/shared/Shared.md) | Utilitários e convenções compartilhadas entre módulos |
| [frontend/](./frontend/Frontend.md) | Arquitetura do Portal Operacional +Compras, mapa de navegação |
| [database/](./database/Database.md) | Persistência, EF Core, SQL Server |
| [agents/](./agents/Agents.md) | Runtime de agentes de IA, AI Factory |
| [operations/](./operations/Operations.md) | Ambiente de execução, deploy, Git Flow, [runbooks](./operations/Runbooks.md) |
| [testing/](./testing/Testing.md) | Estratégia e critérios de teste |
| [releases/](./releases/Release-Notes.md) | Histórico curado de entregas por sprint/marco |
| [assets/](./assets/) | Diagramas Mermaid e árvore de solução usados pela documentação técnica |

Autenticação/identidade corporativa (Microsoft Entra ID) ainda não tem documento próprio — é capacidade planejada, sem implementação real a descrever (ver `.ai/work-orders/backlog/fase-h/`).

## Fora do escopo de `docs/`

- **Estado operacional, sprint atual, roadmap, backlog, ADRs** — exclusivamente em `.ai/`.
- **Material institucional, de marca e apresentações** — em `resources/` (design system, decks).
- **Saída publicável** (Markdown/HTML/PDF gerados pelo Publication Engine) — em `dist/`, nunca versionada.
- **Auditorias históricas** — preservadas em `docs/audits/`, não são documentação viva.
