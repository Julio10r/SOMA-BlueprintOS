# PROJECT_STATE.md

> Estado canônico operacional do SOMA BlueprintOS / +COMPRAS. Atualizar ao concluir cada sprint.

> Política de autonomia dos agentes: [AI_AUTONOMY_POLICY.md](./AI_AUTONOMY_POLICY.md).

> Referência técnica consolidada: [ENGINEERING_BLUEPRINT.md](./ENGINEERING_BLUEPRINT.md).

> Fonte externa de descoberta: [COMPRAS_INDIRETAS_SOURCES.md](./sources/COMPRAS_INDIRETAS_SOURCES.md) (não é evidência de implementação).

## Atualização

- **Data:** 01/08/2026
- **Branch:** `feature/a13-procurement-vertical-slice`
- **Commit de referência:** complementar à implementação B2.1; migration aplicada e validação real executada em 31/07/2026.
- **Validação desta atualização:** `dotnet build backend/BlueprintOS.sln --no-restore`, 0 erros e 0 avisos; 248 testes unitários e 3 testes de integração aprovados. A migration complementar foi aplicada somente no +Compras dev; a validação operacional bidirecional completa ainda está pendente.

## Sistema de Work Orders

- **Estado:** Implementado em 30/07/2026.
- **Evidência:** [templates/README.md](./templates/README.md) e os sete templates padronizados para desenvolvimento, épicos, auditorias, refatorações, hotfixes, spikes e releases.
- **Uso:** os templates complementam, sem substituir, as Work Orders estratégicas em `workorders/` e a governança de [WORKFLOW.md](./WORKFLOW.md). Eles exigem leitura prévia de visão, workflow, estado do projeto e sprint atual.

## Evolução arquitetural do +Compras

- **Decisão aceita:** [ADR-0013](./DECISIONS.md) estabelece a evolução em dois momentos: plataforma operacional primeiro e inteligência progressiva sobre dados reais depois.
- **Princípio obrigatório:** toda operação crítica possui alternativa manual; IA acelera e orienta, mas não é pré-requisito para cadastrar ou selecionar fornecedor/item, criar pedido, enviá-lo ao ERP ou acompanhar a integração.
- **Portal:** é a interface do próprio +Compras e evolui junto aos módulos, sem constituir produto ou módulo separado.
- **B2/B2.1:** B2 permanece como estrutura inicial de descoberta e score (100/80/60/40); B2.1 validou operacionalmente leitura, escrita, atualização e idempotência de fornecedores.
- **B2.2:** planejada em Draft após a B2.1 para enriquecimento cadastral por CNPJ. Consulta externa será apenas sugestão revisável, com auditoria e sem atualização automática do +Compras ou ERP.

## Estratégia de LLM

- **Decisão aceita:** a [ADR-0014](./DECISIONS.md) determina `IAIProvider` e `IAIRuntime` como fronteira entre a aplicação e qualquer fornecedor de LLM.
- **Desenvolvimento:** Ollama local é o padrão arquitetural, com preferência por modelos de 3B a 4B parâmetros; seu adaptador ainda não foi implementado ou configurado por esta decisão documental.
- **Produção:** a plataforma corporativa de IA, definida pela Infraestrutura/Arquitetura Corporativa, será consumida por adaptador e configuração. O fornecedor não é decidido pelo +Compras.
- **Estado atual:** `OpenAIProvider` permanece o adaptador implementado em Infrastructure; agentes e regras de negócio continuam dependentes apenas das abstrações.

## Resumo executivo

O BlueprintOS possui uma fundação backend validada para runtime de IA, agentes simples, conhecimento em Markdown, memória e estratégia de negociação em processo, workflow sequencial e publicação/documentação. O +COMPRAS possui CRUD de fornecedores e sincronização ERP operacional validados; ainda não há autenticação corporativa.

## Ciclo atual

- **Fase real atual:** Fase 0 — Fundação, em andamento. O EPIC de documentação foi concluído, mas a fundação prevista no roadmap ainda não está completa.
- **Última sprint comprovadamente concluída:** B2 — Descoberta Inteligente de Fornecedores (30/07/2026).
- **Sprint atual:** B2.1/B2.1.1 com validação técnica completa em 01/08/2026, incluindo mapeamento canônico ERP → +Compras, aguardando revisão formal antes do encerramento. B2.2 permanece em Draft e B3 não foi iniciada.
- **Próxima sprint planejada:** B2.2 — Enriquecimento Cadastral de Fornecedores por CNPJ, em Draft e condicionada à conclusão da B2.1. B3 não foi iniciada.
- **Progresso real:** documentação/publicação, capacidades internas de IA e um fluxo consultivo de negociação por API estão implementados; os demais fluxos de produto +COMPRAS e os requisitos de operação corporativa permanecem pendentes.

## Capacidades implementadas

| Área | Evidência no código | Estado |
|---|---|---|
| AI Runtime | `IAIRuntime`, `OpenAIProvider` e contratos de chat | Implementado |
| Agents | `IAgent`, `BaseAgent`, `EchoAgent`, `KnowledgeAgent`, `AgentFactory` | Implementado, básico |
| Knowledge | `MarkdownKnowledgeProvider` e `KnowledgeService` | Implementado, baseado em Markdown |
| Negociação | `NegotiationMemory`, regras e `NegotiationStrategy` | Implementado, em memória |
| API de negociação | `POST /api/v1/negociacoes/recomendacoes` via `NegotiationRecommendationUseCase` | Implementado, consultivo e sem estado |
| Fornecedores | `Fornecedor`, EF Core/SQL Server sobre `MaisComprasConnection`, migration e `POST/GET/PUT/DELETE /fornecedores` | Implementado |
| Sincronização de fornecedores | Contrato canônico, adaptadores por BU, importação/exportação/inativação, `LX_SEQUENCIAL`, timestamp Linx, concorrência, idempotência e auditoria append-only | Validação técnica completa; aguardando revisão formal |
| Descoberta de fornecedores | `FornecedorDescoberto`, score centralizado, leitura `SOMA_DESENV`, persistência +Compras e `/api/fornecedores/descobertas` | Implementado; validação SQL ERP pendente de ambiente com acesso |
| Workflow | `Workflow` e `WorkflowRunner` sequenciais | Implementado, básico |
| Documentation | contratos, geradores, publicação Markdown, Git reader e health report | Implementado |
| Publication | renderização Markdown/HTML/PDF, QR Code e publicadores por público | Implementado |

## Capacidades parciais

- **Memória:** existe apenas a memória de negociação em processo; não há memória corporativa genérica, persistência nem recuperação de longo prazo.
- **API:** host ASP.NET Core, OpenAPI em desenvolvimento, `GET /health` e o endpoint consultivo de negociação existem; não há autenticação corporativa, autorização ou contratos para os demais domínios de Procurement.
- **Infraestrutura:** EF Core/SQL Server possui `BlueprintOSDbContext`, migration inicial, conexões segregadas de +Compras/ERP e validador somente leitura; não há CI/CD, GCP, Kubernetes, Terraform, Nginx ou observabilidade implementados.
- **Arquitetura:** o estilo alvo é Modular Monolith com módulos por camada, mas o código real permanece em projetos transversais `Core`/`Infrastructure`.

## Capacidades não iniciadas

- Identity, autorização, multi-tenant e Microsoft Entra ID.
- Planner, Procurement, Notifications, Dashboard e Analytics.
- Frontend React/TypeScript e portal +COMPRAS.
- n8n e APIs corporativas; integração ERP de fornecedores B2.1 está implementada.

## Agentes e integrações concretos

- **Agentes:** `EchoAgent` e `KnowledgeAgent`. Não existe classe concreta `SeniorBuyerAgent`, `NegotiationAgent`, `ComplianceAgent` ou `RiskAgent`.
- **Integrações:** OpenAI Chat Completions via `OpenAIProvider`; descoberta e sincronização de fornecedores via adaptadores em `SOMA_DESENV`; CLI Git somente para leitura de histórico de documentação.
- **Identidade temporária:** `DevelopmentRequestIdentity` atende somente Development e alimenta `ICurrentIdentity`; fornecedores persistem esse vínculo sem dependência da implementação concreta.

## Qualidade

| Suíte | Executados | Aprovados | Ignorados | Falhos |
|---|---:|---:|---:|---:|
| Unitários | 250 | 250 | 0 | 0 |
| Integração | 3 | 3 | 0 | 0 |
| Total | 253 | 253 | 0 | 0 |

Build da solution: sucesso, 0 erros e 0 avisos.

## Riscos e pendências

- Apenas a recomendação de negociação está exposta por API; os demais domínios de negócio ainda não possuem API ou interface utilizável.
- A atualização do nome no ERP permanece limitada pela FK `FORNECEDORES.FORNECEDOR → CADASTRO_CLI_FOR.NOME_CLIFOR`; a B2.1 validou atualização de CNPJ sem operação destrutiva.
- Dados de negociação e documentação ainda não são duráveis.
- A configuração da chave OpenAI depende de ambiente e não há tratamento operacional completo para credenciais, rate limits ou telemetria.
- A arquitetura física diverge do layout alvo; uma migração deve ser planejada somente quando trouxer benefício concreto.
- Métricas e estado de documentação exigem atualização a cada sprint até existir automação de CI.

## Divergências ainda abertas

- O roadmap estratégico de apresentação +COMPRAS continua sem atualização visual; as correções necessárias estão listadas em `docs/presentations/ROADMAP_UPDATE.md`.
- A estrutura alvo descrita em `ARCHITECTURE.md` não é a estrutura física atual.
- Nenhuma Work Order futura está aprovada; a próxima sprint depende de decisão explícita do Product Owner.

## Auditoria de repositório

- **Etapa 1 — Higiene e artefatos gerados (30/07/2026):** remoção exclusiva de resíduos locais comprovados (`.DS_Store`, `bin/`, `obj/` e `dist/`) e reforço do `.gitignore`. Não houve alteração de progresso funcional. Restore serial, build e 231 testes foram concluídos; o único aviso `NU1900` decorre da indisponibilidade de consulta de vulnerabilidades ao nuget.org, sem impedir a validação. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-01.md).
- **Etapa 2 — Obsoletos, duplicados e órfãos (30/07/2026):** auditoria investigativa de 629 arquivos versionados. Foram registrados 13 grupos candidatos (0 para remoção automática); não houve alteração funcional, remoção, movimento ou renomeação. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-02.md).
- **Etapa 3 — Consolidação documental e estado A12 (30/07/2026):** fontes canônicas de visão e workflow definidas, documentos históricos controlados e saídas derivadas republicadas para refletir A12. Não houve alteração de progresso funcional. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-03.md).
