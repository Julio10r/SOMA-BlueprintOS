# PROJECT_STATE.md

> Estado canônico operacional do SOMA BlueprintOS / +COMPRAS. Atualizar ao concluir cada sprint.

> Política de autonomia dos agentes: [AI_AUTONOMY_POLICY.md](./AI_AUTONOMY_POLICY.md).

> Referência técnica consolidada: [ENGINEERING_BLUEPRINT.md](./ENGINEERING_BLUEPRINT.md).

> Fonte externa de descoberta: [COMPRAS_INDIRETAS_SOURCES.md](./sources/COMPRAS_INDIRETAS_SOURCES.md) (não é evidência de implementação).

## Atualização

- **Data:** 01/08/2026
- **Branch:** `feature/a13-procurement-vertical-slice`
- **Commit de referência:** `b08769f`, `3b6d54b` e `0240c35` para as entregas B2.1 e B2.1.1; `77861eb` para B2.1.2; B2.2.4 em andamento pendente de commit nesta atualização.
- **Validação desta atualização:** `dotnet test backend/BlueprintOS.sln --no-restore`, 269 testes unitários e 4 testes de integração aprovados; `dotnet build backend/BlueprintOS.sln --no-restore`, 0 erros/0 avisos; `npm test` em `frontend/web`, 4 testes aprovados; `npm run build` em `frontend/web` concluído.

## Sistema de Work Orders

- **Estado:** Implementado em 30/07/2026.
- **Evidência:** [templates/README.md](./templates/README.md) e os sete templates padronizados para desenvolvimento, épicos, auditorias, refatorações, hotfixes, spikes e releases.
- **Uso:** os templates complementam, sem substituir, as Work Orders estratégicas em `workorders/` e a governança de [WORKFLOW.md](./WORKFLOW.md). Eles exigem leitura prévia de visão, workflow, estado do projeto e sprint atual.

## Evolução arquitetural do +Compras

- **Decisão aceita:** [ADR-0013](./DECISIONS.md) estabelece a evolução em dois momentos: plataforma operacional primeiro e inteligência progressiva sobre dados reais depois.
- **Princípio obrigatório:** toda operação crítica possui alternativa manual; IA acelera e orienta, mas não é pré-requisito para cadastrar ou selecionar fornecedor/item, criar pedido, enviá-lo ao ERP ou acompanhar a integração.
- **Portal:** a ADR-0017 definiu o Portal Operacional +Compras como navegação e identidade visual completas, com evolução funcional incremental por domínio. B2.2.4 iniciou a primeira vertical slice funcional em React com a tela `CadastroFornecedor`.
- **B2/B2.1/B2.1.1:** B2 permanece como estrutura inicial de descoberta e score (100/80/60/40). B2.1 concluiu sincronização bidirecional, regra temporal, inativação, auditoria e concorrência; B2.1.1 concluiu o mapeamento canônico ERP → +Compras.
- **B2.1.2:** concluída conforme ADR-0016, com modelo fornecedor alinhado ao Linx: `Cnpj_Cpf`, `TipoPessoa`, separação de `RazaoSocial`/`NomeFantasia`, proteção do nome fantasia controlado pelo Linx, flags `Beneficiador`/`Licenciado`, domínios ERP estruturados, FKs opcionais e contrato frontend inicial.
- **B2.2:** em andamento como Consulta CNPJ e Enriquecimento de Fornecedor. A B2.2.1 foi concluída com contrato `ICnpjConsultaProvider`, retorno tipado e auditoria persistida. A B2.2.2 implementou `BrasilApiCnpjProvider` como adaptador gratuito BrasilAPI. A B2.2.3 concluiu comparação campo a campo, aprovação/rejeição, atualização seletiva, auditoria de decisões e proteção `NomeFantasia`/Linx. A B2.2.4 iniciou o portal funcional de cadastro, consulta, divergências e decisão humana.

## Estratégia de LLM

- **Decisão aceita:** a [ADR-0014](./DECISIONS.md) determina `IAIProvider` e `IAIRuntime` como fronteira entre a aplicação e qualquer fornecedor de LLM.
- **Desenvolvimento:** Ollama local é o padrão arquitetural, com preferência por modelos de 3B a 4B parâmetros; seu adaptador ainda não foi implementado ou configurado por esta decisão documental.
- **Produção:** a plataforma corporativa de IA, definida pela Infraestrutura/Arquitetura Corporativa, será consumida por adaptador e configuração. O fornecedor não é decidido pelo +Compras.
- **Estado atual:** `OpenAIProvider` permanece o adaptador implementado em Infrastructure; agentes e regras de negócio continuam dependentes apenas das abstrações.

## Resumo executivo

O BlueprintOS possui uma fundação backend validada para runtime de IA, agentes simples, conhecimento em Markdown, memória e estratégia de negociação em processo, workflow sequencial e publicação/documentação. O +COMPRAS possui CRUD de fornecedores e sincronização ERP operacional validados; ainda não há autenticação corporativa.

## Ciclo atual

- **Fase real atual:** Fase 0 — Fundação, em andamento. O EPIC de documentação foi concluído, mas a fundação prevista no roadmap ainda não está completa.
- **Última sprint comprovadamente concluída:** B2.2.3 — Validação e Aprovação de Dados Enriquecidos de Fornecedor (01/08/2026).
- **Sprint atual:** B2.2.4 — Portal Cadastro Fornecedor Enriquecimento CNPJ, em andamento.
- **Próxima pendência planejada:** consolidar aceite da jornada B2.2.4 e preservar próximas etapas da B2.2. B3 não foi iniciada.
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
| Sincronização de fornecedores | Contrato canônico, adaptadores por BU, importação/exportação/inativação, `LX_SEQUENCIAL`, timestamp Linx, concorrência, idempotência, auditoria append-only e modelo Linx B2.1.2 alinhado ao ERP | Concluída (B2.1, B2.1.1 e B2.1.2) |
| Enriquecimento de fornecedores por CNPJ | `ICnpjConsultaProvider`, `ConsultaCnpjResultado`, `BrasilApiCnpjProvider`, análise de divergências, aprovação/rejeição, atualização seletiva, `FornecedorEnriquecimentoAnalise`, endpoint `POST /fornecedores/consulta-cnpj` e tela React `CadastroFornecedor` | B2.2.4 em andamento |
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
- Portal +COMPRAS completo; B2.2.4 iniciou somente a primeira tela funcional de fornecedores em `frontend/web`.
- n8n e APIs corporativas; integração ERP de fornecedores B2.1 está implementada.

## Agentes e integrações concretos

- **Agentes:** `EchoAgent` e `KnowledgeAgent`. Não existe classe concreta `SeniorBuyerAgent`, `NegotiationAgent`, `ComplianceAgent` ou `RiskAgent`.
- **Integrações:** OpenAI Chat Completions via `OpenAIProvider`; descoberta e sincronização de fornecedores via adaptadores em `SOMA_DESENV`; CLI Git somente para leitura de histórico de documentação.
- **Identidade temporária:** `DevelopmentRequestIdentity` atende somente Development e alimenta `ICurrentIdentity`; fornecedores persistem esse vínculo sem dependência da implementação concreta.

## Qualidade

| Suíte | Executados | Aprovados | Ignorados | Falhos |
|---|---:|---:|---:|---:|
| Unitários | 269 | 269 | 0 | 0 |
| Integração | 4 | 4 | 0 | 0 |
| Total | 273 | 273 | 0 | 0 |

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
