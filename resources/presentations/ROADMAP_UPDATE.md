# +COMPRAS Strategic Roadmap — Atualização factual necessária

> Referência para a próxima edição visual do PowerPoint. Este arquivo não altera o layout, masters ou conteúdo binário de `+COMPRAS Strategic Roadmap.pptx`.
>
> Base de evidência: código, validações de 30/07/2026, histórico Git e `.ai/PROJECT_STATE.md`.

## Slide 01 — Capa

- **Conteúdo atual incorreto:** não há incorreção factual na identificação do produto e da plataforma.
- **Conteúdo atualizado:** manter título e tagline; incluir data/versão de atualização somente se o padrão visual permitir.
- **Motivo da alteração:** preservar a capa; a atualização é de estado, não de identidade visual.

## Slide 02 — Dashboard Executivo

- **Conteúdo atual incorreto:** Sprint atual A6, última sprint A5, próximo marco A7, progresso de 12% e referência a Multi-Tenant concluído.
- **Conteúdo atualizado:** última sprint concluída A12 — Especificação Oficial das 56 Work Orders. Não há sprint em execução; próxima sprint aguardando aprovação do Product Owner. Exibir progresso como “sem percentual verificável” e estado “Fase 0 — Fundação em andamento”.
- **Motivo da alteração:** A5 Multi-Tenant e A6 Senior Buyer Agent não são comprovados pelo código nem pelo histórico Git; A12 consolidou o catálogo sem transformar planejamento em entrega.

## Slide 03 — Roadmap Geral

- **Conteúdo atual incorreto:** as oito fases aparecem como cronograma confirmado do produto.
- **Conteúdo atualizado:** apresentar as fases A–H como visão estratégica planejada, separada do roadmap técnico canônico de Fases 0–4; destacar que apenas a fundação backend/documentação possui evidência de implementação.
- **Motivo da alteração:** o repositório não contém evidência de execução das fases estratégicas B–H.

## Slide 04 — Fase A: Foundation

- **Conteúdo atual incorreto:** tratar todas as sprints da fase como concluídas, incluindo Multi-Tenant e Senior Buyer Agent.
- **Conteúdo atualizado:** marcar como “parcialmente implementada”: runtime OpenAI, agentes Echo/Knowledge, conhecimento Markdown, memória/estratégia de negociação em processo, workflow sequencial, documentação e publicação. Marcar identidade, multi-tenant, API de negócio, frontend e persistência como pendentes.
- **Motivo da alteração:** são as únicas capacidades comprovadas no código; não há módulo de Identity nem agente Buyer sênior concreto.

## Slide 05 — Fase B: Sourcing Intelligence

- **Conteúdo atual incorreto:** qualquer indicação de entrega ou início confirmado.
- **Conteúdo atualizado:** “planejada — sem sprint aprovada e sem implementação comprovada”.
- **Motivo da alteração:** não há módulo de sourcing, Procurement ou integração de fornecedor no repositório.

## Slide 06 — Fase C: Negotiation Automation

- **Conteúdo atual incorreto:** apresentar automação de negociação como produto entregue.
- **Conteúdo atualizado:** “planejada; há somente motor interno de estratégia e memória em processo, sem API, portal, persistência ou agente especializado concreto”.
- **Motivo da alteração:** a capacidade existente não forma um fluxo utilizável de negociação.

## Slide 07 — Fase D: Contract & Compliance

- **Conteúdo atual incorreto:** qualquer status de implementação ou agente de compliance entregue.
- **Conteúdo atualizado:** “planejada — não iniciada”.
- **Motivo da alteração:** não há código de contratos, compliance ou agente correspondente.

## Slide 08 — Fase E: Supplier Risk & ESG

- **Conteúdo atual incorreto:** qualquer status de implementação ou agente de risco entregue.
- **Conteúdo atualizado:** “planejada — não iniciada”.
- **Motivo da alteração:** não há código, dados ou integrações de risco/ESG.

## Slide 09 — Fase F: Predictive Analytics

- **Conteúdo atual incorreto:** qualquer status de implementação de analytics preditivo.
- **Conteúdo atualizado:** “planejada — não iniciada”.
- **Motivo da alteração:** Dashboard e Analytics não foram implementados; não há persistência de dados para essa capacidade.

## Slide 10 — Fase G: Marketplace & Integrations

- **Conteúdo atual incorreto:** indicar marketplace, ERP ou n8n como integrações existentes.
- **Conteúdo atualizado:** “planejada — não iniciada”; listar somente OpenAI Chat Completions e leitura de Git como integrações concretas atuais.
- **Motivo da alteração:** ERP, n8n e APIs corporativas não existem no código.

## Slide 11 — Fase H: Scale & Global Rollout

- **Conteúdo atual incorreto:** qualquer compromisso de rollout, escala ou operação global como entrega atual.
- **Conteúdo atualizado:** “planejada — não iniciada”; pré-requisitos pendentes incluem autenticação, multi-tenant, persistência, CI/CD e observabilidade.
- **Motivo da alteração:** infraestrutura de produção e requisitos operacionais ainda não estão implementados.

## Slide 12 — Próximos Marcos

- **Conteúdo atual incorreto:** datas Q4 2026/Q1 2027/Q2 2027 e dependências A4/A5 apresentadas como cronograma confirmado.
- **Conteúdo atualizado:** substituir por “próxima sprint aguardando aprovação do Product Owner”; listar que qualquer entrega funcional exige Work Order aprovada, contrato de API, autenticação, persistência e fronteira de dados conforme aplicável.
- **Motivo da alteração:** não há evidência de planejamento aprovado para A4/A5, nem base para datas de entrega.

## Slide 13 — Roadmap Consolidado

- **Conteúdo atual incorreto:** ausência de distinção entre capacidade implementada, parcial e planejada.
- **Conteúdo atualizado:** adicionar legenda de estado: Implementado (fundação interna), Parcial (Fase 0), Planejado (fases estratégicas futuras) e não atribuir status “em execução” a fases sem evidência.
- **Motivo da alteração:** evita que visão futura seja interpretada como entrega real.

## Slide 14 — Indicadores

- **Conteúdo atual incorreto:** “56 sprints”, “10 módulos” e “3+ agentes especializados” podem ser lidos como entregas; omite a validação real.
- **Conteúdo atualizado:** “sprints históricas A7–A12 registradas; 6 capacidades backend implementadas (AI/Agents, Knowledge, Negociação/Memória, Workflow básico, Documentation, Publication); 2 agentes concretos (Echo e Knowledge); 231 testes aprovados na validação registrada; módulos de produto e integrações corporativas planejados”.
- **Motivo da alteração:** métricas devem refletir evidência atual e distinguir módulos previstos dos implementados.

## Slide 15 — Arquitetura

- **Conteúdo atual incorreto:** apresenta Identity, Planner, Workflow de negócio, Procurement, agentes especializados, ERP, marketplace e portais como camadas existentes.
- **Conteúdo atualizado:** separar “implementado” (backend Core/Infrastructure, OpenAI, agentes básicos, Knowledge, negociação interna, documentação/publicação) de “planejado” (Identity, Planner, Procurement, agentes +COMPRAS, ERP/n8n, portal). Indicar que a arquitetura alvo modular ainda não é a estrutura física atual.
- **Motivo da alteração:** esses componentes planejados não possuem implementação correspondente no repositório.

## Slide 16 — Encerramento

- **Conteúdo atual incorreto:** não há correção factual obrigatória no encerramento.
- **Conteúdo atualizado:** manter a mensagem institucional; atualizar versão e data para a edição que incorporar as correções acima.
- **Motivo da alteração:** preservar o layout e a finalidade institucional, sinalizando a edição factual revisada.
