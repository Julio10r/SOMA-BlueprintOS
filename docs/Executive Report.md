# Executive Report

> Público: Diretoria
> Objetivo: mostrar a evolução do projeto, o roadmap e os indicadores atuais.
> Atualização: a cada sprint.

---

## Resumo Executivo

O BlueprintOS é a plataforma corporativa de IA que sustenta o **+Compras**, primeiro produto construído sobre ela. A fundação backend contém runtime de IA, agentes de referência, memória de negociação em processo e motor de estratégia baseado em regras. Essas capacidades são internas: não há agente Comprador Sênior concreto, API de negócio ou portal utilizável.

A consolidação documental mais recente foi a Sprint A12, que especificou as 56 Work Orders estratégicas sem alterar funcionalidades de negócio. Não há sprint funcional em execução.

---

## Status do Projeto

| Indicador | Valor |
|---|---|
| Build | ✅ Sucesso (0 erros, 0 warnings) |
| Testes automatizados | 230 unitários + 1 integração — 100% passando; 0 ignorados e 0 falhos |
| ADRs registradas | 9 |
| Fase do roadmap | Fase 0 — Fundação (em andamento) |

---

## Roadmap

O projeto foi replanejado oficialmente para o **MVP 1.0**, seguindo a estratégia **Frontend First** (frontend navegável → validação com usuários → blueprint completo do banco → APIs → integrações → Go Live), organizado em Ondas com duração planejada, marcos e critérios de aceite — sem prazo total de projeto ou datas de calendário.

| Onda | Objetivo | Status |
|---|---|---|
| Onda 1 — Fundação Funcional | Frontend navegável, Administração multiempresa, blueprint completo do banco | Planejado |
| Onda 2 — Cadastros | Fornecedores, materiais, serviços, categorias, compradores, centros de custo, sincronização ERP | Parcial (fornecedores concluído) |
| Onda 3 — Processo de Compras | Solicitação, cotação, negociação IA, workflow, orçamento, aprovação, pedido | Planejado |
| Onda 4 — Integrações Operacionais | ERP, Nota Fiscal, Pagamento | Planejado |
| Onda 5 — Go Live | Homologação, observabilidade, performance, segurança | Planejado |

ESG, Portal de Fornecedores, Marketplace, Analytics avançado, Previsão de Demanda, Previsão de Preços, Jurídico, Compliance e Gestão de Riscos foram movidos oficialmente para a versão 1.1.

Detalhe completo em [`.ai/ROADMAP.md`](../.ai/ROADMAP.md).

---

## Estado de sprint

**Última sprint concluída: A12 — Especificação Oficial das 56 Work Orders**

Objetivo: consolidar o catálogo oficial das oito fases e 56 Work Orders, preservando evidências históricas e sem aprovar execução automática.

**Sprint em execução:** nenhuma. **Próxima sprint:** aguardando aprovação do Product Owner.

---

## Entregas Recentes

- Runtime de agentes de IA (`IAgent`, `AgentFactory`), com agentes de exemplo (`EchoAgent`, `KnowledgeAgent`).
- Memória e motor de estratégia de negociação em processo (histórico de fornecedores, score e recomendação por regras), ainda sem agente Comprador Sênior concreto.
- Módulo de conhecimento organizacional (`Knowledge`), ingestão a partir de Markdown.
- Sistema de gestão de documentação do próprio BlueprintOS (versionamento, changelog, ADRs, geração de documentação técnica/funcional).
- Publication Engine: geração automática de relatórios (Executivo, Cliente, Engenharia) em Markdown, HTML e PDF a partir de dados reais do repositório.

---

## Próximos Passos

- Aprovar explicitamente a próxima Work Order antes de iniciar qualquer funcionalidade de produto.
- Definir autenticação, persistência e fronteira de API antes de expor dados corporativos.

---

## Indicadores

| Indicador | Valor |
|---|---|
| Módulos de domínio implementados | AI (Agents, Negotiation), Knowledge, Documentation, Publication, Workflows |
| ADRs aceitas | 8 |
| Cobertura de testes automatizados | 230 testes unitários + 1 de integração, 100% passando; 0 ignorados e 0 falhos |
| Dependências de build sem acesso à internet em runtime | Sim (QuestPDF, QRCoder — bibliotecas .NET puras) |

---

## Riscos

- **Módulos de negócio da Fase 1/3 (Identity, Procurement e workflow como motor de processo) ainda não existem.** O BlueprintOS hoje sustenta capacidades internas de IA, negociação, conhecimento e documentação, mas não os módulos que operacionalizam o +COMPRAS de ponta a ponta.
- **Persistência ainda em memória** para documentação e negociação — nenhum `DbContext`/schema de banco existe hoje.
- **QuestPDF (Community)** é gratuito apenas para empresas com receita anual abaixo de US$ 1M; acima disso, exige licença comercial.

---

## Decisões Arquiteturais

| ADR | Decisão |
|---|---|
| ADR-0001 | Modular Monolith + Clean Architecture + DDD pragmático |
| ADR-0002 | Stack tecnológica oficial (.NET 9, SQL Server, EF Core, React) |
| ADR-0003 | CQRS + MediatR + Domain Events |
| ADR-0004 | Result Pattern em vez de exceções para fluxos esperados |
| ADR-0005 | Comunicação entre módulos exclusivamente via Contracts |
| ADR-0006 | Módulo de Documentação sobre a estrutura atual, com pontos de extensão |
| ADR-0007 | Publication Engine com modelo comum de renderização (Markdown/HTML/PDF) |
| ADR-0008 | Documento de publicação rico (metadados, assets, apêndice, tema) |

Detalhe completo em [`.ai/DECISIONS.md`](../.ai/DECISIONS.md).
