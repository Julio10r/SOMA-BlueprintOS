# PROJECT_STATE.md

> Estado canônico operacional do SOMA BlueprintOS / +COMPRAS. Atualizar ao concluir cada sprint.

> Política de autonomia dos agentes: [AI_AUTONOMY_POLICY.md](./AI_AUTONOMY_POLICY.md).

> Referência técnica consolidada: [ENGINEERING_BLUEPRINT.md](./ENGINEERING_BLUEPRINT.md).

## Atualização

- **Data:** 30/07/2026
- **Branch:** `main`
- **Commit de referência:** `741d809` — `docs: consolidate canonical project state`
- **Validação desta atualização:** `dotnet build backend/BlueprintOS.sln --no-restore`, 0 avisos e 0 erros; 230 testes unitários e 1 teste de integração aprovados.

## Resumo executivo

O BlueprintOS possui uma fundação backend validada para runtime de IA, agentes simples, conhecimento em Markdown, memória e estratégia de negociação em processo, workflow sequencial e publicação/documentação. O +COMPRAS ainda não é uma funcionalidade utilizável de ponta a ponta: não há portal, API de negócio, persistência durável, autenticação, Procurement nem integração ERP.

## Ciclo atual

- **Fase real atual:** Fase 0 — Fundação, em andamento. O EPIC de documentação foi concluído, mas a fundação prevista no roadmap ainda não está completa.
- **Última sprint comprovadamente concluída:** A9 — Publication Engine.
- **Sprint atual:** A11 — Engineering Blueprint (Completed em 30/07/2026).
- **Próxima sprint proposta:** não definida; A11 — +COMPRAS Negotiation API Slice permanece apenas como sugestão, sujeita a priorização e aprovação.
- **Progresso real:** documentação/publicação e capacidades internas de IA estão implementadas; os fluxos de produto +COMPRAS e os requisitos de operação corporativa permanecem pendentes.

## Capacidades implementadas

| Área | Evidência no código | Estado |
|---|---|---|
| AI Runtime | `IAIRuntime`, `OpenAIProvider` e contratos de chat | Implementado |
| Agents | `IAgent`, `BaseAgent`, `EchoAgent`, `KnowledgeAgent`, `AgentFactory` | Implementado, básico |
| Knowledge | `MarkdownKnowledgeProvider` e `KnowledgeService` | Implementado, baseado em Markdown |
| Negociação | `NegotiationMemory`, regras e `NegotiationStrategy` | Implementado, em memória |
| Workflow | `Workflow` e `WorkflowRunner` sequenciais | Implementado, básico |
| Documentation | contratos, geradores, publicação Markdown, Git reader e health report | Implementado |
| Publication | renderização Markdown/HTML/PDF, QR Code e publicadores por público | Implementado |

## Capacidades parciais

- **Memória:** existe apenas a memória de negociação em processo; não há memória corporativa genérica, persistência nem recuperação de longo prazo.
- **API:** host ASP.NET Core e OpenAPI em desenvolvimento existem; a única rota HTTP é `GET /health`.
- **Infraestrutura:** Docker Compose sobe SQL Server e API; não há CI/CD, GCP, Kubernetes, Terraform, Nginx ou observabilidade implementados.
- **Arquitetura:** o estilo alvo é Modular Monolith com módulos por camada, mas o código real permanece em projetos transversais `Core`/`Infrastructure`.

## Capacidades não iniciadas

- Identity, autorização, multi-tenant e Microsoft Entra ID.
- Planner, Procurement, Notifications, Dashboard e Analytics.
- Frontend React/TypeScript e portal +COMPRAS.
- Persistência EF Core/SQL Server, `DbContext`, migrations e schema de negócio.
- Integrações ERP, n8n e APIs corporativas.

## Agentes e integrações concretos

- **Agentes:** `EchoAgent` e `KnowledgeAgent`. Não existe classe concreta `SeniorBuyerAgent`, `NegotiationAgent`, `ComplianceAgent` ou `RiskAgent`.
- **Integrações:** OpenAI Chat Completions via `OpenAIProvider`; CLI Git somente para leitura de histórico de documentação; Docker Compose com SQL Server configurado, mas não consumido pela aplicação.

## Qualidade

| Suíte | Executados | Aprovados | Ignorados | Falhos |
|---|---:|---:|---:|---:|
| Unitários | 230 | 230 | 0 | 0 |
| Integração | 1 | 1 | 0 | 0 |
| Total | 231 | 231 | 0 | 0 |

Build da solution: sucesso, 0 avisos e 0 erros.

## Riscos e pendências

- As capacidades internas não estão expostas em uma API de negócio nem em uma interface utilizável.
- Dados de negociação e documentação ainda não são duráveis.
- A configuração da chave OpenAI depende de ambiente e não há tratamento operacional completo para credenciais, rate limits ou telemetria.
- A arquitetura física diverge do layout alvo; uma migração deve ser planejada somente quando trouxer benefício concreto.
- Métricas e estado de documentação exigem atualização a cada sprint até existir automação de CI.

## Divergências ainda abertas

- O roadmap estratégico de apresentação +COMPRAS continua sem atualização visual; as correções necessárias estão listadas em `docs/presentations/ROADMAP_UPDATE.md`.
- A estrutura alvo descrita em `ARCHITECTURE.md` não é a estrutura física atual.
- O roadmap de alto nível ainda não define uma sprint aprovada após A10; A11 é apenas proposta.
