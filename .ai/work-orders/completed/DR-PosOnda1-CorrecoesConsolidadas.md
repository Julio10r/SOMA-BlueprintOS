# Work Order — DR-PosOnda1 — Correções Consolidadas do Design Review Pós-Onda 1

## Metadados

- Fase: Estabilização pós-Onda 1 / pré-Gate Manual Final
- Sprint: Frente única de correções consolidadas (DR.1–DR.8)
- Status: Concluída Tecnicamente — Gate Manual Final pendente do Product Owner
- Responsável: Equipe +Compras (Claude Code)
- Prioridade: Alta — bloqueia o Gate Manual Final do Product Owner
- Dependências: `docs/architecture/Design-Review-Consolidado-Pos-Onda1.md` (aprovado, commit `1d144a9`); B2.9 concluída tecnicamente
- Data de início: 17/08/2026

## Objetivo

Executar de forma consolidada as correções decorrentes do Design Review Pós-Onda 1, deixando o +Compras tecnicamente estabilizado e pronto para o Gate Manual Final do Product Owner. Esta Work Order NÃO abre Onda 2 nem B3, e NÃO substitui o CRUD/E2E manual definitivo do PO.

## Contexto

Ler `docs/architecture/Design-Review-Consolidado-Pos-Onda1.md` (auditoria completa, matriz DR-01..DR-22, lotes propostos DR.1–DR.8) e `docs/audits/DesignReview-Pos-Onda1-Auditoria-SOMA-vs-Compras.md` antes de cada lote. Itens bloqueantes de Gate confirmados: DR-06 (responsividade 1024×768), DR-10 (Review de Fornecedor sem dados), DR-18 (exclusão física de Fornecedor).

## Escopo — Checklist interno

- [x] DR.1 — Estrutura de Navegação (sidebar por contexto, Governança de Compras, Configurações real, RBAC preservado, 1024×768)
- [x] DR.2 — Header e Estados (identidade `[avatar] Nome ▾`, remoção de texto de dev, estados visuais padronizados)
- [x] DR.3 — Padronização Administrativa (títulos/breadcrumb/busca/filtro/selects nas 13 telas administrativas)
- [x] DR.4 — Fornecedores/Review (DR-10 sem dados, rótulos, acessibilidade NovoFornecedorPanel, StatusBadge)
- [x] DR.5 — PT-BR / Glossário / Acessibilidade pragmática
- [x] DR.6 — Persistência / Remoção Física de Fornecedor (DR-18 — usa `Fornecedor.AlterarStatus`, ZERO DELETE funcional)
- [x] DR.7 — Verificações Pendentes (Feature Flags, Notificações, Indicadores, Agentes IA, RBAC visual, modais, responsividade final)
- [x] DR.8 — Regressão Consolidada (build+testes backend/frontend, smoke E2E Chrome)

## Fora do escopo

Onda 2, B3, novas funcionalidades, CRUD/E2E manual definitivo do PO, validação Visual Linx definitiva, Gate Manual Final (execução e assinatura pertencem ao PO).

## Arquitetura

Sem novo Design System — reutilização exclusiva de `resources/design-system/` (AZZAS 2154/GDT), especialmente `ui_kits/portal-gdt/shell.jsx` (UserChip) e catálogo `preview/*.html`. Fornecedor usa mecanismo de status já existente no domínio (`Fornecedor.Status` / `AlterarStatus`) — não inventar soft delete novo.

## Critérios de aceite

- [x] DR.1–DR.5 e DR.7 concluídos (sem lacunas bloqueantes residuais)
- [x] DR.6 resolvida (não dependeu de decisão do PO — mecanismo de status já existia no domínio)
- [x] DR.8 verde: `dotnet build`, `dotnet test` (838/838), `dotnet ef migrations has-pending-model-changes` (sem pendências), `npx tsc -b`, `npm test -- --run` (145/145), `npm run build`
- [x] P0 = 0; nenhum P1 bloqueador aberto (DR-06, DR-10, DR-18 resolvidos)
- [x] Responsividade 1024×768 corrigida sem `overflow-x:hidden` disfarçando a causa (scroll interno nas tabelas)
- [x] Smoke E2E Chrome satisfatório, console/network sem regressões introduzidas por esta frente
- [x] Nenhum push realizado; commits locais granulares (8 commits, um por lote)

## Plano de implementação

1. Reconhecimento das fontes obrigatórias e do código real (concluído).
2. Executar DR.1 a DR.8 em sequência com checkpoint de testes/TS/build após cada lote.
3. DR.6 requer investigação de domínio antes de qualquer alteração (feito: `Fornecedor.Status`/`AlterarStatus` já existem).
4. Regressão consolidada final (DR.8) e smoke Chrome.
5. Relatório final consolidado ao Product Owner, sem push e sem declarar homologação.

## Relatório final

Todos os 8 lotes (DR.1–DR.8) foram executados com checkpoint (tsc/testes/build) após cada um, em 8 commits granulares locais (nenhum push realizado). Os três itens P1 bloqueadores de Gate identificados no Design Review foram resolvidos:

- **DR-06** (responsividade 1024×768): causa raiz era a ausência de contêiner de scroll permanente nas tabelas administrativas (`.divergence-table` só tinha `overflow-x:auto` abaixo de 860px). Corrigido com um wrapper `.table-scroll` estrutural em 17 arquivos — a página nunca extrapola a viewport, apenas a tabela rola internamente quando necessário.
- **DR-10** (Review de Fornecedor sem dados): quando o fornecedor já existe localmente e a reconsulta externa falha, a tela deixava de mostrar qualquer dado enquanto exibia o painel de decisão. Corrigido com `ExistingSupplierSnapshot`, que exibe os dados já cadastrados nesse cenário; Aceitar/Rejeitar permanecem desabilitados.
- **DR-18** (exclusão física de Fornecedor): `ExcluirFornecedorUseCase` fazia `DbSet.Remove` real. Corrigido reaproveitando o mecanismo `Fornecedor.Status`/`AlterarStatus` já existente no domínio (mesmo usado pela sincronização ERP) — nenhum soft-delete novo foi inventado. Classe renomeada para `InativarFornecedorUseCase`/`IInativarFornecedorUseCase` por honestidade do código; a rota HTTP `DELETE /fornecedores/{id}` permanece inalterada.

Nenhuma decisão do Product Owner foi necessária — o mecanismo de inativação de Fornecedor já existia comprovadamente no domínio (schema + uso real em outro use case), então DR.6 não ficou bloqueada.

Smoke E2E via Chrome (dois rounds — DR.7 e DR.8) validou: login real, shell/navegação agrupada, header/UserMenu, RBAC visual (perfil restrito confirmado via UI e via 403 real do backend), Feature Flags/Notificações/Indicadores/Agentes IA (estado real de cada um documentado sem inventar implementação), modais de confirmação (Escape corrigido), responsividade 1024×768/1440×900, e o fluxo completo de consulta CNPJ (novo e existente) sem persistência indevida. Nenhum fornecedor sintético foi criado no ERP. Console/Network sem erros em nenhuma das duas rodadas.

Regressão final: backend `dotnet build` (0 erros), `dotnet test` 838/838 (13 integração + 825 unitários), `dotnet ef migrations has-pending-model-changes` sem pendências; frontend `npx tsc -b` limpo, `npm test -- --run` 145/145, `npm run build` bem-sucedido.

`.ai/dashboard/DASHBOARD_STATE.md` permaneceu como alteração local pré-existente, não tocada por nenhum commit desta frente. Validação Visual Linx e homologação manual definitiva permanecem para o Gate Manual Final do Product Owner.

**Veredito:** CORREÇÕES PÓS-ONDA 1 CONCLUÍDAS — PRONTO PARA GATE MANUAL FINAL.
