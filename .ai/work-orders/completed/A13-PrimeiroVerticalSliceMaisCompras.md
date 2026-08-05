# Work Order — A13 — Primeiro Vertical Slice do +Compras

## Metadados

- Status: Completed
- Responsável: Codex
- Prioridade: Alta
- Dependências: A2 e A6; ADR-0011.
- Data de aprovação: 30/07/2026

## Objetivo

Implementar o primeiro fluxo funcional de ponta a ponta do +Compras.

## Contexto

Foram lidos [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md), [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), [DECISIONS.md](../DECISIONS.md) e a documentação específica aplicável. A implementação foi concluída com evidências de build, testes e smoke test.

## Task Packet

- ID: A13
- Título: Primeiro Vertical Slice do +Compras
- Descrição: expor o primeiro fluxo consultivo de negociação, a partir das capacidades já existentes.
- Executor: Codex
- Entradas: contexto da compra e dados de negociação aprovados para o fluxo.
- Saídas: recomendação consultiva e explicável.
- Critérios de aceite: contrato versionado, validação, decisão humana explícita, testes e documentação.
- Testes obrigatórios: build, unitários, integração e smoke test.

## Escopo

- Expor o primeiro fluxo consultivo de negociação do +Compras por contrato REST/JSON versionável.
- Definir o endpoint inicial como `POST /api/v1/negociacoes/recomendacoes`; o versionamento inicial será feito pela URL.
- Manter o endpoint exclusivamente consultivo: ele não executará compras, negociações ou alterações de estado.
- Reutilizar as capacidades existentes de memória e estratégia de negociação, sem reimplementar regras de negócio na API.
- Manter a recomendação estritamente consultiva, com decisão humana obrigatória.
- O request representará somente dados já aceitos pelas capacidades existentes: identificador ou referência da solicitação, contexto da compra, categoria, quantidade, preço atual, preço histórico quando disponível, nível de urgência, recorrência, existência de fornecedor novo, nível de concorrência e demais dados já suportados pelas estratégias existentes. Os nomes, tipos, obrigatoriedade e validações finais serão confirmados pela inspeção dos contratos existentes no código.
- O response apresentará, no mínimo, identificador da requisição, recomendação consultiva, estratégia sugerida, justificativas explicáveis, alertas, nível de confiança ou probabilidade quando já suportado, alternativas quando existentes e indicação explícita de que a decisão final pertence ao usuário humano. Não serão criadas métricas artificiais nem resultados ausentes das capacidades atuais.
- Aplicar a estratégia de identidade definida na [ADR-0011](../DECISIONS.md#adr-0011-identidade-temporária-de-desenvolvimento-para-antecipar-a-persistência-de-fornecedores): identidade temporária somente em `Development`, contrato desacoplado e preparado para futura substituição pelo Microsoft Entra ID.

## Fora do escopo

- Implementar Microsoft Entra ID, autenticação corporativa, autorização ou uso produtivo.
- Criar cadastro, persistência ou migração de fornecedores; essas capacidades pertencem à B1.
- Banco de dados, ERP, portal, frontend, pedidos, cotações ou execução automática de compras.
- Alterar a estratégia funcional de negociação existente.

## Arquitetura

Seguir a estrutura física atual sem criar arquitetura paralela. A camada HTTP não poderá acessar diretamente estratégias de negociação, memória ou serviços de domínio: todo o fluxo será orquestrado por um caso de uso da camada Application.

Fluxo arquitetural esperado:

`Controller REST → Application Use Case → contratos de estratégia/memória → resultado de domínio → mapeamento HTTP`

O Controller conterá apenas recebimento HTTP, validação de contrato, chamada ao caso de uso, mapeamento de resposta, logging e tratamento padronizado de erros. A API retornará códigos HTTP coerentes para sucesso, validação e falhas inesperadas.

A identidade temporária será obtida por contrato substituível e somente poderá funcionar em `Development`; a aplicação deverá falhar de forma segura se essa implementação for utilizada fora desse ambiente. Nenhuma persistência de fornecedor ou usuário será criada na A13. O contrato permitirá futura substituição pelo Microsoft Entra ID sem alterar o caso de uso, preservando compatibilidade para a migração posterior conforme a ADR-0011.

## Banco

Sem impacto nesta Work Order. A persistência de fornecedores e o vínculo ao usuário temporário serão definidos e implementados na B1, observando a ADR-0011 e a futura migração de identificadores para Entra ID.

## Observabilidade

- Cada chamada deverá possuir `requestId` ou `correlationId`, propagado na resposta.
- Logging estruturado registrará tempo de execução, estratégia selecionada e resultado técnico da operação.
- Logs não poderão conter dados sensíveis.

## Validação e erros

- Request inválido retornará resposta padronizada.
- Campos obrigatórios e limites serão validados antes do caso de uso.
- Erros de domínio não exporão detalhes internos.
- Falhas inesperadas retornarão resposta segura com `requestId`, sem stack trace ou informações internas.

## Testes

- Build: solução compilando sem erros e sem warnings críticos novos.
- Unitários: validação do request; orquestração do caso de uso; reutilização das estratégias existentes; decisão humana obrigatória; identidade temporária limitada a `Development`; mapeamento da recomendação e justificativas.
- Integração: chamada real ao endpoint; contrato JSON de entrada e saída; códigos HTTP; `requestId`; fluxo completo entre API, Application e capacidades existentes; garantia de que o Controller não contém regra de negócio.
- Smoke test: `/health` respondendo corretamente; endpoint de recomendação respondendo com payload válido; execução do fluxo consultivo completo.

## Documentação

Atualizar somente os documentos afetados, incluindo `PROJECT_STATE.md`, `CURRENT_SPRINT.md`, histórico e documentação específica da sprint quando aplicável.

## Critérios de aceite

- [x] Objetivo e escopo aprovados foram atendidos.
- [x] Compatibilidade e funcionalidades existentes foram preservadas.
- [x] Build sem erros e sem warnings críticos.
- [x] Testes aplicáveis aprovados.
- [x] Documentação e evidências atualizadas.
- [x] Endpoint REST e versão definidos.
- [x] Contratos de request e response documentados.
- [x] Controller sem regras de negócio.
- [x] Application Use Case responsável pela orquestração.
- [x] Estratégias e memória existentes reutilizadas.
- [x] Decisão humana explicitamente obrigatória.
- [x] Identidade temporária bloqueada fora de `Development`.
- [x] Logging estruturado e `requestId` implementáveis.
- [x] Contrato preparado para futura substituição pelo Entra ID.
- [x] Nenhuma persistência ou integração fora do escopo criada.

## Git Workflow

Seguir o Git Flow e Conventional Commits do projeto. Antes do commit, executar `git status` e `git diff --stat`; concluir revisão, validações e aprovação conforme [WORKFLOW.md](../WORKFLOW.md).

## Relatório final

- Objetivo entregue: endpoint consultivo `POST /api/v1/negociacoes/recomendacoes`.
- Decisões técnicas: caso de uso Application reutiliza `INegotiationMemory` e `INegotiationStrategy`; a identidade temporária é um adaptador HTTP somente para `Development`.
- Arquivos alterados: API, Application, DI, testes e documentação impactada.
- Testes executados: build, 231 testes unitários, 1 teste de integração e smoke test HTTP em Development e Production.
- Resultado do build: sucesso, 0 erros e 4 avisos `NU1900` de conectividade com NuGet.
- Riscos: sem persistência e sem identidade corporativa; o adaptador de desenvolvimento não é utilizável em Production.
- Próximos passos: B1 pode evoluir persistência de fornecedores sob a ADR-0011; Entra ID permanece fora de escopo.
- Commit e push: registrados após a validação final.
