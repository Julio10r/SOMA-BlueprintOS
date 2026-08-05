# PROJECT_STATE.md

> Estado canônico operacional do SOMA BlueprintOS / +COMPRAS. Atualizar ao concluir cada sprint.

> Política de autonomia dos agentes: [AI_AUTONOMY_POLICY.md](./AI_AUTONOMY_POLICY.md).

> Referência técnica consolidada: [ENGINEERING_BLUEPRINT.md](./ENGINEERING_BLUEPRINT.md).

> Fonte externa de descoberta: [COMPRAS_INDIRETAS_SOURCES.md](./sources/COMPRAS_INDIRETAS_SOURCES.md) (não é evidência de implementação).

## Atualização

- **Data:** 03/08/2026
- **Branch:** `feature/a13-procurement-vertical-slice`
- **Commit de referência:** `601d937` e `7bf3bf4` para a remoção do Docker e consolidação do ambiente local; `b08769f`, `3b6d54b` e `0240c35` para as entregas B2.1 e B2.1.1; `77861eb` para B2.1.2 estrutural; `5a6aab8`, `234906c` e `32c9971` para B2.2; hardening limite/ChangeTracker da B2.1.3 registrado na atualização anterior.
- **Validação desta atualização:** Sprint de infraestrutura "Remoção do Docker e Consolidação do Ambiente Local" concluída e auditada. `dotnet build backend/BlueprintOS.sln` aprovado, 0 erros e 0 avisos. `dotnet test backend/BlueprintOS.sln` aprovado: 286 testes (281 unitários + 5 integração), 0 falhas. `npm run build` do frontend (`tsc -b && vite build`) aprovado. Scripts de desenvolvimento local (`start-dev.sh`/`stop-dev.sh`/`health-check.sh`) validados. Docker, `Makefile`, `Dockerfile` e `docker-compose.yml` foram removidos do fluxo de desenvolvimento; o ambiente oficial passa a ser 100% local, sem containers (ver ADR-0018 atualizada em `.ai/DECISIONS.md`). Auditoria final não encontrou resíduo funcional, referência quebrada ou documento contraditório. Detalhes em `.ai/CURRENT_SPRINT.md` e `.ai/memory/completed_sprints.md`.

## Sistema de Work Orders

- **Estado:** Implementado em 30/07/2026.
- **Evidência:** [templates/README.md](./templates/README.md) e os sete templates padronizados para desenvolvimento, épicos, auditorias, refatorações, hotfixes, spikes e releases.
- **Uso:** os templates complementam, sem substituir, as Work Orders estratégicas em `workorders/` e a governança de [WORKFLOW.md](./WORKFLOW.md). Eles exigem leitura prévia de visão, workflow, estado do projeto e sprint atual.

## Evolução arquitetural do +Compras

- **Decisão aceita:** [ADR-0013](./DECISIONS.md) estabelece a evolução em dois momentos: plataforma operacional primeiro e inteligência progressiva sobre dados reais depois.
- **Princípio obrigatório:** toda operação crítica possui alternativa manual; IA acelera e orienta, mas não é pré-requisito para cadastrar ou selecionar fornecedor/item, criar pedido, enviá-lo ao ERP ou acompanhar a integração.
- **Portal:** a ADR-0017 definiu o Portal Operacional +Compras como navegação e identidade visual completas, com evolução funcional incremental por domínio. B2.2.4 concluiu a primeira vertical slice funcional em React com a tela `CadastroFornecedor`. A próxima frente formal é o Portal +Compras Frontend, a ser executada pelo Claude Code.
- **B2/B2.1/B2.1.1:** B2 permanece como estrutura inicial de descoberta e score (100/80/60/40). B2.1 concluiu sincronização bidirecional, regra temporal, inativação, auditoria e concorrência; B2.1.1 concluiu o mapeamento canônico ERP → +Compras.
- **B2.1.2 estrutural:** concluída conforme ADR-0016, com modelo fornecedor alinhado ao Linx: `Cnpj_Cpf`, `TipoPessoa`, separação de `RazaoSocial`/`NomeFantasia`, proteção do nome fantasia controlado pelo Linx, flags `Beneficiador`/`Licenciado`, domínios ERP estruturados, FKs opcionais e contrato frontend inicial.
- **B2.1.3 operacional:** concluída para endurecer o fluxo ERP SOMA → +Compras: leitura paginada/lotes, histórico de execução, erros parciais persistidos, logs estruturados, métricas de sincronização e retorno detalhado em `GET /api/fornecedores/sincronizar-erp`. Dois bugs de paginação encontrados pelo teste `Execute_Should_Process_Multiple_Batches_And_Calculate_Totals` foram corrigidos: parada prematura em lote parcial (`21f1a67`) e cálculo não determinístico do offset (`ca48dc3`). Em 02/08/2026, a validação real contra Docker/VPN/SQL Server corporativo encontrou e corrigiu mais três problemas: dependência Docker desnecessária que impedia a API de subir, `limite` tratado como tamanho de página em vez de teto total de fornecedores processados, e erro parcial de persistência que virava HTTP 500 por poluição do `ChangeTracker` do EF Core. Nenhuma regra de negócio foi alterada em nenhuma correção. `dotnet test backend/BlueprintOS.sln` executado com sucesso: 282 testes, 0 falhas.
- **B2.2:** concluída como Enriquecimento Inteligente de Fornecedor. O módulo de fornecedores possui cadastro, consulta CNPJ, enriquecimento, aprovação, rejeição, integração ERP e auditoria. A B2.2.1 foi concluída com contrato `ICnpjConsultaProvider`, retorno tipado e auditoria persistida. A B2.2.2 implementou `BrasilApiCnpjProvider` como adaptador gratuito BrasilAPI. A B2.2.3 concluiu comparação campo a campo, aprovação/rejeição, atualização seletiva, auditoria de decisões e proteção `NomeFantasia`/Linx. A B2.2.4 concluiu o portal funcional de cadastro, consulta, divergências e decisão humana.
- **Portal +Compras Frontend:** concluído tecnicamente no frontend (commit `8ee8f4e`, branch `feature/a13-procurement-vertical-slice`). Shell de navegação (AppShell) e rotas React Router criados; módulo Fornecedores integrado à API real (cadastro, consulta CNPJ, enriquecimento, aprovação/rejeição); demais módulos (Pedidos, Negociações, Indicadores, Agentes IA, Configurações) implementados como telas demonstrativas honestas, sem persistência simulada; Design System AZZAS 2154/GDT aplicado. Build frontend aprovado (`tsc` + `vite build`, 4/4 testes). Revisão manual de código confirmou os endpoints de fornecedores, a regra `Cnpj_Cpf` (`varchar(14)` alfanumérico), a proteção de `NomeFantasia` (só editável quando origem é ERP) e o alerta não bloqueante de situação cadastral `Baixada/Suspensa/Inapta`. Validação de backend (`dotnet build`/`dotnet test`) **não foi executada neste ciclo** por ausência do SDK .NET no ambiente de revisão; permanece pendente de execução local. Roteiro de demonstração em `docs/demo/PortalMaisComprasDemo.md`.

## Estratégia de LLM

- **Decisão aceita:** a [ADR-0014](./DECISIONS.md) determina `IAIProvider` e `IAIRuntime` como fronteira entre a aplicação e qualquer fornecedor de LLM.
- **Desenvolvimento:** Ollama local é o padrão arquitetural, com preferência por modelos de 3B a 4B parâmetros; seu adaptador ainda não foi implementado ou configurado por esta decisão documental.
- **Produção:** a plataforma corporativa de IA, definida pela Infraestrutura/Arquitetura Corporativa, será consumida por adaptador e configuração. O fornecedor não é decidido pelo +Compras.
- **Estado atual:** `OpenAIProvider` permanece o adaptador implementado em Infrastructure; agentes e regras de negócio continuam dependentes apenas das abstrações.

## Resumo executivo

O BlueprintOS possui uma fundação backend validada para runtime de IA, agentes simples, conhecimento em Markdown, memória e estratégia de negociação em processo, workflow sequencial e publicação/documentação. O +COMPRAS possui CRUD de fornecedores e sincronização ERP operacional validados; ainda não há autenticação corporativa.

## Ciclo atual

- **Fase real atual:** Fase 0 — Fundação, em andamento. O EPIC de documentação foi concluído, mas a fundação prevista no roadmap ainda não está completa.
- **Última sprint comprovadamente concluída:** Remoção do Docker e Consolidação do Ambiente Local (03/08/2026), sprint de infraestrutura sem escopo funcional, com auditoria final aprovada (build, testes e scripts validados).
- **Sprint funcional comprovadamente concluída anterior:** B2.1.3 — Endurecimento da Integração ERP de Fornecedores (02/08/2026), com validação real contra VPN/`MaisCompras`.
- **Sprint atual:** nenhuma em andamento; aguardando aprovação explícita da próxima Work Order.
- **Próxima pendência planejada:** B3 não foi iniciada; depende de decisão explícita do Product Owner.
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
| Sincronização de fornecedores | Contrato canônico, adaptadores por BU, importação/exportação/inativação, `LX_SEQUENCIAL`, timestamp Linx, concorrência, idempotência, auditoria append-only, modelo Linx alinhado, fluxo operacional ERP SOMA → +Compras, lotes paginados com teto operacional real, histórico de execução, erros parciais e métricas | B2.1.3 concluída e validada em 02/08/2026 contra Docker/VPN/`MaisCompras` reais |
| Enriquecimento de fornecedores por CNPJ | `ICnpjConsultaProvider`, `ConsultaCnpjResultado`, `BrasilApiCnpjProvider`, análise de divergências, aprovação/rejeição, atualização seletiva, `FornecedorEnriquecimentoAnalise`, endpoint `POST /fornecedores/consulta-cnpj` e tela React `CadastroFornecedor` | B2.2 concluída |
| Portal +Compras (frontend) | AppShell, rotas React Router, `ApprovalPanel.tsx`, `SupplierComparison.tsx`, módulo Fornecedores conectado à API real; demais módulos demonstrativos | Concluído no frontend (commit `8ee8f4e`); backend não revalidado neste ciclo (sem SDK .NET no ambiente) |
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
- Portal +COMPRAS completo; somente fornecedor está funcional e conectado ao backend em `frontend/web`.
- n8n e APIs corporativas; integração ERP de fornecedores B2.1 está implementada.

## Agentes e integrações concretos

- **Agentes:** `EchoAgent` e `KnowledgeAgent`. Não existe classe concreta `SeniorBuyerAgent`, `NegotiationAgent`, `ComplianceAgent` ou `RiskAgent`.
- **Integrações:** OpenAI Chat Completions via `OpenAIProvider`; descoberta e sincronização de fornecedores via adaptadores em `SOMA_DESENV`; CLI Git somente para leitura de histórico de documentação.
- **Identidade temporária:** `DevelopmentRequestIdentity` atende somente Development e alimenta `ICurrentIdentity`; fornecedores persistem esse vínculo sem dependência da implementação concreta.

## Qualidade

| Suíte | Executados | Aprovados | Ignorados | Falhos |
|---|---:|---:|---:|---:|
| Unitários | 290 | 290 | 0 | 0 |
| Integração | 5 | 5 | 0 | 0 |
| Total | 295 | 295 | 0 | 0 |

> Tabela atualizada em 05/08/2026 por execução real de `dotnet build`/`dotnet test backend/BlueprintOS.sln` (não estimativa): build aprovado com 0 erros e 0 avisos; testes aprovados, 0 falhas.

Build da solution: `dotnet build backend/BlueprintOS.sln` executado em 03/08/2026, sucesso, 0 erros e 0 avisos. Build do frontend: `npm run build` (`tsc -b && vite build`) executado em 03/08/2026, sucesso.

## Riscos e pendências

- Apenas a recomendação de negociação está exposta por API; os demais domínios de negócio ainda não possuem API ou interface utilizável.
- A atualização do nome no ERP permanece limitada pela FK `FORNECEDORES.FORNECEDOR → CADASTRO_CLI_FOR.NOME_CLIFOR`; a B2.1 validou atualização de CNPJ sem operação destrutiva.
- Dados de negociação e documentação ainda não são duráveis.
- A configuração da chave OpenAI depende de ambiente e não há tratamento operacional completo para credenciais, rate limits ou telemetria.
- A arquitetura física diverge do layout alvo; uma migração deve ser planejada somente quando trouxer benefício concreto.
- Métricas e estado de documentação exigem atualização a cada sprint até existir automação de CI.
- Portal +Compras Frontend (commit `8ee8f4e`) teve apenas o frontend validado (`tsc`/`vite build`, 4/4 testes) neste ciclo; `dotnet build`/`dotnet test` do backend não foram executados por falta de SDK .NET no ambiente de revisão e seguem pendentes de execução local antes de encerrar a frente.
- B2.1.3: concluída e validada em 02/08/2026 (build, testes e execução real contra VPN/`MaisCompras`, à época via Docker). Ver `.ai/memory/completed_sprints.md` para o detalhamento dos três problemas encontrados e corrigidos nessa validação, e `docs/audits/B-Series-Reconciliation.md` para o levantamento anterior (histórico, pré-validação real).
- Remoção do Docker (03/08/2026): concluída e auditada. Docker deixou de ser parte do fluxo de desenvolvimento; o ambiente oficial é local, sem containers (ADR-0018 atualizada). Nenhum resíduo funcional foi encontrado na auditoria final.

## Ambiente de execução

- **Decisão vigente (ADR-0018):** o Portal +Compras é desenvolvido e validado em ambiente de **Desenvolvimento Local**, no Mac do desenvolvedor — frontend React via Vite (`http://localhost:5173`) e API .NET via `dotnet run` (`http://localhost:5262`), sem Docker.
- Persistência aponta para o SQL Server corporativo (`SOMA_DESENV`), acessado via VPN; não se usa SQL Server em container para o fluxo principal. Connection strings seguem configuráveis via user-secrets/variáveis de ambiente, sem valores fixos no repositório.
- CORS do backend liberado apenas para as origens locais `http://localhost:5173` e `http://127.0.0.1:5173`.
- Uma tentativa anterior de publicar o frontend via n8n (com backend exposto por túnel ngrok) foi revertida: o n8n só serve HTML como string única e não havia ambiente de backend publicado além de localhost. Publicação via n8n/GCP passa a ser tratada como opção futura de homologação, não como ambiente corrente.

## Divergências ainda abertas

- O roadmap estratégico de apresentação +COMPRAS continua sem atualização visual; as correções necessárias estão listadas em `docs/presentations/ROADMAP_UPDATE.md`.
- A estrutura alvo descrita em `ARCHITECTURE.md` não é a estrutura física atual.
- Nenhuma Work Order futura está aprovada; a próxima sprint depende de decisão explícita do Product Owner.

## Auditoria de repositório

- **Etapa 1 — Higiene e artefatos gerados (30/07/2026):** remoção exclusiva de resíduos locais comprovados (`.DS_Store`, `bin/`, `obj/` e `dist/`) e reforço do `.gitignore`. Não houve alteração de progresso funcional. Restore serial, build e 231 testes foram concluídos; o único aviso `NU1900` decorre da indisponibilidade de consulta de vulnerabilidades ao nuget.org, sem impedir a validação. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-01.md).
- **Etapa 2 — Obsoletos, duplicados e órfãos (30/07/2026):** auditoria investigativa de 629 arquivos versionados. Foram registrados 13 grupos candidatos (0 para remoção automática); não houve alteração funcional, remoção, movimento ou renomeação. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-02.md).
- **Etapa 3 — Consolidação documental e estado A12 (30/07/2026):** fontes canônicas de visão e workflow definidas, documentos históricos controlados e saídas derivadas republicadas para refletir A12. Não houve alteração de progresso funcional. Ver [relatório da auditoria](../docs/audits/repository-cleanup-step-03.md).
