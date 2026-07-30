# VISION.md

> Visão estratégica e guardrails de produto do SOMA BlueprintOS / +COMPRAS. Este documento descreve direção e intenção; o estado comprovado está em `PROJECT_STATE.md`.

## Visão do SOMA BlueprintOS

O SOMA BlueprintOS é a plataforma corporativa de IA que fornece capacidades reutilizáveis para produtos da organização. A visão de longo prazo abrange agentes especializados, workflows, memória, conhecimento, integrações, governança, observabilidade e segurança. A plataforma deve evoluir somente na medida em que viabilize valor comprovado para o primeiro produto, +COMPRAS.

### Arquitetura reutilizável

O alvo é um Modular Monolith com Clean Architecture, DDD pragmático, CQRS, contratos públicos entre módulos e possibilidade de extração futura. A implementação atual ainda usa projetos transversais em `backend/src/BlueprintOS.{Api,Application,Core,Domain,Infrastructure,Shared}`; não deve ser descrita como a estrutura alvo `Apps/`, `BuildingBlocks/` e `Modules/` até que a migração exista.

### Capacidades pretendidas

- Agentes especializados que executam tarefas delimitadas e auditáveis.
- Workflows para coordenar processos e etapas humanas ou automatizadas.
- Memória e conhecimento corporativos para recuperar contexto relevante.
- Integrações desacopladas por contratos com ERPs, APIs e automações.
- Governança, observabilidade, segurança e autorização como requisitos transversais.

## Visão do +COMPRAS

O +COMPRAS é o primeiro produto sobre o BlueprintOS: uma plataforma corporativa de Procurement com IA para apoiar compradores, gestores de compras, especialistas de compliance e áreas solicitantes. O problema de negócio é reduzir a dependência de conhecimento disperso, tornar negociações rastreáveis e apoiar decisões com dados e políticas organizacionais.

O valor esperado é reduzir tempo de preparação de negociações, preservar conhecimento de fornecedores, melhorar consistência de decisões e permitir automação controlada do ciclo de Procurement. O produto não substitui a decisão humana nem deve realizar ações críticas sem aprovação.

### Escopo funcional pretendido

- Portal de acompanhamento para compradores e gestores.
- Recomendação explicável de estratégia de negociação.
- Registro e recuperação de histórico de fornecedores e negociações.
- Workflows de compras, aprovações e exceções.
- Integração com ERP e sistemas corporativos.
- Controle de acesso, auditoria e indicadores operacionais.

### Limites atuais

O +COMPRAS ainda não é utilizável ponta a ponta. Não há portal, API de negócio, Procurement, persistência durável, autenticação, integração ERP nem agente Buyer sênior concreto. As capacidades existentes são fundações internas e não devem ser apresentadas como automação de Procurement entregue.

## Princípios do produto

- **AI First:** IA é uma capacidade central, aplicada apenas onde gere valor verificável.
- **Human in the Loop:** decisões e ações críticas exigem revisão ou aprovação humana.
- **API First:** capacidades de produto devem ser expostas por contratos versionáveis antes de interfaces dependentes.
- **Security by Design:** autenticação, autorização, proteção de dados e gestão de segredos fazem parte do desenho, não uma etapa posterior.
- **Explainable AI:** recomendações devem preservar justificativa, contexto e limites conhecidos.
- **Modularidade:** cada módulo possui responsabilidade única e depende apenas de contratos públicos.
- **Governança:** decisões relevantes, estado da sprint e evidências de conclusão são registrados.
- **Documentação viva:** documentação tem público definido, fonte conhecida e atualização vinculada à sprint.
- **Automação com controle humano:** automação reduz trabalho manual sem esconder decisões nem retirar controles.
- **Integrações desacopladas:** integrações externas ficam atrás de interfaces e não contaminam o domínio.

## Filosofia de Desenvolvimento

O BlueprintOS espera que agentes de IA atuem como engenheiros seniores: identifiquem melhorias, reduzam duplicação, proponham soluções e mantenham documentação continuamente. A autonomia é regida por `AI_AUTONOMY_POLICY.md`; melhorias estratégicas ou de escopo exigem aprovação humana.

## Filosofia arquitetural

| Área | Responsabilidade alvo | Estado comprovado atual |
|---|---|---|
| Módulos | Encapsular domínios e comunicar por Contracts | Parcial: organização atual é `Core`/`Infrastructure` |
| Agentes | Executar tarefas especializadas, com contexto e limites | Implementado: `EchoAgent` e `KnowledgeAgent` básicos |
| Workflows | Orquestrar passos, regras e aprovações | Parcial: workflow sequencial básico |
| Memória e conhecimento | Reter e recuperar contexto organizacional | Parcial: Markdown e memória de negociação em processo |
| APIs | Expor contratos REST/JSON versionáveis | Parcial: somente `GET /health` |
| Persistência | Dados duráveis por EF Core/SQL Server | Não iniciado no código |
| Integrações | Isolar ERP, n8n e provedores externos | Parcial: OpenAI e leitura de Git |
| Autenticação/autorização | Entra ID, perfis e isolamento multi-tenant | Planejado |
| Observabilidade | Logs, métricas, tracing e auditoria | Planejado |

## Roadmap estratégico: 8 fases e 56 sprints

O roadmap executivo organiza a visão futura em oito fases de sete sprints. Nomes e detalhes não existentes em fontes rastreáveis são deliberadamente marcados **A detalhar**. Os estados abaixo não são aprovações de implementação.

| Fase | Objetivo e resultado esperado | Valor de negócio | Dependências | Status real |
|---|---|---|---|---|
| A — Foundation | Consolidar a fundação técnica e documental | Reduzir risco antes do produto | Arquitetura, qualidade e governança | Parcial |
| B — Sourcing Intelligence | Apoiar sourcing e fornecedores | Melhor seleção e preparação de compras | Dados, API e Procurement | Planejado |
| C — Negotiation Automation | Tornar recomendação de negociação utilizável | Decisões mais consistentes | API, persistência e portal | Planejado; estratégia interna existe |
| D — Contract & Compliance | Fluxos de contrato e conformidade | Reduzir risco e retrabalho | Procurement, identidade e políticas | Não iniciado |
| E — Supplier Risk & ESG | Avaliar risco e ESG de fornecedores | Melhor governança de fornecedores | Dados e integrações | Não iniciado |
| F — Predictive Analytics | Indicadores e previsões | Antecipar riscos e oportunidades | Dados duráveis e observabilidade | Não iniciado |
| G — Marketplace & Integrations | Conectar ecossistema corporativo | Operação integrada | APIs, segurança e contratos | Não iniciado |
| H — Scale & Global Rollout | Operar com escala e multiempresa | Expansão sustentável | Observabilidade, multi-tenant e operação | Não iniciado |

O inventário de todas as 56 Work Orders está em `BACKLOG.md`. A ordem e a prioridade de execução dependem de aprovação do Product Owner.

## Estratégia de documentação

| Artefato | Finalidade |
|---|---|
| `VISION.md` | Direção estratégica, princípios e limites do produto |
| `PROJECT_STATE.md` | Estado operacional comprovado por código, testes e Git |
| `ROADMAP.md` | Fases técnicas de alto nível e estado real por fase |
| `CURRENT_SPRINT.md` | Única sprint em execução e seu status |
| Work Orders | Escopo aprovado e critérios executáveis de cada sprint |
| `memory/completed_sprints.md` | Histórico de entregas concluídas e evidências |
| Executive Report | Comunicação concisa para diretoria |
| Client Guide / Product Blueprint | Explicação honesta de produto para clientes e usuários |
| Engineering Guide | Onboarding e referência de implementação |
| Apresentações executivas | Comunicação visual derivada; nunca fonte de conclusão técnica |

## Governança de IA

Agentes de IA devem ler os artefatos canônicos antes de alterar o repositório. Não podem inventar requisitos, expandir escopo, iniciar sprint sem aprovação, declarar entrega sem evidência nem converter planejamento em implementação. Devem registrar decisões relevantes, manter documentação atualizada e diferenciar sempre **Implementado**, **Parcial**, **Planejado**, **Não iniciado** e **Não comprovado**.
