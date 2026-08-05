# Decisões Arquiteturais (ADRs)

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## Architecture Decision Records (ADRs)

- ADR-0001: Adoção de Modular Monolith + Clean Architecture + DDD pragmático
- ADR-0002: Seleção da stack tecnológica oficial
- ADR-0003: CQRS + MediatR + Domain Events como padrão de camada de aplicação
- ADR-0004: Result Pattern em vez de exceções para fluxos de negócio esperados
- ADR-0005: Comunicação entre módulos exclusivamente via Contracts
- ADR-0006: Módulo Documentation implementado sobre a estrutura Core/Infrastructure atual, com pontos de extensão não disruptivos para a arquitetura alvo
- ADR-0007: Publication Engine gera documentos profissionais (HTML/PDF/Markdown) a partir de um modelo comum estruturado (ViewModel), reaproveitando os geradores do Portal de Documentação Viva e usando QuestPDF para PDF sem conversão de HTML
- ADR-0008: PublicationDocument evolui para um modelo rico (Metadata, Assets, Appendix, Theme), com pontos de extensão para recursos futuros sem refatoração
- ADR-0009: Estrutura oficial de diretórios da documentação publicada é `docs/{executive,client,engineering,assets}`, não `docs/{architecture,api,adr}`
- ADR-0011: Identidade temporária de desenvolvimento para antecipar a persistência de fornecedores
- ADR-0012: Persistência de fornecedores isolada por repositório e identidade abstrata
- ADR-0013: Estratégia de Evolução Incremental da Plataforma Operacional e Inteligente do +Compras
- ADR-0014: Estratégia de LLM para Desenvolvimento e Produção
- ADR-0015: Contrato canônico e sincronização bidirecional de fornecedores
- ADR-0016: Modelo Canônico de Fornecedor Integrado ao ERP Linx
- ADR-0017: Estratégia de Construção do Portal Operacional +Compras
- ADR-0018: Ambiente de execução do Portal +Compras é Desenvolvimento Local (Mac)

Ver `.ai/DECISIONS.md` para o texto completo de contexto, decisão e consequências de cada ADR.
