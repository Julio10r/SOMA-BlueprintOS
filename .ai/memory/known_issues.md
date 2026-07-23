# known_issues.md

> Log de dívidas técnicas e problemas conhecidos do BlueprintOS, atualizado ao final de cada sprint (ver WORKFLOW.md §14).

---

## Sprint A7 — Sistema de Documentação do BlueprintOS

- **Frontend React ainda não inicializado.** O projeto Web (React/TypeScript) previsto em PROJECT.md/ARCHITECTURE.md ainda não foi criado; toda entrega até aqui, incluindo a Sprint A7, é exclusivamente backend.
- **Biblioteca UI baseada no GDT será implementada na Sprint A8.** A tradução do Global Design Tokens (GDT) para componentes React está fora do escopo desta sprint e planejada para a Sprint A8.
- **Migração completa da arquitetura Core/Infrastructure para a arquitetura alvo será realizada em sprint futura.** O backend ainda segue o padrão `Core/{Módulo}/Contracts,Models` + `Infrastructure/{Módulo}/...`, e não a estrutura `Modules/{Domain,Application,Infrastructure,Api}` definida em ARCHITECTURE.md. A migração para a estrutura alvo (incluindo o módulo `Documentation` criado nesta sprint) fica registrada para uma sprint futura (ver ADR-0006 em `.ai/DECISIONS.md`).


## Sprint A8 — Portal de Documentação Viva

- **KPIs, FAQ e runbook operacional ainda estão vazios/mínimos.** O portal de documentação viva gera esses documentos de forma honesta (sem dados fabricados), mas eles permanecem sparse até que existam fontes reais (uso em produção, suporte ao cliente, incidentes reais).
- **Nenhum `DbContext`/schema de banco de dados existe ainda**, portanto `docs/engineering/database.md` reflete apenas essa ausência.
- **Atualização de `.ai/ROADMAP.md` e `.ai/memory/completed_sprints.md` via `DocumentationPublishService` é idempotente mas ainda manual** (sem acionamento automático por um motor de Workflow, que ainda não existe).