# DECISIONS.md

> Log oficial de Architecture Decision Records (ADRs) do SOMA BlueprintOS.

Este arquivo é o log canônico de ADRs do projeto, substituindo a convenção de arquivos individuais em `.ai/decisions/ADR-000N.md` mencionada em [ARCHITECTURE.md](./ARCHITECTURE.md) §14. Novas ADRs devem ser adicionadas ao final deste arquivo, com numeração sequencial.

Formato de cada ADR:

- **Título**
- **Status** (Proposto / Aceito / Rejeitado / Substituído)
- **Contexto** — o problema ou força que motivou a decisão
- **Decisão** — o que foi decidido
- **Consequências** — efeitos, positivos e negativos, da decisão

---

## ADR-0001: Adoção de Modular Monolith + Clean Architecture + DDD pragmático

**Status:** Aceito

**Contexto:** O BlueprintOS precisa suportar múltiplos domínios de negócio (Identity, Planner, Procurement, Workflow, Knowledge, Memory, Agents, Notifications, Dashboard, Analytics) evoluindo de forma independente, sem o custo operacional de microsserviços desde o início.

**Decisão:** Adotar Modular Monolith com Clean Architecture e DDD pragmático, organizando o código em `/src/Apps`, `/src/BuildingBlocks` e `/src/Modules`, cada módulo com camadas Domain/Application/Infrastructure/Api.

**Consequências:**
- Módulos evoluem com baixo acoplamento e alta coesão.
- Caminho aberto para extração futura em microsserviços sem reescrita (ver ARCHITECTURE.md §13).
- Exige disciplina para não criar dependências diretas entre módulos.

---

## ADR-0002: Seleção da stack tecnológica oficial

**Status:** Aceito

**Contexto:** O projeto precisa de uma stack única, moderna e suportada, adequada a uma plataforma corporativa multi-tenant hospedada em nuvem.

**Decisão:** Backend em .NET 9 / ASP.NET Core / C#; banco SQL Server com Entity Framework Core; frontend em React/TypeScript; containers via Docker; cloud Google Cloud Platform; autenticação via Microsoft Entra ID; controle de versão em Git/GitHub.

**Consequências:**
- Stack única reduz curva de aprendizado e custo de manutenção.
- Dependência do ecossistema .NET/Microsoft para autenticação e runtime.
- Ver [context/tech-stack.md](./context/tech-stack.md) para detalhes operacionais.

---

## ADR-0003: CQRS + MediatR + Domain Events como padrão de camada de aplicação

**Status:** Aceito

**Contexto:** A camada Application precisa de um padrão consistente para separar leitura de escrita e para propagar efeitos colaterais de domínio sem acoplar módulos entre si.

**Decisão:** Utilizar CQRS (Commands e Queries) via MediatR como mediador de handlers, e Domain Events para comunicar efeitos de domínio dentro do próprio módulo ou via Contracts entre módulos.

**Consequências:**
- Casos de uso ficam isolados em Commands/Queries/Handlers, facilitando testes.
- Domain Events permitem reagir a mudanças sem acoplamento direto.
- Exige disciplina para não transformar Domain Events em substituto de chamadas síncronas necessárias.

---

## ADR-0004: Result Pattern em vez de exceções para fluxos de negócio esperados

**Status:** Aceito

**Contexto:** Uso indiscriminado de exceções para controle de fluxo de negócio (ex.: validação, regra violada) torna o código difícil de ler e prejudica performance.

**Decisão:** Utilizar Result Pattern para representar sucesso/falha esperada de operações de negócio. Exceções ficam reservadas a erros verdadeiramente excepcionais (ex.: falha de infraestrutura).

**Consequências:**
- Fluxos de erro esperado tornam-se explícitos na assinatura dos métodos.
- Handlers e Controllers tratam falhas de forma uniforme.
- Exige padronização do tipo `Result`/`Result<T>` no SharedKernel.

---

## ADR-0005: Comunicação entre módulos exclusivamente via Contracts

**Status:** Aceito

**Contexto:** Módulos independentes precisam colaborar sem acessar Infrastructure, Repositories, DbContext ou entidades internas de outro módulo, sob risco de recriar um monolito acoplado.

**Decisão:** Toda comunicação entre módulos ocorre exclusivamente através de Contracts expostos em BuildingBlocks, nunca por acesso direto a camadas internas de outro módulo (ver ARCHITECTURE.md §9).

**Consequências:**
- Módulos podem evoluir e até ser extraídos para serviços separados sem quebrar consumidores.
- Contracts tornam-se superfície pública estável, exigindo cuidado ao alterá-los.
- Pode exigir duplicação controlada de DTOs entre módulos para evitar acoplamento.

---

## ADR-0006: Módulo Documentation implementado sobre a estrutura Core/Infrastructure atual, com pontos de extensão não disruptivos para a arquitetura alvo

**Status:** Aceito

**Contexto:** A Sprint A7 exige um sistema de documentação do BlueprintOS (estrutura de documentos, versionamento, changelog, ADRs, geração de documentação técnica/funcional/IA/desenvolvedor, diagramas Mermaid, sincronização/detecção de documentação desatualizada, integração com Git e um ponto de extensão para memória). Por decisão explícita do Product Owner, esta sprint não deve migrar o backend para a estrutura `Modules/` descrita em ARCHITECTURE.md (ainda não adotada por nenhum módulo existente), nem implementar frontend.

**Decisão:** Implementar o módulo `Documentation` seguindo exatamente o padrão já estabelecido pelo módulo `Knowledge` (`BlueprintOS.Core/Documentation/{Contracts,Models}` + `BlueprintOS.Infrastructure/Documentation/...`, registrado via `AddInfrastructure` em `ServiceCollectionExtensions.cs`, com `IOptions<DocumentationOptions>` para as configurações de persistência de ADRs). Como ponto de extensão pensando na futura migração para a arquitetura alvo (`Modules/Documentation/{Domain,Application,Infrastructure,Api}`), todos os contratos foram desenhados como interfaces coesas e de responsabilidade única em `Core.Documentation.Contracts`, sem dependência de tipos concretos de Infrastructure, de forma que possam ser realocados para `Modules/Documentation/Application` e `Modules/Documentation/Domain` sem alteração de assinatura quando a migração ocorrer. A integração com um módulo de Memória genérico foi deixada como ponto de extensão explícito via `IDocumentationMemoryNotifier`, com implementação no-op/log (`NoOpDocumentationMemoryNotifier`), já que hoje o BlueprintOS possui apenas memória específica de negociação (`INegotiationMemory`).

**Consequências:**
- O módulo Documentation fica imediatamente consistente com o restante do backend (mesmo padrão do Knowledge), sem exigir revisão arquitetural adicional nesta sprint.
- A migração futura para `Modules/` (ADR futura, quando ocorrer) poderá mover os arquivos de `Core.Documentation`/`Infrastructure.Documentation` com baixo retrabalho, pois os contratos já são desacoplados de detalhes de Infrastructure.
- A integração com Memória permanece incompleta (apenas no-op/log) até que um módulo de Memória genérico exista — registrado como dívida técnica em `.ai/memory/known_issues.md`.
- A persistência de `DocumentationEntry`, versões e changelog permanece em memória (não durável), adequado ao escopo desta sprint; persistência durável (arquivo ou banco) pode ser tratada em sprint futura sem alterar os contratos públicos.
