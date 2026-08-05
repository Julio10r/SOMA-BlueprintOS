# Release Notes

Registro curado por marco/sprint relevante — não um log granular de commits. Para o histórico operacional completo de cada sprint (validações, arquivos alterados, riscos), ver `.ai/memory/completed_sprints.md`. Este documento não substitui aquela fonte; resume apenas a entrega técnica de cada marco para quem consulta a documentação técnica.

## Sprint de Infraestrutura — Remoção do Docker e Consolidação do Ambiente Local

Ambiente de desenvolvimento local (sem containers) tornado oficial; `Makefile`, `Dockerfile` e `docker-compose.yml` removidos do fluxo ativo.

## Sprint B2.1.3 — Endurecimento da Integração ERP de Fornecedores

Sincronização ERP SOMA → +Compras tornada rotina operacional rastreável, paginada e resiliente a erros parciais.

## Sprint B2.2 — Enriquecimento Inteligente de Fornecedor

Consulta e enriquecimento de CNPJ via BrasilAPI, comparação campo a campo, aprovação/rejeição e tela React `CadastroFornecedor` conectada à API real.

## Sprint B2.1 / B2.1.1 / B2.1.2 — Sincronização de Fornecedores com ERP

Contrato canônico e sincronização bidirecional de fornecedores entre +Compras e ERP (adaptadores por BU, regra temporal, inativação, idempotência, auditoria imutável), incluindo alinhamento estrutural ao ERP Linx.

## Sprint B2 — Descoberta Inicial de Fornecedores

Consulta somente leitura ao ERP SOMA_DESENV por item, descrição ou categoria, com score explicável e persistência de descobertas no +Compras.

## Sprint B1 — Persistência de Fornecedores

Agregado `Fornecedor`, value object `Cnpj`, DbContext EF Core/SQL Server, migration versionada, casos de uso CRUD e endpoints REST `/fornecedores`.

## Sprint A13 — Primeiro Vertical Slice do +Compras

Endpoint consultivo `POST /api/v1/negociacoes/recomendacoes`, orquestrado por caso de uso Application sobre memória e estratégia de negociação existentes, sem persistência ou alteração de estado.

## Sprints A7–A12 — Fundação de Documentação e Governança

Módulo Documentation (A7), Portal de Documentação Viva com 19 geradores por público (A8), Publication Engine com saída Markdown/HTML/PDF (A9), e consolidação de governança e especificação das 56 Work Orders estratégicas (A10–A12).

---

Para o roadmap de fases futuras, ver `.ai/ROADMAP.md`. Para o catálogo estratégico completo, ver `.ai/work-orders/backlog/README.md`.
