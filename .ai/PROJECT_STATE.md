# PROJECT_STATE.md

> Estado canônico operacional do SOMA BlueprintOS / +COMPRAS. Atualizar ao concluir cada sprint.

> Política de autonomia dos agentes: [AI_AUTONOMY_POLICY.md](./AI_AUTONOMY_POLICY.md).

> Referência técnica consolidada: [ENGINEERING_BLUEPRINT.md](./ENGINEERING_BLUEPRINT.md).

> Fonte externa de descoberta: [COMPRAS_INDIRETAS_SOURCES.md](./sources/COMPRAS_INDIRETAS_SOURCES.md) (não é evidência de implementação).

## Atualização

- **Data:** 02/08/2026
- **Branch:** `feature/a13-procurement-vertical-slice`
- **Commit de referência:** `b08769f`, `3b6d54b` e `0240c35` para as entregas B2.1 e B2.1.1; `77861eb` para B2.1.2 estrutural; `5a6aab8`, `234906c` e `32c9971` para B2.2.
- **Validação desta atualização:** B2.1.3 concluída em código; `dotnet build backend/BlueprintOS.sln` aprovado (histórico). `dotnet test backend/BlueprintOS.sln` completo ainda não foi executado em nenhum ambiente disponível desde a B2.1.3 (o sandbox atual não possui SDK .NET); dois bugs de paginação no teste de múltiplos lotes foram encontrados e corrigidos por inspeção de código (commits `21f1a67`, `ca48dc3`) sem confirmação por execução real da suíte. Validação real de API/VPN/banco permanece pendente em ambiente local corporativo. Auditoria completa em `docs/audits/B-Series-Reconciliation.md` (02/08/2026).

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
- **B2.1.3 operacional:** concluída em código para endurecer o fluxo ERP SOMA → +Compras: leitura paginada/lotes, histórico de execução, erros parciais persistidos, logs estruturados, métricas de sincronização e retorno detalhado em `GET /api/fornecedores/sincronizar-erp`. Pós-entrega, dois bugs reais no loop de paginação foram encontrados pelo teste `Execute_Should_Process_Multiple_Batches_And_Calculate_Totals` e corrigidos: parada prematura em lote parcial (`21f1a67`) e cálculo não determinístico do offset (`ca48dc3`, `skip += tamanhoLote` em vez de `skip += lote.Count`). Nenhuma regra de negócio foi alterada; `dotnet test` completo ainda não foi executado para confirmar 0 falhas.
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
- **Última sprint comprovadamente concluída:** B2.2 — Enriquecimento Inteligente de Fornecedor (01/08/2026).
- **Sprint atual:** B2.1.3 — Endurecimento da Integração ERP de Fornecedores.
- **Próxima pendência planejada:** executar `dotnet test` fora do sandbox, aplicar migration B2.1.3 em `MaisCompras`, validar endpoint com VPN e dados persistidos; B3 não foi iniciada.
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
| Sincronização de fornecedores | Contrato canônico, adaptadores por BU, importação/exportação/inativação, `LX_SEQUENCIAL`, timestamp Linx, concorrência, idempotência, auditoria append-only, modelo Linx alinhado, fluxo operacional ERP SOMA → +Compras, lotes paginados, histórico de execução, erros parciais e métricas | B2.1.3 concluída em código; validação real pendente de VPN/API/banco |
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
| Unitários | 269 | 269 | 0 | 0 |
| Integração | 4 | 4 | 0 | 0 |
| Total | 273 | 273 | 0 | 0 |

> **Tabela desatualizada / não reverificada.** Estes números refletem a última execução real de `dotnet test` registrada, anterior à B2.1.3. Desde então, a sprint B2.1.3 adicionou testes (`SincronizarFornecedoresErpUseCaseTests`, 6 `[Fact]`, mais teste de integração condicionado a VPN) e dois bugs reais de paginação foram encontrados e corrigidos por inspeção estática (commits `21f1a67` e `ca48dc3`) **sem que `dotnet test` tenha sido executado** para confirmar — nenhum ambiente disponível desde então teve SDK .NET instalado. A auditoria de 02/08/2026 (`docs/audits/B-Series-Reconciliation.md`) estimou por leitura de código ~266 testes unitários + 5 de integração (~271 total), divergindo da contagem acima; nenhuma das duas contagens deve ser tratada como confirmada até rodar `dotnet test backend/BlueprintOS.sln` localmente.

Build da solution: última execução real registrada foi sucesso (0 erros/avisos), também anterior à B2.1.3; não reexecutado desde então neste ambiente.

## Riscos e pendências

- Apenas a recomendação de negociação está exposta por API; os demais domínios de negócio ainda não possuem API ou interface utilizável.
- A atualização do nome no ERP permanece limitada pela FK `FORNECEDORES.FORNECEDOR → CADASTRO_CLI_FOR.NOME_CLIFOR`; a B2.1 validou atualização de CNPJ sem operação destrutiva.
- Dados de negociação e documentação ainda não são duráveis.
- A configuração da chave OpenAI depende de ambiente e não há tratamento operacional completo para credenciais, rate limits ou telemetria.
- A arquitetura física diverge do layout alvo; uma migração deve ser planejada somente quando trouxer benefício concreto.
- Métricas e estado de documentação exigem atualização a cada sprint até existir automação de CI.
- Portal +Compras Frontend (commit `8ee8f4e`) teve apenas o frontend validado (`tsc`/`vite build`, 4/4 testes) neste ciclo; `dotnet build`/`dotnet test` do backend não foram executados por falta de SDK .NET no ambiente de revisão e seguem pendentes de execução local antes de encerrar a frente.
- B2.1.3: dois bugs de paginação corrigidos por inspeção de código, sem confirmação por `dotnet test` real (nenhum ambiente com SDK .NET disponível desde a entrega). A tabela de qualidade acima está desatualizada. Ver `docs/audits/B-Series-Reconciliation.md` para o levantamento completo de 02/08/2026.

## Ambiente de execução

- **Decisão vigente (ADR-0018):** o Portal +Compras é desenvolvido e validado em ambiente de **Desenvolvimento Local**, no Mac do desenvolvedor — frontend React via Vite (`http://localhost:5173`) e API .NET via Docker (`http://localhost:8080`) ou `dotnet run` (`http://localhost:5262`).
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
