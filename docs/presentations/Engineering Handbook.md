# Engineering Handbook — Conteúdo da Apresentação

> Conteúdo e narrativa da apresentação, a partir de `docs/Engineering Handbook.md` (fonte aprovada, público Desenvolvedores).
> Público: Engenheiros de Software, Tech Leads, novos desenvolvedores em onboarding.
> Fontes: `docs/Engineering Handbook.md`, `.ai/ARCHITECTURE.md`, `.ai/STANDARDS.md`.
> Segue exatamente o mesmo padrão visual e estrutural do `Executive Report.md` — 11 slides, mesma sequência de masters do Design System.

---

## Slide 1 — Capa

**Objetivo:** apresentar o handbook e situar a audiência antes de qualquer detalhe.

**Conteúdo:**
- Engineering Handbook
- SOMA BlueprintOS
- Guia de arquitetura e desenvolvimento para engenheiros

**Speaker Notes:**
Este é o documento de referência para onboarding e desenvolvimento contínuo no BlueprintOS.

---

## Slide 2 — Arquitetura

**Objetivo:** apresentar o estilo arquitetural adotado pela plataforma.

**Conteúdo:**
- Modular Monolith — um único deployable, dividido internamente em módulos.
- Clean Architecture — Api, Application, Domain e Infrastructure como camadas.
- DDD pragmático — regras de negócio concentradas no Domain, sem sobre-engenharia.

**Speaker Notes:**
Ver ADR-0001 e `.ai/ARCHITECTURE.md` para o detalhamento completo da decisão arquitetural.

---

## Slide 3 — Regras de Arquitetura

**Objetivo:** deixar explícitas as regras que nenhum código pode violar.

**Conteúdo:**
- Módulos se comunicam apenas via Contracts — nunca acessando Infrastructure, repositórios ou entidades internas de outro módulo diretamente (ADR-0005).
- Domain não referencia nenhuma outra camada.
- Nenhuma regra de negócio em Api ou Infrastructure.

**Speaker Notes:**
A estrutura alvo descrita em `ARCHITECTURE.md` (`/src/Apps`, `/src/BuildingBlocks`, `/src/Modules`) ainda não foi adotada fisicamente — o layout real atual está documentado na Estrutura das Pastas (ADR-0006).

---

## Slide 4 — Stack

**Objetivo:** apresentar as tecnologias oficiais da plataforma.

**Conteúdo:**
- Backend: .NET 9, ASP.NET Core, C#.
- Dados e Autenticação: SQL Server + Entity Framework Core; Microsoft Entra ID (planejado, Fase 1).
- Infraestrutura: Docker (ativo), Google Cloud Platform.

**Speaker Notes:**
Ver `.ai/PROJECT.md` §4 para a lista oficial completa, incluindo QuestPDF, QRCoder e xUnit.

---

## Slide 5 — Organização do Projeto

**Objetivo:** situar onde cada tipo de código vive hoje no backend.

**Conteúdo:**
- Domain → Application → Infrastructure/Api — um projeto por camada, ainda não por módulo.
- Core concentra os contratos e modelos dos módulos já implementados (AI, Agents, Documentation, Knowledge, Publication, Workflows).
- Suporte: fluxo de dependência sempre em uma direção, nunca circular.

**Speaker Notes:**
Dentro de Core/Infrastructure, cada módulo segue `{Módulo}/{Contracts,Models}` (Core) e `{Módulo}/...` (Infrastructure) — ver ADR-0006.

---

## Slide 6 — Convenções

**Objetivo:** consolidar os padrões de código e de processo do time.

**Conteúdo:**
- Código: idioma inglês, PascalCase em classes/métodos, prefixo `I` em interfaces, Result Pattern no tratamento de erro, `ILogger` para logging, métodos até ~30 linhas, classes até ~300 linhas.
- Governança: idioma português na documentação, proibido `#region`/Service Locator/classes estáticas para regra de negócio/SQL concatenado/dependências cíclicas.

**Speaker Notes:**
Ver `.ai/STANDARDS.md` para o guia completo — este slide é o resumo executável do dia a dia.

---

## Slide 7 — Estrutura das Pastas

**Objetivo:** mostrar o layout físico real do repositório.

**Conteúdo:**
- backend/src — projetos por camada (Api, Application, Domain, Infrastructure, Core, Shared) e tests (UnitTests, IntegrationTests).
- docs/ — documentação permanente (Engineering Handbook, Executive Report, Product Blueprint).
- .ai/ — estado operacional da AI Factory (governança, roadmap, decisões, memória).
- infrastructure/ — docker (ativo); terraform, kubernetes, nginx, monitoring (reservados, ainda vazios).

**Speaker Notes:**
`dist/` é a saída gerada pelo Publication Engine e não é versionada.

---

## Slide 8 — Testes e Qualidade

**Objetivo:** mostrar a prioridade de cobertura e o estado atual dos testes.

**Conteúdo:**
- Application → Domain → Integration — prioridade de cobertura, nesta ordem.
- 167 testes unitários + 1 teste de integração, 100% passando.
- Build sem warnings.

**Speaker Notes:**
Framework xUnit, sem biblioteca de mocking — fakes escritos manualmente. Ainda não há testes End-to-End.

---

## Slide 9 — Ambiente e Deploy

**Objetivo:** apresentar como rodar e publicar a plataforma hoje.

**Conteúdo:**
- Ambiente local: `make up` / `make down` / `make status`, via Docker Compose.
- Deploy atual: local, via `infrastructure/docker/docker-compose.yml`.
- Reservado para escala: Terraform, Kubernetes, Nginx e observabilidade, ainda não implementados (Roadmap, Fase 4).

**Speaker Notes:**
Variáveis sensíveis seguem o padrão `.env.example` — nunca commitadas com valor real.

---

## Slide 10 — Git Flow

**Objetivo:** consolidar o fluxo oficial de versionamento.

**Conteúdo:**
- `main` nunca recebe commit direto.
- Todo trabalho parte de `feature/`, `bugfix/`, `hotfix/` ou `release/`.
- Commits no formato `tipo: descrição`; todo Pull Request contém objetivo, mudanças, impactos, testes realizados e checklist.

**Speaker Notes:**
Ver `.ai/STANDARDS.md` §22 para o checklist completo de Pull Request.

---

## Slide 11 — Conclusão

**Objetivo:** encerrar reforçando a maturidade técnica atual e o próximo passo.

**Conteúdo:**
- 167 testes, 100% passando, build sem warnings — a fundação técnica é sólida hoje.
- Esta é a base para o próximo módulo do BlueprintOS e para os produtos que virão depois.

**Speaker Notes:**
Termina em fato, não em promessa — a qualidade de hoje sustenta a velocidade de amanhã.
