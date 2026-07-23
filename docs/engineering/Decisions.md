# Decisões Arquiteturais (ADRs)

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 15:00:02 UTC
- **Última atualização:** 2026-07-23

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

Ver `.ai/DECISIONS.md` para o texto completo de contexto, decisão e consequências de cada ADR.
