# Work Order — DR-PosOnda1 — Correções Consolidadas do Design Review Pós-Onda 1

## Metadados

- Fase: Estabilização pós-Onda 1 / pré-Gate Manual Final
- Sprint: Frente única de correções consolidadas (DR.1–DR.8)
- Status: Em Desenvolvimento
- Responsável: Equipe +Compras (Claude Code)
- Prioridade: Alta — bloqueia o Gate Manual Final do Product Owner
- Dependências: `docs/architecture/Design-Review-Consolidado-Pos-Onda1.md` (aprovado, commit `1d144a9`); B2.9 concluída tecnicamente
- Data de início: 17/08/2026

## Objetivo

Executar de forma consolidada as correções decorrentes do Design Review Pós-Onda 1, deixando o +Compras tecnicamente estabilizado e pronto para o Gate Manual Final do Product Owner. Esta Work Order NÃO abre Onda 2 nem B3, e NÃO substitui o CRUD/E2E manual definitivo do PO.

## Contexto

Ler `docs/architecture/Design-Review-Consolidado-Pos-Onda1.md` (auditoria completa, matriz DR-01..DR-22, lotes propostos DR.1–DR.8) e `docs/audits/DesignReview-Pos-Onda1-Auditoria-SOMA-vs-Compras.md` antes de cada lote. Itens bloqueantes de Gate confirmados: DR-06 (responsividade 1024×768), DR-10 (Review de Fornecedor sem dados), DR-18 (exclusão física de Fornecedor).

## Escopo — Checklist interno

- [ ] DR.1 — Estrutura de Navegação (sidebar por contexto, Governança de Compras, Configurações real, RBAC preservado, 1024×768)
- [ ] DR.2 — Header e Estados (identidade `[avatar] Nome ▾`, remoção de texto de dev, estados visuais padronizados)
- [ ] DR.3 — Padronização Administrativa (títulos/breadcrumb/busca/filtro/selects nas 13 telas administrativas)
- [ ] DR.4 — Fornecedores/Review (DR-10 sem dados, rótulos, acessibilidade NovoFornecedorPanel, StatusBadge)
- [ ] DR.5 — PT-BR / Glossário / Acessibilidade pragmática
- [ ] DR.6 — Persistência / Remoção Física de Fornecedor (DR-18 — usar `Fornecedor.AlterarStatus`, ZERO DELETE funcional)
- [ ] DR.7 — Verificações Pendentes (Feature Flags, Notificações, Indicadores, Agentes IA, RBAC visual, modais, responsividade final)
- [ ] DR.8 — Regressão Consolidada (build+testes backend/frontend, smoke E2E Chrome)

## Fora do escopo

Onda 2, B3, novas funcionalidades, CRUD/E2E manual definitivo do PO, validação Visual Linx definitiva, Gate Manual Final (execução e assinatura pertencem ao PO).

## Arquitetura

Sem novo Design System — reutilização exclusiva de `resources/design-system/` (AZZAS 2154/GDT), especialmente `ui_kits/portal-gdt/shell.jsx` (UserChip) e catálogo `preview/*.html`. Fornecedor usa mecanismo de status já existente no domínio (`Fornecedor.Status` / `AlterarStatus`) — não inventar soft delete novo.

## Critérios de aceite

- [ ] DR.1–DR.5 e DR.7 concluídos (ou lacunas residuais não bloqueantes registradas)
- [ ] DR.6 resolvida ou bloqueada por decisão explícita do PO (registrada, sem impedir os demais lotes)
- [ ] DR.8 verde: `dotnet build`, `dotnet test`, `dotnet ef migrations has-pending-model-changes`, `npx tsc -b`, `npm test -- --run`, `npm run build`
- [ ] P0 = 0; nenhum P1 bloqueador aberto
- [ ] Responsividade 1024×768 corrigida sem `overflow-x:hidden` disfarçando a causa
- [ ] Smoke E2E Chrome satisfatório, console/network sem regressões introduzidas por esta frente
- [ ] Nenhum push realizado; commits locais granulares

## Plano de implementação

1. Reconhecimento das fontes obrigatórias e do código real (concluído).
2. Executar DR.1 a DR.8 em sequência com checkpoint de testes/TS/build após cada lote.
3. DR.6 requer investigação de domínio antes de qualquer alteração (feito: `Fornecedor.Status`/`AlterarStatus` já existem).
4. Regressão consolidada final (DR.8) e smoke Chrome.
5. Relatório final consolidado ao Product Owner, sem push e sem declarar homologação.

## Relatório final

(a preencher ao final da execução)
