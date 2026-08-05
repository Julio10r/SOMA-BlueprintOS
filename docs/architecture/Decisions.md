# Decisões Arquiteturais

As ADRs (Architecture Decision Records) do SOMA BlueprintOS vivem exclusivamente em [`.ai/DECISIONS.md`](../../.ai/DECISIONS.md) — este documento nunca reproduz o texto integral de uma decisão, apenas referencia as mais relevantes para quem está lendo a arquitetura técnica.

## Decisões mais relevantes para a arquitetura atual

- **ADR-0001** — Modular Monolith + Clean Architecture + DDD pragmático.
- **ADR-0002** — Seleção da stack tecnológica oficial.
- **ADR-0003** — CQRS + MediatR + Domain Events como padrão de camada de aplicação.
- **ADR-0004** — Result Pattern em vez de exceções para fluxos de negócio esperados.
- **ADR-0005** — Comunicação entre módulos exclusivamente via Contracts.
- **ADR-0006** — Módulo Documentation implementado sobre a estrutura Core/Infrastructure atual.
- **ADR-0019** — `docs/` como fonte canônica única da documentação técnica, organizada por domínio; substitui a ADR-0009 nas decisões sobre arquitetura documental. É a decisão vigente para a estrutura descrita neste documento.
- **ADR-0009** — Estrutura de diretórios de documentação publicada por público-alvo (`docs/{executive,client,engineering,assets}`). Histórica/substituída pela ADR-0019; mantida no log apenas como registro do que já foi decidido.
- **ADR-0011** — Identidade temporária de desenvolvimento para antecipar a persistência de fornecedores.
- **ADR-0012** — Persistência de fornecedores isolada por repositório e identidade abstrata.
- **ADR-0013** — Estratégia de evolução incremental da plataforma operacional e inteligente do +Compras.
- **ADR-0015** — Contrato canônico e sincronização bidirecional de fornecedores.
- **ADR-0016** — Modelo canônico de fornecedor integrado ao ERP Linx.
- **ADR-0017** — Estratégia de construção do Portal Operacional +Compras.
- **ADR-0018** — Ambiente de execução local sem Docker.

Para contexto completo, alternativas consideradas e consequências de qualquer decisão, ler o texto integral em `.ai/DECISIONS.md`.
