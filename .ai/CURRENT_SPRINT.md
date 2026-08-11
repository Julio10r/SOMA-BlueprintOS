# Sprint de Infraestrutura — Remoção do Docker e Consolidação do Ambiente Local

Status:
Concluída e encerrada em 03/08/2026, com auditoria final aprovada.

Objetivo:
Remover o Docker do fluxo de desenvolvimento do BlueprintOS/+Compras e consolidar o ambiente local (sem containers) como ambiente oficial de desenvolvimento, mantendo a documentação de engenharia consistente com essa decisão.

Entregas:

- `Makefile`, `backend/src/BlueprintOS.Api/Dockerfile` e `infrastructure/docker/docker-compose.yml` removidos do repositório (commits `601d937`, `7bf3bf4`).
- Dependência opcional de SQL Server local em Docker removida antes da remoção completa (commit `601d937`).
- Scripts de desenvolvimento local (`start-dev.sh`, `stop-dev.sh`, `health-check.sh`) confirmados como caminho oficial para subir/parar/verificar backend e frontend.
- `frontend/web/.env.example` atualizado para apontar por padrão para `http://localhost:5262` (API via `dotnet run`), sem referência a Docker.
- `BlueprintOS.UnitTests.csproj` limpo de referências de pacote não utilizadas.
- Documentação de engenharia revisada para remover referências a Docker como ambiente ativo: `docs/Engineering Handbook.md`, `docs/INDEX.md`, `docs/assets/solution-tree.md`, `docs/engineering/Deploy.md`, `docs/engineering/FornecedorErpSynchronization.md`, `.ai/ENGINEERING_BLUEPRINT.md`, `.ai/content/engineering/08-devops.md`.
- ADR-0018 (`.ai/DECISIONS.md`) atualizada para refletir o ambiente local sem Docker.

Validações executadas:

- `dotnet build backend/BlueprintOS.sln`: aprovado, 0 erros e 0 avisos.
- `dotnet test backend/BlueprintOS.sln`: aprovado, 286 testes (281 unitários + 5 integração), 0 falhas, 0 ignorados.
- `npm run build` (`tsc -b && vite build`) em `frontend/web`: aprovado.
- Scripts de desenvolvimento (`start-dev.sh`/`stop-dev.sh`/`health-check.sh`) verificados como funcionais.
- Branch `feature/a13-procurement-vertical-slice` sincronizada com o remoto; working tree limpo antes desta atualização de encerramento.
- Auditoria final de consistência: nenhum resíduo funcional, referência quebrada a Docker ou documento contraditório encontrado.

Resultado:

Docker deixou de ser parte do fluxo de desenvolvimento do projeto. O ambiente oficial de desenvolvimento é local (backend via `dotnet run`, frontend via `npm run dev`/Vite, banco SQL Server corporativo via VPN), sem containers. Nenhuma regra de negócio, contrato de API ou comportamento funcional foi alterado por esta sprint — o escopo foi exclusivamente de infraestrutura e documentação.

Riscos remanescentes:

- Nenhum risco funcional identificado. `infrastructure/docker/` permanece reservado no repositório apenas como diretório documentado (sem `docker-compose.yml`/`Dockerfile` ativos); se não houver uso futuro, sua remoção completa pode ser avaliada em uma sprint futura de limpeza.
- CI/CD e ambiente de homologação continuam não implementados (fora do escopo desta sprint).

---

## Encerramento de sprint

Nenhuma sprint funcional está em andamento. O projeto foi replanejado oficialmente para o MVP 1.0 (estratégia Frontend First, ver `.ai/ROADMAP.md`); a próxima Work Order candidata é a **Onda 1 — Fundação Funcional** (frontend navegável + Administração + blueprint completo do banco), e depende de aprovação explícita do Product Owner (ver `.ai/PROJECT_STATE.md` e `.ai/BACKLOG.md`).

O histórico completo da sprint funcional anterior (B2.1.3 — Endurecimento da Integração ERP de Fornecedores, concluída em 02/08/2026) está arquivado em `.ai/memory/completed_sprints.md`.

---

## Fase atual

**Revisão Arquitetural concluída.** A Revisão Arquitetural R1 (R1.1 + R1.2) e a Revisão Arquitetural R2 (Arquitetura Frontend) da Onda 1 foram concluídas. **R2 concluída**: arquitetura Vertical Slice aprovada como padrão obrigatório do frontend do +Compras, decisões consolidadas na ADR-0020 (`.ai/DECISIONS.md`, seção "Arquitetura Frontend"). Reconciliação documental encerrada (`.ai/PROJECT_STATE.md`, `docs/architecture/domain-principles.md`, `.ai/ENGINEERING_BLUEPRINT.md`, `docs/product/ComprasFuncional.md`, `docs/product/ComprasUX.md`, `docs/product/ComprasDataModel.md`).

## Próxima etapa

**A Sprint O1.2 está oficialmente autorizada para implementação**, sob o padrão arquitetural Vertical Slice registrado na ADR-0020. Permanecem como pendências a resolver durante a O1.2 (não bloqueantes para o início): conteúdo do catálogo de Perfis/Permissões, provedor de e-mail transacional para o OTP, processo de acionamento do Agente Engenheiro de Segurança Sênior, e demais itens registrados em `.ai/PROJECT_STATE.md` e na seção "Dúvidas de produto" de `docs/product/ComprasFuncional.md`.

---

## Sprint O1.2.1 — Fundação Física do Frontend Vertical Slice

Status:
✅ Concluída — encerrada oficialmente em 06/08/2026, com todas as validações aprovadas (estrutural, sem alteração de comportamento funcional). Work Order registrada em `.ai/work-orders/completed/O1.2.1-FundacaoFisicaFrontendVerticalSlice.md`.

Objetivo:
Criar o padrão físico oficial do frontend (Vertical Slice, ADR-0020) e migrar o módulo Fornecedores para esse padrão, como referência para os próximos módulos.

Entregas:

- Criadas as pastas físicas `frontend/web/src/core/` (AppShell, AppRoutes — infraestrutura transversal) e `frontend/web/src/shared/components/` (StatusBadge — único componente hoje usado por mais de um domínio).
- Módulo Fornecedores migrado integralmente para `frontend/web/src/procurement/suppliers/`, organizado em `pages/`, `components/`, `services/`, `types/`, `tests/` (sem pasta `hooks/`, pois não existe hook customizado próprio hoje — não criado artificialmente).
- Eliminadas as pastas horizontais de topo `frontend/web/src/pages/Fornecedores/` e os arquivos de Fornecedores em `frontend/web/src/components/` (sem duplicação remanescente).
- `Dashboard` e `Pedidos` (fora do escopo desta sprint, não migrados) tiveram apenas os imports corrigidos para os novos caminhos (`SupplierCard`, `Fornecedor`, `listSuppliers` em `procurement/suppliers/*`; `StatusBadge` em `shared/components/`).
- Nenhuma regra de negócio, contrato de API, endpoint, comportamento de consulta CNPJ, enriquecimento, aprovação/rejeição, proteção de `NomeFantasia` ou integração ERP foi alterada.

Validações executadas:

- `npx tsc -b`: aprovado, 0 erros.
- `npm run build` (`tsc -b && vite build`) em `frontend/web`: aprovado.
- `npm run test` (Vitest): aprovado, 4/4 testes de `CadastroFornecedor` passando sem alteração de asserções.
- Smoke test do servidor de desenvolvimento (`npm run dev`): rota `/` carrega o AppShell/SPA normalmente; rota `/fornecedores` resolve os módulos JS corretamente (nenhum erro de import); o erro 500 observado nessa rota é o proxy do Vite tentando alcançar a API em `127.0.0.1:8080` (backend não estava rodando durante o teste) — comportamento pré-existente, não introduzido por esta sprint.

Pendências registradas (fora do escopo desta sprint, para sprints futuras de estrutura):

- Demais páginas demonstrativas (`Dashboard`, `Pedidos`, `Negociacoes`, `Indicadores`, `AgentesIA`, `Configuracoes`) permanecem na pasta horizontal `pages/` — ainda não migradas para suas respectivas slices de domínio; ADR-0020 as proíbe como estrutura final, mas migrá-las está fora do escopo desta sprint (que tratou apenas da fundação + Fornecedores como referência).
- Pasta `design-system/` prevista na estrutura conceitual da ADR-0020 não foi criada: hoje o Design System AZZAS 2154/GDT é apenas um `@import` de CSS cross-repo (`resources/design-system/`) em `styles.css`, sem camada de componentes dentro do frontend. Não há código real para mover para essa pasta nesta sprint — criá-la vazia violaria a regra de não criar estrutura sem necessidade real.
- Divergência de porta entre `.env.example` (`5262`) e o proxy `/fornecedores` em `vite.config.ts` (`8080`) identificada na auditoria; não corrigida por estar fora do escopo estrutural desta sprint.
- Sem aliases de import configurados (`tsconfig.json`/`vite.config.ts`); todos os imports da migração usam caminhos relativos. Avaliar introdução de `paths`/`resolve.alias` em sprint futura para reduzir fragilidade.
- `SupplierCard` permanece em `procurement/suppliers/components/`, e o `Dashboard` passou a depender publicamente dessa slice (import entre domínios) — decisão consciente da auditoria, não uma violação: `SupplierCard` é acoplado ao tipo `Fornecedor` e não é genérico o suficiente para `shared/`.

## Próxima etapa

Resumo executivo do encerramento: Vertical Slice implantado, módulo Fornecedores migrado, template oficial do frontend criado, build aprovado, testes aprovados.

**A próxima sprint é a O1.3.1**, que deve aplicar o padrão consolidado nesta sprint aos demais módulos/domínios ainda pendentes (ver lista de pendências acima) e às telas de Administração previstas na Onda 1. Abertura formal da O1.3.1 depende de autorização explícita, seguindo o mesmo fluxo desta sprint.

O comando `[atualizar dashboard]`, quando executado, deverá refletir a conclusão da Sprint O1.2.1.

---

## Nota de execução estrutural (06/08/2026) — migração incremental pós O1.2.1

Sem abertura formal de nova Work Order, foi executada uma continuação puramente estrutural do padrão Vertical Slice (nenhuma regra de negócio, UX, contrato de API ou tela nova alterada):

- `Pedidos` migrado de `pages/Pedidos/PedidosPage.tsx` para `procurement/orders/pages/PedidosPage.tsx`.
- `Dashboard` migrado de `pages/Dashboard/Dashboard.tsx` para `analytics/pages/Dashboard.tsx`.
- `Indicadores` migrado de `pages/Indicadores/IndicadoresPage.tsx` para `analytics/pages/IndicadoresPage.tsx`.
- `Agentes IA` migrado de `pages/AgentesIA/AgentesIAPage.tsx` para `ai/pages/AgentesIAPage.tsx`.
- Esqueleto de pastas (sem código) criado em `administration/{users,profiles,branches,cost-centers,allocation-units}/{pages,components,services,types,tests}`, com `.gitkeep`, preparando os domínios previstos na ADR-0020 sem antecipar implementação.
- `Negociacoes` e `Configuracoes` permanecem intencionalmente em `pages/` — fora do escopo desta execução.
- `npx tsc -b`, `npm run build` e `npm run test` (Vitest) aprovados após a migração, sem alteração de asserções.

Pendência remanescente reafirmada: `Negociacoes`, `Configuracoes` e a pasta `design-system/` continuam não migradas/criadas, aguardando decisão explícita de escopo em sprint futura.

---

## Sprint O1.3.1 (em andamento) — Fundação funcional do módulo Gestão de Perfis

Status:
Em andamento. Escopo desta etapa: fundação visual (mockada, sem backend/autenticação/persistência) do primeiro módulo funcional da Onda 1, `administration/profiles`, base do modelo RBAC exclusivo por perfil aprovado na ADR-0020 (item 8).

Entregas desta etapa:

- Vertical Slice `administration/profiles` implementada com `pages/` (`PerfisPage`, `PerfilFormPage`, `PerfilDetalhesPage`), `components/` (`PerfilTable`, `PerfilForm`, `PermissoesResumo`, `ConfirmExclusaoModal`), `hooks/` (`usePerfis`), `services/` (`perfisMockApi` com CRUD em memória e latência simulada; `permissionCatalog` com catálogo estático de permissões), `types/` (`perfilTypes`) e `tests/` (`PerfisPage.test.tsx`). Nenhuma pasta vazia criada; `routes/` contém `PerfisRoutes.tsx` para a navegação aninhada do módulo.
- Fluxos visuais implementados com dados mockados (sem API real): listagem de perfis, cadastro/edição, visualização somente leitura de permissões agrupadas por recurso, e exclusão com fluxo de confirmação (bloqueado visualmente quando o perfil possui usuários vinculados).
- Regra RBAC refletida na interface: o formulário de perfil exibe aviso explícito de que permissões pertencem exclusivamente ao perfil (nunca ao usuário individualmente) e que um usuário pode ter múltiplos perfis simultaneamente; não existe nenhum campo de permissão individual de usuário na tela.
- Rota `/administracao/perfis` (e sub-rotas `novo`, `:id`, `:id/editar`) registrada em `core/AppRoutes.tsx`; item de navegação "Perfis" adicionado a `core/AppShell.tsx`.
- Design System: nenhum componente visual novo introduzido; reaproveitadas as classes já existentes em `styles.css` (`card`, `page-stack`, `divergence-table`, `data-grid`, `notice`, `btn`, `badge`, `status-*`). Duas variantes de status (`status-ativo`/`status-inativo`) e um `modal-overlay`/`modal-card` mínimo (reaproveitando tokens de espaçamento/cor existentes) foram adicionados a `styles.css` para suportar o fluxo de exclusão e o status Ativo/Inativo do perfil — sem novo componente, apenas novas classes utilitárias sobre os tokens já aprovados.

Validações executadas:

- `npx tsc -b`: aprovado, 0 erros.
- `npm run build` (`tsc -b && vite build`): aprovado.
- `npm run test` (Vitest): aprovado, 7/7 testes (4 de `CadastroFornecedor` inalterados + 3 novos de `PerfisPage`).
- Smoke test do `npm run dev`: `/` e `/administracao/perfis` respondem com o shell da SPA sem erro.

Pendências (fora do escopo desta etapa, não bloqueantes):

- Conteúdo definitivo do catálogo de Perfis/Permissões ainda é pendência de produto (registrada em `PROJECT_STATE.md`); o catálogo usado aqui é um recorte inicial cobrindo os domínios já funcionais/planejados.
- Sem integração com API real, autenticação ou persistência — conforme escopo explícito desta etapa.
- Demais domínios de Administração (`users`, `branches`, `cost-centers`, `allocation-units`) permanecem como esqueleto vazio, aguardando suas próprias etapas.
- Sprint não encerrada; encerramento formal depende de nova autorização explícita.

---

## Sprint O1.3.3 — Fundação funcional do módulo Gestão de Filiais

Status:
✅ Concluída e encerrada em 06/08/2026. Escopo desta etapa: fundação visual (mockada, sem backend/ERP real/persistência) do módulo `administration/branches`, implementando a regra de cadastro integrado do ERP aprovada na ADR-0020 (item 3): Filial é dado mestre do ERP, nunca criada ou alterada no +Compras, apenas ativada/inativada localmente, com metadados locais opcionais.

Entregas desta etapa:

- Vertical Slice `administration/branches` implementada com `pages/` (`FiliaisPage`, `FilialDetalhesPage`, `FilialEditarPage`), `components/` (`FilialTable`, `FilialForm`), `hooks/` (`useFiliais`), `services/` (`filiaisMockApi` com dados mockados em memória e latência simulada, apenas leitura + atualização de metadados locais — sem `create`/`delete`), `types/` (`filialTypes`), `routes/` (`FiliaisRoutes`) e `tests/` (`FiliaisPage.test.tsx`), seguindo exatamente o padrão físico de `administration/profiles`/`administration/users`. Nenhuma pasta vazia criada.
- Catálogo mockado com 8 filiais realistas, cobrindo os três cenários exigidos: filial ativa sem Descrição +Compras (`SOMA MATRIZ SAO PAULO`), filial ativa com Descrição +Compras (`ANIMALE LOJA JARDINS`, `FABULA LOJA VILLAGE MALL`, `FARM LOJA OSCAR FREIRE`) e filial inativa no +Compras (`FARM CD GUARULHOS`, `ANIMALE CD EXTREMA`).
- Regra de cadastro integrado refletida na interface: não existe botão "Nova Filial"/"Criar" em nenhuma tela (verificado por teste); a listagem sempre exibe as três colunas exigidas — Código CliFor, Nome CliFor/Descrição ERP, Descrição +Compras — nunca ocultando a descrição oficial do ERP; a tela de edição separa visualmente "Dados do ERP (somente leitura)" (Código CliFor, Nome CliFor, Unidade de Negócio) de "Dados +Compras (editáveis)" (Descrição +Compras, Ativo no +Compras), com aviso explícito de que "os dados de origem do ERP são somente leitura" e que alterações no +Compras não modificam o ERP.
- Operação de status implementada como "Ativar no +Compras"/"Inativar no +Compras" — diretamente na listagem (sem abrir o formulário) e também dentro do formulário de edição; não existe nenhuma operação de "Excluir Filial" em nenhuma tela.
- Rota `/administracao/filiais` (e sub-rotas `:id`, `:id/editar` — sem rota `novo`, propositalmente) registrada em `core/AppRoutes.tsx`; item de navegação "Filiais" adicionado a `core/AppShell.tsx`.
- Design System: nenhum componente ou classe CSS nova introduzida; reaproveitadas integralmente as classes já existentes em `styles.css` (`card`, `page-stack`, `divergence-table`, `data-grid`, `field-readonly`, `notice`/`notice-warn`, `btn`, `input-row`, `status-ativo`/`status-inativo` via `StatusBadge`), na mesma linha do que `administration/profiles` e `administration/users` já usam.

Validações executadas:

- `npx tsc -b`: aprovado, 0 erros.
- `npm run build` (`tsc -b && vite build`): aprovado.
- `npm run test` (Vitest): aprovado, 17/17 testes (4 de `CadastroFornecedor` + 3 de `PerfisPage` + 3 de `UsuariosPage`, todos inalterados, + 7 novos de `FiliaisPage`, cobrindo listagem, somente leitura dos dados do ERP, edição da Descrição +Compras, ativação/inativação local pela listagem, ausência de botão de criar/excluir, filtro por status e pesquisa por Código CliFor).
- Smoke test real do `npm run dev`: `/` e `/administracao/filiais` respondem HTTP 200; os módulos JS da nova slice (`FiliaisRoutes.tsx`, `FiliaisPage.tsx`, `FilialDetalhesPage.tsx`, `FilialEditarPage.tsx`) resolvem via Vite sem erro de import. Confirmado também, por análise estática, que a rota `/administracao/filiais/*` está registrada em `AppRoutes.tsx` apontando para `FiliaisRoutes` e que o item "Filiais" em `AppShell.tsx` aponta para `/administracao/filiais`.

Pendências (fora do escopo desta etapa, não bloqueantes):

- Sem integração com API real, ERP ou persistência — conforme escopo explícito desta etapa e regra de dado mestre do ERP (ADR-0020, item 2/3).
- Demais domínios de Administração ainda pendentes (`cost-centers`, `allocation-units`) permanecem como esqueleto vazio, aguardando suas próprias etapas.
- Oportunidades de refatoração identificadas (não implementadas nesta etapa): `administration/profiles`, `administration/users` e `administration/branches` repetem o mesmo layout de página (`page-header` + `card` com `card-heading`), o mesmo padrão de hook `use<Entidade>` (estado `loading`/`error`/`reload` idêntico), a mesma estrutura de tabela (`divergence-table` com coluna de `Ações` renderizando botões `btn-secondary`) e o mesmo padrão de mock service (array em memória + `delay`). Avaliar, em sprint futura de estrutura, extrair esses padrões para `administration/shared` (não criado nesta etapa, por regra explícita) somente quando um quarto módulo (`cost-centers`, `allocation-units`) reforçar a duplicação.

Encerramento:
Sprint aprovada pelo Product Owner e encerrada oficialmente em 06/08/2026. Work Order movida para `completed/` com critérios de aceite, build, testes e smoke test aprovados (ver `.ai/work-orders/completed/O1.3.3-GestaoDeFiliais.md`).

---

## Sprint O1.3.4 — Fundação funcional do módulo Gestão de Centros de Custo

Status:
✅ Concluída e encerrada em 06/08/2026. Escopo desta etapa: fundação visual (mockada, sem backend/ERP real/persistência) do módulo `administration/cost-centers`, aplicando a mesma regra de cadastro integrado do ERP (ADR-0020, item 3) já usada em `administration/branches`: Centro de Custo é dado mestre do ERP, nunca criado, editado ou excluído no +Compras — apenas ativado/inativado localmente, com Descrição +Compras opcional. Prepara também o relacionamento (ainda não implementado) com Unidade de Alocação (ADR-0020, item 5), exibindo Unidade de Alocação padrão e quantidade de vínculos com dados mockados.

Entregas desta etapa:

- Vertical Slice `administration/cost-centers` implementada com `pages/` (`CentrosCustoPage`, `CentroCustoDetalhesPage`, `CentroCustoEditarPage`), `components/` (`CentroCustoTable`, `CentroCustoForm`), `hooks/` (`useCentrosCusto`), `services/` (`centrosCustoMockApi`, apenas leitura + atualização de metadados locais, sem `create`/`delete`), `types/` (`centroCustoTypes`), `routes/` (`CentrosCustoRoutes`) e `tests/` (`CentrosCustoPage.test.tsx`), seguindo o mesmo padrão físico de `administration/branches`.
- Nenhum botão de criação ou exclusão de Centro de Custo em nenhuma tela (dado mestre do ERP); listagem e edição separam dados do ERP (somente leitura) dos dados +Compras (editáveis: Descrição +Compras e Ativo no +Compras).
- Relacionamento com Unidade de Alocação exibido apenas visualmente (coluna "Unidade de Alocação padrão" na listagem e no formulário), com dados mockados — sem implementação real do módulo `allocation-units`.
- Rota `/administracao/centros-custo` (e sub-rotas `:id`, `:id/editar`) registrada em `core/AppRoutes.tsx`; item "Centros de Custo" adicionado ao menu em `core/AppShell.tsx`.
- Design System: nenhum componente novo; classes CSS acrescentadas a `styles.css` (14 linhas) reaproveitando os tokens já existentes (mesma linha de `administration/branches`).

Validações executadas:

- `npm run test` (Vitest): aprovado, 25/25 testes (4 de `CadastroFornecedor` + 3 de `PerfisPage` + 3 de `UsuariosPage` + 7 de `FiliaisPage`, todos inalterados, + 8 novos de `CentrosCustoPage`).
- `npm run build` (`tsc -b && vite build`): aprovado, sem erros.
- Verificação estática: rota `/administracao/centros-custo/*` registrada em `AppRoutes.tsx` apontando para `CentrosCustoRoutes`; item de menu em `AppShell.tsx` apontando para `/administracao/centros-custo`.

Pendências (fora do escopo desta etapa, não bloqueantes):
- Módulo `Unidades de Alocação` ainda não implementado; o relacionamento Centro de Custo × Unidade de Alocação é representado apenas visualmente, com dados mockados.
- Domínio `allocation-units` permanece como esqueleto vazio.
- Sem integração com API real, ERP ou persistência — conforme escopo explícito desta etapa.
- Oportunidade de refatoração reafirmada: `administration/profiles`, `administration/users`, `administration/branches` e `administration/cost-centers` repetem o mesmo padrão de página/hook/tabela/mock service; avaliar extração para `administration/shared` em sprint futura de estrutura.

Encerramento:
Sprint encerrada com base em evidência técnica (build e testes aprovados, código presente no working tree). Work Order registrada em `.ai/work-orders/completed/O1.3.4-GestaoDeCentrosDeCusto.md`.

## Próxima etapa

Resumo executivo do encerramento: `administration/cost-centers` implantado como quarta Vertical Slice de Administração (após `profiles`, `users`, `branches`), reafirmando o padrão físico e a regra de cadastro integrado do ERP.

A próxima sprint candidata é a **O1.3.5**, para o domínio `allocation-units` (Unidades de Alocação), que resolveria formalmente o relacionamento hoje apenas mockado em `cost-centers`. Abertura formal depende de autorização explícita do Product Owner.

---

## Sprint O1.3.5 (06/08/2026) — ✅ concluída — Fundação funcional do módulo Gestão de Unidades de Alocação

Status:
✅ Concluída e encerrada formalmente, com aprovação explícita do Product Owner (06/08/2026, na revisão consolidada do Gate Administrativo que precede a frente de Autenticação da Onda 1). Escopo desta etapa: fundação visual (mockada, sem backend/persistência) do módulo `administration/allocation-units`, aplicando a ADR-0020 (item 4): Unidade de Alocação pertence exclusivamente ao +Compras e nunca é integrada do ERP — ao contrário de Filial e Centro de Custo. Conclui a fundação administrativa do +Compras (quinto e último módulo desta frente de Administração da Onda 1).

Entregas desta etapa:

- Vertical Slice `administration/allocation-units` implementada com `pages/` (`UnidadesAlocacaoPage`, `UnidadeAlocacaoFormPage` reaproveitada para criação e edição, `UnidadeAlocacaoDetalhesPage`), `components/` (`UnidadeAlocacaoTable`, `UnidadeAlocacaoForm`), `hooks/` (`useUnidadesAlocacao`), `services/` (`unidadesAlocacaoMockApi`, com `create`/`update`/`toggleStatus` em memória e latência simulada — sem exclusão física), `types/` (`unidadeAlocacaoTypes`), `routes/` (`UnidadesAlocacaoRoutes`) e `tests/` (`UnidadesAlocacaoPage.test.tsx`, 6 testes), seguindo o mesmo padrão físico dos demais módulos de Administração.
- Diferente de `branches`/`cost-centers` (dados mestres do ERP), Unidade de Alocação é criada e editada integralmente pelo +Compras: existe botão "Nova unidade de alocação" e todos os campos (Nome, Descrição, Unidade de Negócio, Status) são editáveis — não há separação "Dados do ERP" vs. "Dados +Compras", pois não existe origem ERP.
- Dados representados: Nome, Descrição, Unidade de Negócio e Status (Ativo/Inativo), com 5 unidades de alocação mockadas cobrindo os cenários ativo e inativo.
- Funcionalidades implementadas: listagem (com busca e filtro por status), cadastro, edição, visualização e ativação/inativação. Não existe exclusão física em nenhuma tela — apenas inativação, mesmo princípio já aplicado aos demais módulos de Administração.
- Relacionamento N:N com Centro de Custo (ADR-0020, item 5) permanece fora do escopo desta etapa — apenas o cadastro da própria Unidade de Alocação foi implementado; o vínculo com Centro de Custo continua representado apenas visualmente (mockado) em `administration/cost-centers`, sem alteração nesta etapa.
- Rota `/administracao/unidades-alocacao` (e sub-rotas `novo`, `:id`, `:id/editar`) registrada em `core/AppRoutes.tsx`; item de navegação "Unidades de Alocação" adicionado a `core/AppShell.tsx`.
- Design System: nenhum componente ou classe CSS nova introduzida; reaproveitadas integralmente as classes já existentes em `styles.css` (`card`, `page-stack`, `divergence-table`, `notice`/`notice-warn`, `btn`, `input-row`, `data-grid`, `field-readonly`, `form-card`, `status-ativo`/`status-inativo` via `StatusBadge`), na mesma linha dos demais módulos de Administração.

Validações executadas:

- `npx tsc -b`: aprovado, 0 erros.
- `npm run build` (`tsc -b && vite build`): aprovado, sem erros.
- `npm run test` (Vitest): aprovado, 31/31 testes (25 pré-existentes inalterados + 6 novos de `UnidadesAlocacaoPage`, cobrindo listagem, criação, visualização, edição, ativação/inativação pela listagem e ausência de ação de exclusão física).
- Smoke test real em navegador headless (Playwright, `npm run dev`): rotas `/`, `/administracao/perfis`, `/administracao/usuarios`, `/administracao/filiais`, `/administracao/centros-custo` e `/administracao/unidades-alocacao` carregadas com sucesso (HTTP 200, sem erro de import de módulo JS). Em `/administracao/unidades-alocacao`: listagem carrega os dados mockados, botão "Nova unidade de alocação" abre o formulário, "Visualizar" e "Editar" funcionam a partir da listagem, e ativação/inativação pela listagem altera o status corretamente. Único erro de console observado: HTTP 500 do proxy do Vite para a API em `/` (rota `Dashboard`) — comportamento pré-existente e já documentado desde a Sprint O1.2.1 (backend não estava em execução durante o smoke test), não introduzido por esta etapa.

Pendências (fora do escopo desta etapa, não bloqueantes):

- Relacionamento N:N entre Centro de Custo e Unidade de Alocação (ADR-0020, item 5) ainda não implementado de fato — cada lado continua com dados mockados independentes; vincular os dois módulos (por exemplo, permitir escolher a Unidade de Alocação padrão de um Centro de Custo a partir de uma lista real de Unidades de Alocação cadastradas) é candidato a uma sprint futura.
- Sem integração com API real, autenticação ou persistência — conforme escopo explícito desta etapa.
- Oportunidade de refatoração reafirmada: `administration/profiles`, `administration/users`, `administration/branches`, `administration/cost-centers` e agora `administration/allocation-units` repetem o mesmo padrão de página/hook/tabela/mock service; avaliar extração para `administration/shared` permanece candidato a uma sprint futura de estrutura — não implementada aqui, para não antecipar decisão de escopo.
- Divergência de porta entre `.env.example` (`5262`) e o proxy Vite (`8080`), identificada desde O1.2.1, permanece não corrigida.

Encerramento:
Sprint encerrada formalmente em 06/08/2026 com aprovação explícita do Product Owner, com base nas evidências técnicas já registradas acima (build, testes e smoke test aprovados nesta própria etapa — nenhuma nova rodada de validação foi executada no encerramento). Pendências remanescentes não bloqueantes: (1) relacionamento N:N entre Centro de Custo e Unidade de Alocação (ADR-0020, item 5) segue não implementado; (2) Work Order ainda não movida fisicamente para `.ai/work-orders/completed/`; (3) inconsistência identificada na revisão consolidada do Gate Administrativo — `administration/users` possui `deleteUsuario` (exclusão física), divergente do padrão "sem exclusão física" adotado nos demais módulos administrativos, recomendada para avaliação em sprint futura. Nenhum commit ou push foi realizado como parte deste encerramento documental. **Atualização (Housekeeping Administrativo, 06/08/2026): as três pendências acima foram regularizadas — Work Order movida para [O1.3.5-GestaoDeUnidadesDeAlocacao.md](./work-orders/completed/O1.3.5-GestaoDeUnidadesDeAlocacao.md); `deleteUsuario` substituído por ativação/inativação; relacionamento N:N Centro de Custo × Unidade de Alocação permanece pendência aberta para sprint futura.**

## Próxima etapa

Com `administration/allocation-units` implementado e a O1.3.5 formalmente encerrada, a fundação administrativa do +Compras (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação) está completa em nível de fundação visual mockada — Gate Administrativo revisado em 06/08/2026 (ver relatório de review consolidada). Próxima frente candidata: **Autenticação da Onda 1**. As pendências de governança já registradas (Work Order [O1.3.2](./work-orders/completed/O1.3.2-GestaoDeUsuarios.md) retroativa, movimentação da Work Order [O1.3.5](./work-orders/completed/O1.3.5-GestaoDeUnidadesDeAlocacao.md) para `completed/`) foram regularizadas na sprint de Housekeeping Administrativo de 06/08/2026, detalhada a seguir.

---

## Sprint de Housekeeping Administrativo (06/08/2026) — ✅ concluída

Status:
✅ Concluída. Escopo exclusivamente de regularização administrativa, exigido antes da abertura da frente de Autenticação da Onda 1 (O1.4) — sem nova funcionalidade, sem alteração de arquitetura. Nenhum commit ou push foi realizado.

Entregas desta etapa:

- `administration/users`: `deleteUsuario` (exclusão física) substituído por `setStatusUsuario` — fluxo de ativação/inativação, alinhado ao padrão já adotado em Perfis, Filiais, Centros de Custo e Unidades de Alocação. `ConfirmExclusaoUsuarioModal` substituído por `ConfirmToggleAtivoUsuarioModal`; `UsuarioTable`, `UsuariosPage` e `useUsuarios` atualizados. Usuários permanecem auditáveis — nenhum registro é removido da base mockada. Novo teste cobrindo a inativação de um usuário pela listagem.
- Work Order [O1.3.2-GestaoDeUsuarios.md](./work-orders/completed/O1.3.2-GestaoDeUsuarios.md) criada retroativamente, a partir exclusivamente de evidência já existente (código e testes pré-existentes de `UsuariosPage.test.tsx`).
- Work Order [O1.3.5-GestaoDeUnidadesDeAlocacao.md](./work-orders/completed/O1.3.5-GestaoDeUnidadesDeAlocacao.md) extraída desta seção de `CURRENT_SPRINT.md` e movida fisicamente para `.ai/work-orders/completed/`.
- Correções documentais: `.ai/BACKLOG.md` (linha da O1.3.2 ausente da tabela, adicionada; pendências desatualizadas da O1.3.5 corrigidas), `.ai/PROJECT_STATE.md` (pendências de rastreabilidade marcadas como regularizadas).

Validações executadas nesta etapa:

- `npx tsc -b`: aprovado, 0 erros.
- `npm run build` (`tsc -b && vite build`): aprovado.
- `npm run test` (Vitest): aprovado, 32/32 testes (31 pré-existentes + 1 novo em `UsuariosPage.test.tsx`, cobrindo a inativação de usuário pela listagem em substituição à exclusão física).

Pendências restantes (fora do escopo desta sprint, não bloqueantes):

- Relacionamento N:N entre Centro de Custo e Unidade de Alocação (ADR-0020, item 5) ainda não implementado.
- Ausência de Work Order equivalente para O1.3.1 (Perfis) em `.ai/work-orders/completed/`.

---

## Encerramento formal — Gate Administrativo e liberação da O1.4 (06/08/2026)

**Gate Administrativo: APROVADO.** Housekeeping Administrativo concluído (regularizações de `deleteUsuario`→`setStatusUsuario`, Work Orders retroativa/movida, documentação corrigida — ver seção acima). A fundação administrativa da Onda 1 (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação) está completa em nível de fundação visual mockada, sem pendência de governança bloqueante remanescente. Nenhuma decisão estratégica ou de roadmap foi alterada por este encerramento — apenas registro formal de conclusão de housekeeping já executado.

**Frente O1.4 (Autenticação e Segurança) — liberada**, iniciando pela etapa O1.4.1 (Security Design Review), exigida por ADR-0020 (item 13) e `docs/architecture/domain-principles.md` §Segurança antes de qualquer implementação de autenticação.

**O1.4.1 — Security Design Review — ✅ concluída (06/08/2026).** Revisão arquitetural de segurança, threat modeling e definição de controles obrigatórios para Login OTP, Bootstrap Mode e sessão/RBAC, produzida sem nenhuma implementação de código (sem endpoints, sem migrations, sem OTP, sem Bootstrap, sem alteração de frontend/backend). Documento: [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md).

**Security Design Gate: APROVADO COM PENDÊNCIAS.** A arquitetura recomendada (sessão persistida server-side + cookie `HttpOnly/Secure/SameSite=Strict`, OTP com hash/uso único/rate limiting, Bootstrap com segredo de implantação + identidade pré-autorizada + OTP + transação atômica, RBAC exclusivo por Perfil sem cache de longa duração) está pronta para servir de base à Work Order técnica de O1.4.2. A implementação de código (O1.4.2) **não pode iniciar** até o Product Owner resolver os bloqueadores registrados na seção 12 do documento: (1) catálogo de Perfis/Permissões incluindo "Administrador Sênior"; (2) contratação/seleção do provedor transacional de e-mail para OTP; (3) escopo de `Perfil`/`Permissao` (global vs. por Unidade de Negócio). O modelo físico de sessão recomendado (seção 1.2 do documento) também exige ratificação explícita do Product Owner/CTO na Work Order técnica antes da implementação.

Achados adicionais registrados no documento (não bloqueantes para esta revisão, mas requisitos obrigatórios para O1.4.2): ausência atual de headers de segurança HTTP (CSP/HSTS/X-Content-Type-Options/Referrer-Policy/X-Frame-Options/Cache-Control), CORS atual incompatível com cookies de sessão (`AllowAnyHeader`/`AllowAnyMethod` sem isolamento de fallback por ambiente), e recomendação de condicionar o registro de `DevelopmentRequestIdentity` (ADR-0011) no DI ao ambiente `Development` quando o adaptador de produção for implementado.

Nenhum commit ou push foi realizado como parte deste encerramento.
- Divergência de porta entre `.env.example` (`5262`) e o proxy Vite (`8080`), identificada desde O1.2.1, permanece não corrigida.

---

## O1.4.1.1 — Formalização da Estratégia de Autenticação em Development (07/08/2026) — ✅ concluída

**Status:** ✅ concluída. Escopo exclusivamente documental — nenhum código, provider, configuração de SMTP/Microsoft Graph, App Registration ou credencial real foi criado, alterado ou solicitado. Nenhum commit ou push foi realizado.

**Objetivo:** formalizar, antes do início da implementação de O1.4.2, a estratégia de desenvolvimento do fluxo OTP aprovada por uma revisão adversarial dedicada — **Development Auth Strategy: aprovada com ajustes pelo Product Owner** — que permite iniciar O1.4.2 sem dependência imediata da Infra.

**Decisões formalizadas** (detalhe completo em [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md), seção 17, complemento da O1.4.1):

- O +Compras permanece em fase Frontend First; a integração corporativa definitiva de e-mail/Entra ID **não é solicitada à Infra agora** — será preparada antes de Homologação.
- Contrato `IOtpEmailSender` (não um `IEmailSender` genérico) como única abstração conhecida pelo domínio para envio de OTP.
- `DevelopmentOtpEmailSender` estritamente exclusivo de `Development`, sem fallback automático em nenhum outro ambiente; Staging/Homologação/Produção sem provider corporativo válido devem falhar de forma fechada.
- Defesa em profundidade obrigatória para O1.4.2: seleção de provider exclusivamente por `IHostEnvironment`, `ValidateOnStart()` para configuração obrigatória fora de Development, fail-closed antes de aceitar tráfego, e — quando viável — ausência física do Development provider no artefato de Release. Múltiplas checagens de `IsDevelopment()` isoladas **não** contam como defesa em profundidade independente.
- Regra absoluta: OTP nunca aparece em log/API/HTML/frontend/query string/arquivo/telemetria/auditoria, **inclusive em Development** — nenhum prefixo como `[DEV-OTP]` em log. Mecanismo de diagnóstico/teste para recuperar OTP em E2E, se necessário, é exclusivo de Development e será detalhado em O1.4.2.
- Secrets: User Secrets/variáveis de ambiente em Development, secrets da plataforma em CI, secret manager corporativo em Homologação/Produção — nunca versionados.
- **Authentication Infra Readiness Gate** formalizado como gate obrigatório antes de Homologação (distinto do Security Design Gate da O1.4.1), cobrindo provider corporativo, integração real de envio, mailbox, secrets corporativos, indisponibilidade do Development provider, Entra ID/App Registration, URLs/callbacks, headers/CORS/CSRF/rate limiting, secret scanning, testes de segurança, runbook e rotação de credenciais.
- Entra ID/Microsoft Graph **não descartados** — deliberadamente postergados para o preparo de Homologação em conjunto com a Infra.

**Efeito sobre os bloqueadores da O1.4.1 (seção 12 do documento):** o bloqueador nº 2 (provedor transacional de e-mail) deixa de impedir o **início** de O1.4.2 e passa a ser exigido apenas pelo Authentication Infra Readiness Gate, antes de Homologação. Os bloqueadores nº 1, 3 e 4 (catálogo de Perfis/Permissões incl. "Administrador Sênior"; escopo de `Perfil`/`Permissao`; semântica de "nenhum `UsuarioCentroCusto`") **permanecem inalterados** — decisões de Product Owner fora do escopo desta formalização. A ratificação do modelo de sessão (seção 1.2) pelo Product Owner/CTO na Work Order técnica também permanece pendente.

**Situação da O1.4.2:** liberada para implementação **sem dependência imediata da Infra**, com as regras desta seção como critérios de aceite obrigatórios da implementação. Ainda condicionada, para fechamento completo do escopo original, aos bloqueadores nº 1 e 3 remanescentes e à ratificação do modelo de sessão.

**Validação:** nenhuma contradição encontrada com ADR-0020 (item 11 — coexistência de múltiplos Identity Providers) ou com a Security Design Review original (seções 1–16 do documento, não alteradas por esta seção).

**Encerramento formal:** O1.4.1.1 está oficialmente encerrada em 07/08/2026. Nenhuma pendência aberta desta etapa permanece — os itens que ela deliberadamente não resolvia (bloqueadores nº 1, 3 e 4 e ratificação do modelo de sessão) foram resolvidos separadamente pelo Product Owner/CTO na abertura da O1.4.2 (ver seção abaixo), não por esta etapa.

---

## Sprint atual: O1.4.2 — Login Passwordless OTP e Sessão Segura

**Status:** Em desenvolvimento.

**Ratificação de decisões de produto pelo Product Owner/CTO (07/08/2026), na abertura desta sprint** — resolvem os bloqueadores nº 1, 3 e 4 da seção 12 de `security-design-auth-o1.4.md` e a pendência de ratificação do modelo de sessão da seção 1.2 do mesmo documento:

- **Permissões:** catálogo **global** da aplicação (ex.: `PEDIDO.CRIAR`, `PEDIDO.APROVAR`, `PEDIDO.CANCELAR`, `USUARIO.GERENCIAR`, `PERFIL.GERENCIAR`). Sem permissão individual por usuário.
- **Perfis:** configurados por Unidade de Negócio; usuário pode ter 1..N Perfis dentro da BU; permissões efetivas = união dos Perfis; usuário nunca recebe permissão diretamente.
- **Administrador Sênior:** Perfil especial de plataforma, para Bootstrap inicial, Administração do Sistema e funções administrativas globais — modelo preparado nesta sprint, Bootstrap completo **não** implementado agora.
- **Usuário × Centro de Custo:** vínculo com 1..N Centros de Custo ativos, ou acesso a todos os ativos — conceito de escopo operacional, distinto e não misturado com Perfil (autorização funcional).
- **Modelo de sessão:** ratificado — sessão persistida server-side, identificador opaco em cookie `HttpOnly/Secure/SameSite=Strict`, sem JWT stateless como mecanismo principal. Nenhuma informação de identidade/autorização (usuário, perfil, permissões, `UnidadeNegocioId`, e-mail, token, claims) no cookie.

**Objetivo desta sprint:** implementar o primeiro fluxo real de autenticação Passwordless do +Compras em Development — solicitação de OTP, geração segura do desafio, mecanismo de obtenção do OTP exclusivo para testes locais, validação, criação de sessão server-side, cookie seguro, identidade autenticada, logout. Sem Entra ID, sem provider corporativo de e-mail, sem Bootstrap completo.

**Estado ao final desta etapa: implementação concluída, pendente de Security Validation e aprovação do Product Owner antes de ser considerada "Pronta" (ADR-0020, item 13).**

Entregas:

- **Backend** (Vertical Slice `Domain/Identity`, `Application/Identity`, `Infrastructure/Identity`, `Api/Auth` + `Api/Identity`): entidades `UnidadeNegocio`, `Usuario`, `Perfil`, `Permissao`, `PerfilPermissao`, `UsuarioPerfil`, `UsuarioCentroCusto` (modelo preparado, não populado por esta sprint), `CodigoVerificacaoOtp`, `SessaoAutenticacao`; casos de uso `SolicitarOtpUseCase`, `ValidarOtpUseCase`, `LogoutUseCase`, `ObterIdentidadeAtualUseCase`; contrato `IOtpEmailSender`; `DevelopmentOtpEmailSender` (exclusivo de Development) + `UnconfiguredCorporateOtpEmailSender` (fail-closed fora de Development, com `ValidateOnStart()`); endpoints `POST /auth/otp/request`, `POST /auth/otp/verify`, `POST /auth/logout`, `GET /auth/me`; mecanismo de diagnóstico `GET /dev/otp` (exclusivo de Development, restrito a loopback, leitura de uso único, nunca mapeado fora de Development); `SessionCurrentIdentity` como novo adaptador de `ICurrentIdentity` fora de Development (`DevelopmentRequestIdentity`/ADR-0011 permanece exclusivo de Development); rate limiting por IP (`Microsoft.AspNetCore.RateLimiting`) em `/auth/otp/request` e `/auth/otp/verify`; defesa CSRF por header customizado (`X-MaisCompras-Csrf`); headers de segurança (`CSP`, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`, `Cache-Control: no-store` em `/auth`/`/dev`, `HSTS` fora de Development); CORS com `AllowCredentials()` restrito à allowlist de origens; migration EF Core `AddIdentityAuthentication`.
- **Frontend** (Vertical Slice `auth/`): `LoginPage` (etapas e-mail → OTP, reenvio com cooldown de 60s, botão de diagnóstico exclusivo de `import.meta.env.DEV`), `AuthContext`/`AuthProvider`/`useAuth`, `RequireAuth` (guarda de rota), integração em `AppRoutes.tsx` (rota pública `/login`, demais rotas protegidas) e `AppShell.tsx` (botão "Sair"). Nenhum dado de sessão/OTP em `localStorage`/`sessionStorage`. Correção incidental da divergência de porta do proxy Vite (`8080` → `5262`, documentada desde O1.2.1).
- **Testes:** 24 novos testes de backend (xUnit — segurança de OTP/sessão, casos de uso com fakes, seleção de provider por ambiente, cookie, fail-closed) elevando a suíte de 251 para 275 testes, todos aprovados; 6 novos testes de frontend (Vitest — fluxo de login, mensagens genéricas de erro, ausência de dados sensíveis em storage, guarda de rota), elevando a suíte de 32 para 38 testes, todos aprovados.

Validações executadas:

- `dotnet build backend/BlueprintOS.sln`: aprovado, 0 erros, 0 avisos.
- `dotnet test backend/tests/BlueprintOS.UnitTests`: aprovado, 275/275.
- `npx tsc -b`, `npm run build`, `npm run test` (frontend): aprovados, 38/38 testes.
- Smoke test real em navegador (Chromium via Playwright) contra backend e frontend rodando localmente: confirmado redirecionamento para `/login` sem sessão, headers de segurança presentes na resposta, nenhuma chave em `localStorage`/`sessionStorage`, rejeição de requisição sem header CSRF (403), rate limiting ativo (429 na 3ª solicitação em 15 min), e exposição de erro genérico ao usuário mesmo quando o backend falha internamente (nenhum detalhe de exceção/SQL chega à UI).
- **Limitação registrada:** o fluxo completo de sucesso (OTP real → sessão → área autenticada → logout) não pôde ser validado ponta a ponta nesta sessão porque o banco `MaisComprasConnection` compartilhado (corporativo, via VPN) está com o histórico de migrations dessincronizado de um estado anterior não relacionado a esta sprint (`Invalid object name 'Usuarios'` seguido de `There is already an object named 'Fornecedores'` ao tentar aplicar migrations pendentes) — corrigir isso exigiria intervenção em um banco compartilhado fora do escopo desta tarefa e é tratado como pendência separada, não como defeito desta implementação. A lógica completa do fluxo de sucesso está coberta pelos 24 testes de unidade de `Application.Identity`, que exercitam solicitação → validação → criação de sessão → revalidação → logout com repositórios fake.

Pendências (fora do escopo desta sprint, não bloqueantes):

- Bootstrap Mode completo (Administrador Sênior, segredo de implantação, atomicidade) — modelo de dados preparado (`Perfil.AdministradorSenior`), fluxo não implementado.
- Catálogo real de Perfis/Permissões e vínculo `UsuarioCentroCusto` populado — modelo preparado, sem dados/tela.
- Correção do histórico de migrations do banco compartilhado (pendência de infraestrutura de dados, não de código desta sprint).
- Matriz de autorização por permissão nas rotas do frontend (`RequireAuth` cobre apenas presença de sessão).
- Conversão de `ICurrentIdentity.GetRequired()` para assíncrono (hoje `SessionCurrentIdentity` usa `GetAwaiter().GetResult()` sobre o caso de uso assíncrono — documentado como dívida técnica no próprio código).

**Situação:** implementação técnica concluída e validada dentro dos limites acima. Não marcada como "Pronta" — aguarda Security Validation dedicada e aprovação do Product Owner (ADR-0020, item 13), conforme já formalizado na O1.4.1.1.

---

## O1.4.2.1 — Security Hardening da Autenticação OTP (07/08/2026)

**Status:** implementação técnica concluída. **A O1.4.2 permanece não concluída** — esta é uma micro-iteração de hardening sobre a mesma sprint, não um encerramento. Aguarda uma **nova** Security Validation independente antes de qualquer avanço para Bootstrap/Administrador Sênior.

**Contexto:** a Security Validation adversarial da O1.4.2 resultou em **Security Implementation Gate: Aprovado com pendências**, com 4 achados ALTO (A–D) e 1 achado MÉDIO (N, cobertura de testes) a fechar antes de prosseguir. Detalhe completo dos achados e das correções: [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md), seção 18.

**Correções implementadas:**

- **Achado A (rate limiting só por IP):** novo throttle server-side por e-mail normalizado (`OtpRequestThrottle`, EF Core com RowVersion), complementar ao limite por IP já existente — ~3/15min + cooldown de 60s, aplicado identicamente a e-mail existente/inexistente para não criar oráculo de enumeração.
- **Achado B (consumo de OTP sem garantia atômica):** RowVersion em `CodigoVerificacaoOtp` + índice único filtrado (`Status=Pendente`) por usuário — duas validações concorrentes do mesmo código produzem exatamente um sucesso; comprovado por teste de concorrência real (`Task.WhenAll`, múltiplos `DbContext`, não fakes sequenciais).
- **Achado C (autorização opt-in):** `AuthorizationOptions.FallbackPolicy` exige autenticação por padrão em todo endpoint; anônimo passa a ser exceção explícita (`.AllowAnonymous()`) em `/health`, `/auth/otp/request`, `/auth/otp/verify`, `/auth/logout` e `/dev/otp` (Development). Dois novos authentication handlers (`SessionCookieAuthenticationHandler` fora de Development, `DevelopmentHeaderAuthenticationHandler` em Development) publicam a identidade em `HttpContext.User`; `ICurrentIdentity.GetRequired()` permanece como segunda barreira, agora lendo apenas essas claims — o que também elimina a dívida técnica de sync-over-async da O1.4.2 em `SessionCurrentIdentity`.
- **Achado D (/dev/otp sem defesa redundante):** o handler agora verifica `IHostEnvironment.IsDevelopment()` internamente, além do `if` de mapeamento em `Program.cs`; documentado explicitamente que o mecanismo não é suportado via proxy reverso/túnel/rede compartilhada.
- **Achado MÉDIO/N (fail-closed não comprovado por host real):** novos testes iniciam um `IHost` real em Staging/Production sem provider corporativo configurado e comprovam falha de startup (`OptionsValidationException`), não apenas resolução de tipo via DI.
- **Achados F/G/H revisados:** F resolvido por consequência arquitetural do secure-by-default (toda requisição autenticada estende a sessão, não só `/auth/me`); G corrigido com identificador de correlação não reversível (`EmailAuditHasher`) nos logs de autenticação; H postergado com justificativa (depende de decisão de hospedagem da SPA, registrado no Authentication Infra Readiness Gate).

**Testes:** suíte de backend cresceu de 275 para **294 testes** (19 novos: 4 de throttle, 3 de concorrência real, 5 de fail-closed real, 3 do authentication handler de sessão, 4 do hardening de `/dev/otp`), todos aprovados. Suíte de frontend inalterada, 38/38 aprovados (sem regressão). `dotnet build`: 0 erros/0 avisos. Nova migration EF Core `AddOtpHardening`.

**Smoke test real** (backend+frontend locais, sem tocar o banco corporativo compartilhado): confirmado ao vivo — endpoint de negócio sem sessão → 401 (antes dependia só de proteção implícita); `/auth/me` sem sessão → 401; `/auth/otp/request` sem CSRF → 403; rate limiting por IP → 429 após o limite; `/dev/otp` sem código armazenado → 404; `logout` sem sessão → 204 (idempotente); nenhuma chave sensível em `localStorage`/`sessionStorage`. O fluxo de sucesso ponta a ponta com usuário real permanece bloqueado pela mesma dessincronia de migrations do banco compartilhado já registrada na O1.4.2 — ambiental, não corrigida nem contornada aqui.

**Situação:** aguardando nova Security Validation independente. Não avançar para Bootstrap/Administrador Sênior antes dela.

---

## O1.4.2.2 — Hardening do DevelopmentHeaderAuthenticationHandler (07/08/2026)

**Status:** implementação técnica concluída. **A O1.4.2 permanece não concluída.** Correção pontual decorrente da segunda Security Validation independente sobre a O1.4.2.1, que encontrou um novo achado ALTO (Achado E): `DevelopmentHeaderAuthenticationHandler` autenticava qualquer requisição em Development com `X-Development-User-Id` sintaticamente válido, **sem checagem de origem** — diferente de `/dev/otp`, que já exigia loopback desde a mesma iteração de hardening. Como esse handler alimenta a `AuthorizationOptions.FallbackPolicy` (secure-by-default), a lacuna concedia identidade completa sobre qualquer endpoint protegido, não apenas exposição de um OTP.

**Correção:** adicionada exigência de `RemoteIpAddress` estritamente loopback (IPv4/IPv6) no handler, mantendo a checagem de `IHostEnvironment.IsDevelopment()` como barreira independente — mesma defesa já usada em `/dev/otp`, aplicada de forma consistente. Nenhum forwarded header é lido ou honrado; documentado explicitamente que o mecanismo não é suportado via proxy reverso/túnel/rede compartilhada.

**Testes:** suíte de backend cresceu de 294 para **306 testes** (12 novos: 8 do handler isolado incluindo cenário de `X-Forwarded-For` forjando loopback, 4 de pipeline HTTP real via Kestrel/`WebApplication` sem `AddInfrastructure`), todos aprovados. Frontend inalterado, 38/38, sem regressão. `dotnet build`: 0 erros/0 avisos. `git diff --check`: limpo.

**Pendências mantidas em aberto (fora do escopo desta correção pontual, por instrução explícita):** `EmailAuditHasher` sem salt/HMAC (dicionário offline contra e-mails previsíveis) — não bloqueante para Development. Validação de RowVersion+índice único filtrado em provider relacional real — obrigatória antes de Homologação.

**Situação:** aguardando **última** Security Validation independente antes de qualquer avanço para Bootstrap/Administrador Sênior. Não avançar sem ela.

---

## Encerramento formal — O1.4.2 — Login Passwordless OTP e Sessão Segura (07/08/2026) — ✅ CONCLUÍDA

**Status: ✅ CONCLUÍDA.** A última Security Validation independente sobre a O1.4.2 (incluindo as micro-iterações O1.4.2.1 — Security Hardening e O1.4.2.2 — Hardening do `DevelopmentHeaderAuthenticationHandler`) foi realizada. **Security Implementation Gate III: APROVADO COM PENDÊNCIAS NÃO BLOQUEANTES PARA DEVELOPMENT.** Com este Gate, a O1.4.2 satisfaz integralmente a exigência de ADR-0020 (item 13) — revisão de segurança antes (O1.4.1) e validação de segurança dedicada depois (Gates I/II/III) da implementação — e é encerrada formalmente.

**Escopo encerrado, com suas três iterações:**
- **O1.4.2.1 — Security Hardening da Autenticação OTP:** fechamento dos achados ALTO A–D e MÉDIO/N da Security Validation I (rate limiting por e-mail, atomicidade de OTP via RowVersion + índice único filtrado, secure-by-default via `AuthorizationOptions.FallbackPolicy`, hardening de `/dev/otp`, fail-closed comprovado por host real). Ver `docs/architecture/security-design-auth-o1.4.md` §18.
- **O1.4.2.2 — Hardening do `DevelopmentHeaderAuthenticationHandler`:** fechamento do achado ALTO E da Security Validation II (exigência de origem estritamente loopback). Ver `docs/architecture/security-design-auth-o1.4.md` §19.

**Pendências não bloqueantes para Development, explicitamente carregadas para o Authentication Infra Readiness Gate (seção 17.7 de `security-design-auth-o1.4.md`), a resolver antes de Homologação, não antes de continuar a implementação em Development:**
- Validação de `RowVersion` + índice único filtrado (`Status = Pendente`) em provider relacional real (SQL Server ou equivalente isolado) — hoje comprovada apenas via EF Core InMemory (limitação documentada em §18.5) e por leitura de configuração/migration.
- CSP efetiva da SPA — hoje protege apenas as respostas JSON da API; a SPA servida separadamente (Vite/dist) não recebe o header (Achado H, §18.7, postergado).
- `EmailAuditHasher` sem salt/HMAC — avaliação futura de HMAC com chave de aplicação (Achado F da Security Validation II).
- Sincronização de migrations do banco `MaisComprasConnection` compartilhado — dessincronia pré-existente, ambiental, não relacionada ao código desta sprint.
- Provedor corporativo/Entra ID/Microsoft Graph — deliberadamente postergados (O1.4.1.1, §17), exigidos apenas pelo Authentication Infra Readiness Gate.

Nenhuma destas pendências bloqueia o início da próxima etapa (O1.4.3 — Bootstrap/Administrador Sênior) em Development; todas permanecem registradas para resolução antes de Homologação.

**Commit/push:** não realizado nesta atualização documental — apenas encerramento de governança nos arquivos canônicos.

---

## O1.4.3 — Security Design do Bootstrap e Administrador Sênior (07/08/2026)

**Status:** Security Design Review concluída — **nenhuma implementação de código, migration, endpoint ou alteração de frontend/backend/banco compartilhado foi realizada nesta etapa.**

**Objetivo:** definir com precisão o Bootstrap Mode do +Compras (ADR-0020, item 12) antes de qualquer implementação — estados, condição de abertura, Bootstrap Secret, identidade inicial pré-autorizada, autenticação, sessão Bootstrap de privilégios limitados, modelo do Administrador Sênior, atomicidade/concorrência da conclusão, encerramento permanente, Recovery, threat model, rate limiting, auditoria, UX conceitual, modelo de dados conceitual e plano de testes obrigatório. Detalhe completo: `docs/architecture/security-design-auth-o1.4.md`, seção 20.

**Decisões principais desta revisão (detalhe e justificativa completos na seção 20 do documento):**
- Dois estados persistidos para `BootstrapEstado` (Disponível/Concluído), não quatro — `Concluido == false`/`true` já é suficiente; estado "em execução" de um candidato é tratado como sessão de fluxo, não como terceiro estado global.
- Identidade inicial: lista explícita de e-mails pré-autorizados em secret manager/User Secrets (Opção A) — nunca domínio+lista, nunca token administrativo separado.
- Sessão Bootstrap distinta da sessão normal (`BootstrapSession`), privilégios limitados por allowlist explícita de endpoints — nunca herda `AuthorizationOptions.FallbackPolicy` da sessão comum.
- Conclusão do Bootstrap: transação única (BU + Usuário + Perfil + `Concluido=true`) com `UPDATE` condicional (`WHERE Concluido = 0`) como compare-and-swap — tudo ou nada, sem estado intermediário persistido.
- Pode existir mais de um Administrador Sênior; o sistema deve impedir ficar sem nenhum Administrador Sênior ativo após o Bootstrap (hipótese do Product Owner validada).
- Bootstrap nunca reabre sob nenhuma circunstância (reafirmação de ADR-0020); Recovery pós-Bootstrap é procedimento operacional/break-glass separado, offline, nunca um endpoint HTTP.
- Development não recebe nenhum bypass/shortcut específico — mesmos endpoints/handlers em todos os ambientes, apenas configuração (secret/lista de e-mails) via User Secrets.

**Respostas SIM/NÃO às 10 decisões solicitadas:** ver `docs/architecture/security-design-auth-o1.4.md`, seção 20.21.

**BOOTSTRAP SECURITY DESIGN GATE: APROVADO COM PENDÊNCIAS** (nenhuma bloqueante para o início do detalhamento técnico — todas são decisões de detalhamento de implementação: nome de rota, schema exato da sessão Bootstrap, constraint física de linha única em `BootstrapEstado`, runbook de Recovery). Ver seção 20.22/20.23 do documento.

**Situação:** liberada para detalhamento técnico e Work Order de implementação (O1.4.3, próxima etapa), condicionada à ratificação desta arquitetura pelo Product Owner/CTO na Work Order técnica. Implementação de código não iniciada — exigirá nova validação de segurança dedicada depois de implementada, antes de Homologação (mesmo padrão de O1.4.1→O1.4.2→O1.4.2.1→O1.4.2.2).

**Commit/push:** não realizado.

---

## Work Order Técnica O1.4.3 — Bootstrap Mode e Administrador Sênior (07/08/2026)

**Status:** 🚧 Planejamento técnico concluído. **Nenhuma implementação de código, migration, endpoint ou alteração de frontend/backend/banco foi realizada.**

Transformação do Security Design aprovado (`docs/architecture/security-design-auth-o1.4.md`, seção 20) em plano técnico executável: fluxo completo (portão de segurança secret+identidade+OTP → wizard de BU/Administrador já especificado em `ComprasFuncional.md`/`ComprasUX.md` → conclusão transacional), responsabilidades por camada, projeto da sessão Bootstrap distinta (`BootstrapSessao`, esquema `BootstrapSession`, política `BootstrapAuthenticated`), Bootstrap Secret/allowlist como `Options` validadas, hardening de `BootstrapEstado` (linha única por chave fixa + `UPDATE` condicional), regra do último Administrador Sênior como método de domínio reutilizável, API proposta (`GET /bootstrap/estado`, `POST /bootstrap/iniciar`, `POST /bootstrap/otp/verificar`, `POST /bootstrap/concluir`), frontend `bootstrap/` (Vertical Slice), 2 migrations previstas (não criadas), 22 categorias de teste obrigatórias, e exigência de Security Self-Review + Security Validation independente antes de qualquer conclusão.

**Divergências encontradas (nenhuma bloqueante, detalhe completo na Work Order):** (1) o wizard de 3 passos já especificado em `ComprasFuncional.md`/`ComprasUX.md` não reflete os 3 fatores de autenticação do Security Design — resolvido nesta Work Order com um "passo 0" de portão de segurança antes do wizard já existente, com recomendação de atualizar os documentos de produto; (2) Vertical Slice de Identity não usa CQRS/MediatR (ADR-0003) — divergência pré-existente de O1.4.2, mantida por consistência; (3) `SessaoAutenticacao` sem `IdentityProviderId` (divergência pré-existente de O1.4.2, não relacionada ao Bootstrap); (4) `Perfil` sem índice único por (`UnidadeNegocioId`, `Nome`) — fechada nesta Work Order via nova migration prevista; (5) dessincronia de migrations do banco compartilhado (já registrada, não corrigida).

**Granularidade recomendada:** dividir em O1.4.3.1 (Fundação Backend — BootstrapEstado/BootstrapSession), O1.4.3.2 (Conclusão Transacional/Administrador Sênior), O1.4.3.3 (Frontend Bootstrap), O1.4.3.4 (Security Self-Review), pelo volume de código comparável ao da própria O1.4.2.

**Work Order:** `.ai/work-orders/active/O1.4.3-BootstrapEAdministradorSenior.md` (caminho válido no momento deste registro, 07/08/2026; a Work Order foi movida para `.ai/work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md` no fechamento formal de 10/08/2026 — ver seção de encerramento ao final deste documento).

**Situação (histórica, 07/08/2026):** aguardando autorização explícita do Product Owner/CTO para iniciar a implementação (Etapa O1.4.3.1). Nenhuma decisão de produto ou de segurança permanece bloqueante — apenas decisões de detalhamento de implementação (nomes de rota, estratégia exata de adaptação do OTP para candidatos sem `Usuario`), listadas como pendências na Work Order. **Estado atual: a O1.4.3 está FORMALMENTE CONCLUÍDA (10/08/2026)** — ver seção de encerramento ao final deste documento.

**Commit/push:** não realizado.

---

## Ato documental — Autorização explícita do Product Owner/CTO para início da O1.4.3.1 (07/08/2026)

**Registro:** o Product Owner/CTO (Julio) confirmou explicitamente, em 07/08/2026, a aprovação do início da implementação da Etapa **O1.4.3.1 — Fundação Backend do Bootstrap** (`BootstrapEstado`, `BootstrapSessao`, `BootstrapSessionAuthenticationHandler` + política `BootstrapAuthenticated`, endpoints `GET /bootstrap/estado`/`POST /bootstrap/iniciar`/`POST /bootstrap/otp/verificar`, sem `/bootstrap/concluir`), conforme escopo já definido na Work Order Técnica O1.4.3 (`.ai/work-orders/active/O1.4.3-BootstrapEAdministradorSenior.md`, seção 21). Esta aprovação não estava registrada nos documentos canônicos até este momento; este é exclusivamente o registro documental do fato de autorização, feito antes do início da implementação.

**Efeito:** a implementação da O1.4.3.1 está iniciando. A O1.4.3 (como um todo) permanece **não concluída** — nenhuma etapa da divisão recomendada (O1.4.3.1–O1.4.3.4) é declarada "Pronta" apenas por esta autorização; a conclusão de cada etapa segue exigindo build/testes aprovados, Security Self-Review e, ao final da O1.4.3 completa, Security Validation independente (ADR-0020, item 13), conforme já fixado na Work Order.

**Commit/push:** não realizado por este ato documental.

---

## O1.4.3.1 — Fundação Backend do Bootstrap: implementação técnica **PARCIAL** (07/08/2026)

**Status: PARCIAL — NÃO declarada concluída.** Todo o código de produção e de teste da etapa foi escrito seguindo integralmente a Work Order Técnica O1.4.3 (seção 21) e o Security Design (`docs/architecture/security-design-auth-o1.4.md`, §20), mas **não foi possível executar `dotnet restore`/`dotnet build`/`dotnet test`/`dotnet ef migrations add` nesta sessão de implementação**: o ambiente de execução de comandos disponível (`mcp__workspace__bash`, sandbox Ubuntu 22.04 isolado) não possui o .NET SDK instalado, não possui acesso de root/sudo funcional para instalá-lo, e o acesso de rede está bloqueado por allowlist (`blocked-by-allowlist` em todas as tentativas de download do instalador do .NET, incluindo `dot.net`/`download.visualstudio.microsoft.com`, e `apt-get install` falha por falta de permissão de root). Nenhuma ferramenta de shell alternativa com `dotnet` disponível foi encontrada via `ToolSearch`. Este é um bloqueio ambiental, não uma limitação de escopo ou de conhecimento da tarefa — está registrado explicitamente aqui, conforme instrução explícita do Product Owner/CTO para este cenário.

### Entregas de código realizadas (não validadas por build/test real)

- **Domain** (`backend/src/BlueprintOS.Domain/Identity/`): `BootstrapEstado.cs` (linha única, chave fixa `00000000-0000-0000-0000-000000000001`), `BootstrapSessao.cs` (sem `UsuarioId`, validade absoluta de 15 min, uso único), `CodigoVerificacaoOtp.cs` estendido (campo `EmailCandidato` novo, `UsuarioId` tornado `Guid?`, factory `ParaCandidatoBootstrap` — decisão de reuso da seção 11 da Work Order: opção recomendada, adotada).
- **Application** (`backend/src/BlueprintOS.Application/Identity/`): `IBootstrapEstadoRepository`, `IBootstrapSessaoRepository`, `BootstrapSecretOptions`, `BootstrapAllowedCandidatesOptions`, DTOs (`BootstrapDtos.cs`), contratos de caso de uso (`IBootstrapUseCases.cs`), `ConsultarBootstrapEstadoUseCase`, `IniciarBootstrapUseCase`, `ValidarOtpBootstrapUseCase` — todos reaproveitando `OtpHasher`/`OtpCodeGenerator`/`OtpRequestThrottle`/`OpaqueSessionToken`/`EmailAuditHasher` sem duplicação.
- **Infrastructure** (`backend/src/BlueprintOS.Infrastructure/`): `BootstrapEstadoRepository`, `BootstrapSessaoRepository`, `BootstrapSecretOptionsValidator` (fail-closed fora de Development, mesmo padrão de `CorporateOtpEmailSenderOptionsValidator`), configurações EF (`BootstrapEstadoConfiguration`, `BootstrapSessaoConfiguration`, `CodigoVerificacaoOtpConfiguration` atualizada, `PerfilConfiguration` com novo índice único `UnidadeNegocioId`+`Nome` — fecha a divergência nº 4 da Work Order), registro em `IdentityServiceCollectionExtensions`, `BlueprintOSDbContext` com dois novos `DbSet`s.
- **Api** (`backend/src/BlueprintOS.Api/Auth/` e `Identity/`): `BootstrapSessionAuthenticationHandler` (+ `BootstrapCookie`), `BootstrapAuthorization.cs` (`BootstrapNaoConcluidoRequirement`/`BootstrapNaoConcluidoAuthorizationHandler`), `BootstrapController` (`GET /bootstrap/estado`, `POST /bootstrap/iniciar`, `POST /bootstrap/otp/verificar` — sem `/bootstrap/concluir`), política `RateLimitingPolicies.BootstrapIniciar`, `Program.cs` atualizado (esquema `BootstrapSession` registrado em todos os ambientes como esquema adicional, política `BootstrapAuthenticated`, sem alteração de `FallbackPolicy`/`SessionCookie`/`DevelopmentHeader` existentes).
- **Migrations** (hand-escritas, `dotnet ef migrations add` não executável neste ambiente — ver bloqueio acima): `20260807160000_AddBootstrapEstado` (+ `.Designer.cs`) e `20260807170000_AddPerfilNomeUnidadeNegocioUniqueIndex` (+ `.Designer.cs`), `BlueprintOSDbContextModelSnapshot.cs` atualizado para o estado cumulativo final. **Nenhuma delas foi aplicada em nenhum banco, local ou compartilhado** — não houve acesso a nenhum banco de dados nesta sessão.
- **appsettings.json**: seção `Bootstrap` adicionada com `Secret: ""` e `AllowedCandidateEmails: []` — nenhum segredo ou e-mail real commitado.
- **Testes** (`backend/tests/BlueprintOS.UnitTests/`): `Domain/Identity/BootstrapEstadoTests.cs`, `BootstrapSessaoTests.cs`, adições a `CodigoVerificacaoOtpTests.cs`; `Application/Identity/BootstrapUseCasesTests.cs`, `BootstrapAllowedCandidatesOptionsTests.cs`; `Infrastructure/Identity/BootstrapSecretOptionsValidatorTests.cs`, `PerfilUniqueIndexTests.cs`, `BootstrapRepositoriesTests.cs`; `Api/Auth/BootstrapSessionAuthenticationHandlerTests.cs`, `BootstrapAuthorizationPipelineTests.cs`, `BootstrapControllerEndpointsTests.cs`. Fake `ICodigoVerificacaoOtpRepository` em `AuthUseCasesTests.cs` atualizado para o novo método de interface.

### Validações **NÃO executadas** (bloqueio ambiental, ver acima)

- `dotnet restore`/`dotnet build backend/BlueprintOS.sln` — não executado.
- `dotnet test` — não executado; contagem de testes pré-existente (306 backend) não pôde ser reconfirmada nem comparada à contagem pós-implementação nesta sessão.
- `dotnet ef migrations add` — não executado; as duas migrations desta etapa foram escritas manualmente, replicando fielmente o padrão das migrations já existentes (`AddOtpHardening`), mas **sem garantia de compilação/consistência de schema verificada por ferramenta** — risco residual explícito.
- `npx tsc -b`/`npm run build`/`npm run test` (frontend) — não executado (mesmo bloqueio de ambiente; nenhum arquivo de frontend foi alterado por esta etapa, então o risco de regressão incidental é baixo, mas não foi comprovado por execução real).
- `git diff --check` — não executado via `git` (sem acesso a repositório Git configurado neste ambiente de shell para o caminho do usuário); nenhuma edição de texto introduziu espaços em branco intencionais.

### Security Self-Review adversarial (realizada por leitura de código, não pôde ser combinada com evidência de execução)

Perguntas adversariais e achados (ver detalhamento completo no relatório desta sessão):
1. Comparação de secret em tempo constante — **OK** (`CryptographicOperations.FixedTimeEquals`, nunca `==`/`Equals`).
2. Enumeration protection nas respostas de erro de `/bootstrap/iniciar` — **OK** (mesma resposta 200 genérica para secret inválido/e-mail não autorizado/sucesso; trabalho equivalente executado nos três casos).
3. Independência de `BootstrapSession` frente a `SessionCookie`/`DevelopmentHeader`/`FallbackPolicy` — **OK** (esquema adicional, nunca default; política própria com `AddAuthenticationSchemes` explícito; testes de pipeline cobrindo os 4 sentidos da matriz).
4. Fail-closed de `BootstrapSecretOptions`/`BootstrapAllowedCandidatesOptions` ausentes — **OK** (`ValidateOnStart` fora de Development para o secret; lista vazia nunca autoriza ninguém, sem bypass).
5. Uso único e expiração absoluta de `BootstrapSessao` — **OK** (`MarcarUsada`/`Revogar` idempotentes com `??=`; sem renovação por atividade).
6. Ausência de vazamento de OTP/secret em logs/respostas — **OK** (nenhum `logger.Log*` ou resposta HTTP inclui o valor do OTP/secret; apenas `EmailAuditHasher.Hash` para correlação).
7. Rate limiting cobrindo secret+e-mail+IP em `/bootstrap/iniciar` — **OK** (`RateLimitingPolicies.BootstrapIniciar` por IP + `OtpRequestThrottle` reaproveitado por e-mail dentro do caso de uso).
8. Nenhuma migration aplicada em banco compartilhado — **OK** (nenhum comando de banco foi executado nesta sessão; nenhum acesso a `MaisComprasConnection` ocorreu).

Nenhum achado MÉDIO/ALTO identificado nesta revisão por leitura de código — mas a Self-Review **não pôde ser cruzada com execução real de testes**, o que é uma limitação relevante que impede a certificação completa exigida pela seção 19 da Work Order ("build verde e testes aprovados são pré-condição necessária, nunca suficiente"). A Self-Review completa e a Security Validation independente exigidas por ADR-0020 (item 13) **permanecem pendentes de confirmação por execução real**.

### Situação

**A Etapa O1.4.3.1 NÃO está concluída.** O código foi implementado integralmente conforme a Work Order, mas a Definition of Done (seção 24 da Work Order) exige build/testes aprovados como pré-condição, o que não pôde ser produzido nesta sessão por bloqueio de ambiente (ausência de .NET SDK executável, sem rede para instalação, sem privilégios de root). **Próximo passo obrigatório antes de qualquer avanço para O1.4.3.2:** executar, em um ambiente com .NET SDK disponível, `dotnet restore && dotnet build backend/BlueprintOS.sln && dotnet test`, revisar qualquer erro de compilação nas migrations hand-escritas (`20260807160000_AddBootstrapEstado`/`20260807170000_AddPerfilNomeUnidadeNegocioUniqueIndex` e seus `.Designer.cs`) contra o schema real gerado por `dotnet ef migrations add` (recomenda-se regenerar essas duas migrations via `dotnet ef migrations add` real e comparar/substituir as versões hand-escritas desta sessão), e só então declarar a etapa concluída. A O1.4.3 (etapa-mãe) permanece **não concluída**, restando O1.4.3.2/.3/.4 e a Security Validation independente final.

**Commit/push:** não realizado.

---

## O1.4.3.1 — Fundação Backend do Bootstrap: **CONCLUÍDA** (07/08/2026)

**Status: CONCLUÍDA.** Este registro fecha formalmente o bloqueio ambiental descrito no item anterior. A validação real foi executada no Mac do Product Owner (.NET SDK 9.0.316), em três sessões sucessivas de correção/reconciliação:

1. **Correção da suíte local pós-bootstrap** — a validação real revelou 7 testes falhando (nunca detectáveis no ambiente Cowork, que não tinha SDK .NET). Causas raiz: (a) `Host.CreateDefaultBuilder().Build()` valida (`ValidateOnBuild`) todo o grafo de DI, incluindo repositórios dependentes de `BlueprintOSDbContext` nunca registrado nos testes de fail-closed isolados; (b) três testes de "host deve iniciar quando X está configurado" não configuravam `Bootstrap:Secret`, então passaram a ser corretamente bloqueados pelo novo mecanismo fail-closed introduzido por esta mesma etapa; (c) `PerfilUniqueIndexTests` assumia que o provider InMemory do EF Core aplica índices únicos relacionais, o que é falso. Nenhuma regra de segurança foi enfraquecida — todas as correções foram no harness de teste (DbContext InMemory de teste, configuração de `Bootstrap:Secret` nos testes que a exigem, teste de índice reescrito para validar a declaração via metadata do EF em vez de depender do InMemory para aplicar uma constraint que ele não aplica). Resultado: **369/369 testes aprovados**, build limpo.
2. **Descoberta e reconciliação de duplicação de schema na cadeia de migrations** — `dotnet ef migrations script 0` revelou `CREATE TABLE [Fornecedores]` duplicado. Causa raiz: o repositório nunca teve um `BlueprintOSDbContextModelSnapshot.cs` commitado (as 8 migrations históricas de Fornecedor foram todas escritas manualmente, sem nunca passar por `dotnet ef migrations add`, porque nenhum ambiente anterior tinha SDK .NET real). Quando `AddIdentityAuthentication` foi gerada pela primeira vez com o SDK real, o EF não tinha baseline para diferenciar o que já existia — tratou o banco como vazio e tentou recriar as 8 tabelas de Fornecedor já existentes no banco compartilhado.
3. **Correção** — criada `BaselineFornecedorSnapshot` (migration NO-OP, `Up()`/`Down()` vazios, existe exclusivamente para estabelecer o snapshot correto do schema já aplicado pelas migrations manuscritas). As 4 migrations de Identity/Bootstrap (`AddIdentityAuthentication`, `AddOtpHardening`, `AddBootstrapEstado`, `AddPerfilNomeUnidadeNegocioUniqueIndex`) foram então regeneradas via `dotnet ef migrations add` real, com fidelidade total aos limites de escopo originais (cada uma isolando exatamente o incremento de schema daquela etapa, verificado por comparação byte-a-byte/semântica contra as versões manuscritas anteriores — `AddOtpHardening` idêntica, `AddPerfilNomeUnidadeNegocioUniqueIndex` idêntica exceto BOM/comentário, `AddBootstrapEstado` mesmas operações em ordem diferente, `AddIdentityAuthentication` agora sem nenhuma duplicação de Fornecedor).

**Evidências finais validadas pelo Product Owner no Mac:**
- `dotnet build backend/BlueprintOS.sln --no-restore` — sucesso, sem erros.
- `dotnet test backend/BlueprintOS.sln --no-restore` — **369/369 aprovados, 0 falhas**.
- `dotnet ef migrations has-pending-model-changes` — `"No changes have been made to the model since the last migration."` (modelo e snapshot em sincronia).
- `dotnet ef migrations script 0` — gerado com sucesso; busca por `CREATE TABLE [Fornecedores]` retorna **exatamente 1 ocorrência** (linha 12, pertencente à cadeia histórica de Fornecedor) — a duplicação foi eliminada.

**Migrations Identity/Bootstrap: VALIDADAS, MAS NÃO APLICADAS.** Nenhum `dotnet ef database update` foi executado em nenhum momento desta reconciliação. O banco compartilhado permanece com migrations aplicadas somente até `202608020001_B213FornecedorErpSyncHardening` — inalterado. A aplicação das migrations de Identity/Bootstrap ao banco compartilhado é uma decisão operacional deliberada e separada, ainda pendente, não coberta por este fechamento.

**Security Self-Review adversarial:** ver seção anterior (registrada na sessão de implementação original) — nenhum achado MÉDIO/ALTO. As correções desta etapa de validação foram exclusivamente de harness de teste e de reconciliação de metadata de migration, sem tocar em nenhuma regra de segurança (fail-closed do Bootstrap Secret, fail-closed do Corporate OTP Provider, índice único de `Perfil`, isolamento de `BootstrapSession` — todos permanecem intactos e cobertos pela suíte de 369 testes).

**Commit/push:** não realizado por este fechamento — as alterações de código/migrations desta reconciliação permanecem no working tree, aguardando decisão do Product Owner sobre commit.

---

## O1.4.3.2 — Conclusão Transacional e Administrador Sênior: ✅ CONCLUÍDA (10/08/2026)

**Registro de início:** implementação iniciada nesta sessão a partir do escopo da Etapa **O1.4.3.2 — Conclusão Transacional e Administrador Sênior** (`.ai/work-orders/active/O1.4.3-BootstrapEAdministradorSenior.md`, seção 21), confirmada como NÃO INICIADA antes desta sessão (nenhum `ConcluirBootstrapUseCase`/mutação de `BootstrapEstado` existia no código). A O1.4.3.1 permanece CONCLUÍDA e não foi reaberta.

**Entregas:** `ConcluirBootstrapUseCase` (Application/Identity) orquestrando, em uma única transação implícita (`SaveChangesAsync`), a conclusão do Bootstrap: criação-ou-reaproveitamento de `UnidadeNegocio` (reaproveitamento bloqueado se já houver Administrador Sênior ativo), criação do `Usuario` Administrador Sênior com e-mail exclusivamente da `BootstrapSessao` já validada por OTP (nunca do payload), criação-ou-reaproveitamento do `Perfil` "Administrador Sênior" pelo índice único (`UnidadeNegocioId`, `Nome`), vínculo `UsuarioPerfil`, invocação do método de domínio `AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo` (seção 14 da Work Order — identificado e chamado trivialmente nesta etapa, sem implementar os fluxos futuros de inativação/remoção), e transição única/permanente de `BootstrapEstado.Concluido` via compare-and-swap otimista (`RowVersion`, mesmo padrão já usado em `CodigoVerificacaoOtp`). Endpoint `POST /bootstrap/concluir` adicionado sob a política `BootstrapAuthenticated` já existente (nunca `FallbackPolicy`/`AllowAnonymous`), com `CsrfHeaderFilter` e rate limiting próprio (`bootstrap-concluir`, 3/15min por IP). Sessão de Bootstrap invalidada (uso único) em qualquer resultado; cookie `mc_bootstrap_sid` removido após sucesso — sem login automático do Administrador recém-criado (fluxo normal de OTP, O1.4.2, é usado depois).

**Migration:** `20260810120746_AddBootstrapConclusaoConcurrency`, contendo exclusivamente `ALTER TABLE [BootstrapEstado] ADD [RowVersion] rowversion NOT NULL` — necessária porque a garantia de "conclusão única sob concorrência real" (Work Order, seção 13/18 item 12) exige um token de concorrência otimista; gerada via `dotnet ef migrations add` real (SDK 9.0.316), nunca hand-escrita, após confirmação de que era a única alteração de modelo pendente. Auditoria do script completo (`dotnet ef migrations script 0`, 498 linhas) confirmou exatamente 1 `CREATE TABLE [Fornecedores]` (histórica) e que a última instrução da cadeia inteira é exatamente esse `ALTER TABLE` — nenhuma migration histórica ou reconciliada alterada, `BaselineFornecedorSnapshot` permanece NO-OP.

**Evidências finais:** build limpo (0 erros/avisos); **388/388 testes aprovados** (383 unitários + 5 integração, incluindo testes de concorrência real via EF Core InMemory — `Task.WhenAll` sobre `ConcluirBootstrapUseCase` produzindo exatamente um sucesso e nenhuma entidade órfã); `has-pending-model-changes` → `"No changes have been made to the model since the last migration."`; `git diff --check` limpo; nenhum resíduo (`.disabled`/temporário) encontrado.

**Migrations Identity/Bootstrap (incluindo esta nova): VALIDADAS, MAS NÃO APLICADAS.** Nenhum `dotnet ef database update` foi executado em nenhum momento desta etapa. O banco compartilhado `MaisComprasConnection` permanece inalterado, com migrations aplicadas somente até `202608020001_B213FornecedorErpSyncHardening`. A aplicação ao banco compartilhado continua sendo decisão operacional separada do Product Owner/Infra, ainda pendente.

**Security Self-Review:** revisão do próprio código contra a Work Order e `security-design-auth-o1.4.md` — fail-closed, `BootstrapAuthenticated`, CSRF, rate limiting, uso único de sessão e demais controles da O1.4.3.1 preservados sem redução; nenhum bypass específico para testes introduzido. **A Security Validation independente (segunda passada, fora desta sessão de implementação) permanece pendente**, exigida antes de a O1.4.3 (etapa-mãe) ser considerada "Pronta" (ADR-0020, item 13) — não bloqueia o encerramento desta etapa individual, mas bloqueia o encerramento da Work Order O1.4.3 como um todo.

**Situação:** **O1.4.3.2 formalmente CONCLUÍDA.** A Work Order mãe O1.4.3 permanece ATIVA (`.ai/work-orders/active/`) — restam O1.4.3.3 (Frontend Bootstrap, **NÃO INICIADA**) e O1.4.3.4 (Security Self-Review dedicada + preparação da Security Validation independente, **NÃO INICIADA**). Commit/push: não realizados nesta etapa.

## O1.4.3.3 — Frontend Bootstrap: ✅ CONCLUÍDA (10/08/2026)

**Registro de início:** implementação iniciada nesta sessão a partir do escopo da Etapa **O1.4.3.3 — Frontend Bootstrap** (`.ai/work-orders/active/O1.4.3-BootstrapEAdministradorSenior.md`, seções 16/21), confirmada como NÃO INICIADA antes desta sessão (nenhum diretório `frontend/web/src/bootstrap/` existia). A O1.4.3.1 e a O1.4.3.2 permanecem CONCLUÍDAS e não foram reabertas; nenhum código/migration de backend foi tocado nesta etapa (frontend puro). A O1.4.3.4 (Security Self-Review + preparação da Security Validation independente) permanece **NÃO INICIADA**, por desenho — não faz parte desta etapa.

**Entregas (Vertical Slice `frontend/web/src/bootstrap/`, mesmo padrão físico de `auth/`):** `types/bootstrapTypes.ts`; `services/bootstrapApi.ts` (`consultarEstado`/`iniciar`/`verificarOtp`/`concluir`, mesmo padrão de `authApi.ts` — cookie HttpOnly via `credentials: "include"`, header CSRF `X-MaisCompras-Csrf`, `BootstrapApiError` com `status`/`code`); `hooks/useBootstrapEstado.ts`; `components/BootstrapGate.tsx` (decisão de roteamento raiz — consulta `GET /bootstrap/estado` uma vez e decide `/bootstrap` vs. `/login`/área autenticada, exclusivamente UX, nunca controle de segurança); `routes/BootstrapRoutes.tsx`; `pages/BootstrapPage.tsx` (wizard completo: passo 0 de segurança — e-mail + Bootstrap Secret → OTP — seguido dos passos de produto já especificados em `ComprasUX.md`: Unidade de Negócio, Administrador Sênior sem nenhum campo de e-mail, confirmação explícita via checkbox → `POST /bootstrap/concluir`); `tests/BootstrapPage.test.tsx` (15 testes: render inicial, validação de campos obrigatórios via botão desabilitado, payload exato enviado a `/bootstrap/iniciar` e a `/bootstrap/concluir` — comprovando ausência de qualquer campo de e-mail no payload de conclusão —, estado "não disponível" em 404, erro genérico de OTP inválido, sessão expirada em 401/403 com retorno ao passo de acesso, 404 na conclusão tratado como indisponível, conflito/concorrência com mensagem genérica, erro 5xx genérico, falha de rede genérica, habilitação do botão de conclusão condicionada ao checkbox, e ausência de qualquer gravação em `localStorage`/`sessionStorage`). Roteamento raiz (`frontend/web/src/core/AppRoutes.tsx`) modificado para envolver as rotas existentes com `BootstrapGate` e adicionar `/bootstrap/*` → `BootstrapRoutes`, sem alterar nenhuma rota/comportamento existente (`/login/*`, `RequireAuth`, `AppShell`).

**Contrato real usado (verificado no código, não apenas na Work Order):** `backend/src/BlueprintOS.Api/Auth/BootstrapController.cs`/`BootstrapRequests.cs` — `GET /bootstrap/estado` (anônimo) → `{ disponivel }`; `POST /bootstrap/iniciar` (anônimo) → `200 { message }` genérico sempre, exceto `404` quando `Concluido == true`; `POST /bootstrap/otp/verificar` (anônimo) → `204` + cookie `mc_bootstrap_sid`, ou `400 { code, message }` genérico; `POST /bootstrap/concluir` (`[Authorize(Policy = "BootstrapAuthenticated")]`) → `200 { usuario: { id, email, nome }, unidadeNegocioId }`, sem nenhum campo de e-mail no `BootstrapConcluirRequest` (`UnidadeNegocio { Id, Nome, Slug }`, `Administrador { Nome }`) — confirmado no próprio `record` do backend.

**Evidências desta etapa:** suíte de frontend completa **53/53 testes aprovados** (9 arquivos, incluindo os 15 novos de `BootstrapPage` e os 38 pré-existentes, sem regressão); `tsc -b` (typecheck) limpo; `vite build` validado com sucesso (redirecionado para um `outDir` temporário fora do repositório devido a uma trava de permissão pré-existente e não relacionada a esta sessão sobre `frontend/web/dist/`, herdada do ambiente de sandbox — não uma falha de compilação; o `dist/` versionado foi restaurado ao estado original do HEAD via `git show`). Smoke test estrutural em Chrome: ver detalhe abaixo.

**GAP identificado (não bloqueante, registrado, não implementado):** `POST /bootstrap/concluir` aceita `unidadeNegocio` como `{ id }` (reaproveitar Unidade de Negócio existente sem Administrador Sênior) além de `{ nome, slug }` (criar nova) — esta etapa implementa apenas o caminho de criação de nova Unidade de Negócio na UI (fluxo primário de primeiro uso); o caminho de reaproveitamento por `id` não tem UI própria (não há endpoint de listagem de Unidades de Negócio acessível anonimamente/pela `BootstrapSessao` para popular tal seleção) — não é uma divergência de contrato, apenas uma extensão de UX não implementada nesta etapa, sem impacto de segurança.

**Situação (registro anterior):** implementada nesta sessão, pendente de validação do Product Owner — superado pelo encerramento formal abaixo.

---

## O1.4.3.3 — Frontend Bootstrap: Encerramento formal pós-smoke test real no Chrome (10/08/2026)

**Registro de encerramento:** o Product Owner/CTO (Julio) executou e aprovou, nesta sessão, o smoke test real completo da O1.4.3.3 em navegador Chrome real (via Chrome DevTools MCP), fechando a pendência de validação registrada no bloco anterior. Nenhum código de frontend ou backend foi alterado nesta sessão de encerramento — apenas execução do smoke test e fechamento documental.

**Auditoria prévia ao fechamento (executada nesta sessão):**
- `git status --short`: apenas alterações de documentação (`.ai/*.md`) e movimentações de arquivos frontend já pré-existentes no working tree (reorganização de outra tarefa em andamento, não tocada). Nenhuma alteração inesperada em `backend/` ou em migrations nesta sessão — o código de backend/migrations presente no working tree é o mesmo já entregue e registrado nas etapas O1.4.3.1/O1.4.3.2 (ainda não commitado), não algo criado ou alterado por esta sessão de smoke test.
- `git diff --check`: sem problemas de whitespace/conflito.
- Testes de frontend (`npm test`, `frontend/web`): **53/53 aprovados** (9 arquivos, incluindo os 15 de `BootstrapPage`), sem regressão.
- Typecheck + build de frontend (`npm run build`, que executa `tsc -b && vite build`): **aprovado**, sem erros.
- Nenhum arquivo temporário de smoke test (screenshot, dump, `.tmp`, `.disabled`) encontrado no repositório.
- Nenhum `dotnet ef database update` ou equivalente foi executado nesta sessão; não há evidência de alteração de schema nos arquivos — apenas a garantia documental de que nenhum comando desse tipo foi executado.

**Evidência de aceite — smoke test real no Chrome (executado e aprovado nesta sessão):**
- `GET /bootstrap/estado` inicial → `{"disponivel":true}`.
- Fluxo completo executado em Chrome real via Chrome DevTools MCP: acesso ao Bootstrap → e-mail autorizado → Bootstrap Secret válido → OTP gerado e validado → sessão de Bootstrap criada (cookie `mc_bootstrap_sid` enviado corretamente pelo Chrome e aceito/autenticado pelo backend, end-to-end, em contexto real de navegador) → Unidade de Negócio preenchida → Administrador Sênior preenchido → confirmação → conclusão real.
- Unidade de Negócio criada: Nome "Grupo Soma", Slug "grupo-soma".
- Administrador Sênior criado: Nome "Julio Cesar", E-mail julio.cesar@somagrupo.com.br (exclusivamente da `BootstrapSessao` validada por OTP, nunca do payload — conforme desenho já validado na O1.4.3.2).
- `POST /bootstrap/concluir` → **HTTP 200 OK**, payload `{"unidadeNegocio":{"nome":"Grupo Soma","slug":"grupo-soma"},"administrador":{"nome":"Julio Cesar"}}`; resposta confirmou criação de usuário e Unidade de Negócio.
- Após a conclusão, o cookie de Bootstrap foi expirado pelo servidor (comportamento esperado, sessão de uso único).
- UI exibiu: "Configuração inicial concluída. Redirecionando para o login…".
- `GET /bootstrap/estado` final → `{"disponivel":false}` → `BootstrapEstado.Concluido = true`.
- Esta conclusão é **real, intencional e foi autorizada explicitamente pelo Product Owner/CTO nesta sessão** — não deve ser revertida.

**Sobre a investigação anterior de um 401 relatado em sessão prévia:** o problema não foi reproduzido neste smoke test final — o fluxo completo funcionou de ponta a ponta no Chrome real, `/bootstrap/concluir` retornou 200, e `mc_bootstrap_sid` foi enviado e autenticado corretamente. A hipótese de falha estrutural por `Secure=true` em Development/loopback foi **refutada por teste real** (o cookie `Secure` foi aceito e reenviado normalmente pelo Chrome em contexto loopback HTTP). Não há evidência suficiente para atribuir uma causa definitiva ao 401 observado anteriormente — nenhuma hipótese (incluindo expiração de 15 minutos) foi comprovada, e nenhuma delas deve ser registrada como causa confirmada. Como o problema não foi reproduzido, nenhuma correção de segurança foi criada para ele.

**Situação:** **O1.4.3.3 formalmente CONCLUÍDA**, com frontend implementado, testes automatizados aprovados, build aprovado e smoke test real no Chrome aprovado pelo Product Owner/CTO nesta sessão. A Work Order mãe O1.4.3 **permanece ATIVA** (`.ai/work-orders/active/`) — resta **O1.4.3.4** (Security Self-Review dedicada + preparação da Security Validation independente), **NÃO INICIADA**, deliberadamente fora do escopo desta sessão de encerramento. Commit/push: não realizados.

---

## O1.4.3.4 — Security Self-Review dedicada + preparação da Security Validation independente (10/08/2026) — ✅ CONCLUÍDA

**Status:** ✅ CONCLUÍDA. Confirmado antes do início: O1.4.3.1/.2/.3 CONCLUÍDAS, O1.4.3.4 NÃO INICIADA, Work Order mãe O1.4.3 ATIVA (nenhuma delas reaberta nesta etapa). Revisão adversarial dedicada do código real (não da documentação), cruzando Security Design (`security-design-auth-o1.4.md` §20) → Work Order (seção 21) → código → testes → configuração → migrations → comportamento observado no smoke test real em Chrome já registrado na O1.4.3.3.

**Metodologia:** 4 revisões adversariais paralelas e independentes, cada uma tentando ativamente os cenários de ataque exigidos (assumir Administrador Sênior indevidamente, reutilizar OTP/BootstrapSession, contornar allowlist/Secret/CSRF/rate limiting, concluir Bootstrap duas vezes, acessar endpoint após conclusão, abusar de superfícies de Development), cobrindo: (1) Bootstrap Secret/allowlist/OTP; (2) BootstrapSession/CSRF/autorização; (3) Conclusão transacional/concorrência/Administrador Sênior; (4) Superfícies exclusivas de Development. Um finding HIGH condicional levantado pela revisão (3) — dependência não verificada do índice único de `UnidadeNegocio.Slug` — foi verificado diretamente nesta etapa: `UnidadeNegocioConfiguration.cs` confirma `HasIndex(x => x.Slug).IsUnique()`, refutando o finding.

**Resultado: nenhum CRITICAL ou HIGH confirmado.** Findings reais remanescentes, todos MEDIUM/LOW/INFORMATIONAL, apresentados como diagnóstico (nenhuma correção aplicada silenciosamente, conforme exigido):

- **MEDIUM — Detecção de violação de índice único por substring de mensagem de exceção** (`BootstrapEstadoRepository.cs`, `IsUniqueConstraintViolation`): acoplada a texto de erro do provedor (`Contains("UNIQUE", ...)`), não a código de erro estruturado (`SqlException.Number`). Funciona hoje contra SQL Server em inglês, mas é frágil a mudança de locale/versão. Recomendação: usar `SqlException.Number` (2601/2627) com fallback de string apenas para InMemory/teste. Não bloqueante — a transação ainda reverte corretamente mesmo se o texto não casar; o efeito é um 500 não classificado, não uma falha de segurança.
- **MEDIUM — Corrida sobre o índice único de `Perfil` (reaproveitamento de BU existente sem Administrador Sênior) não é exercitada por teste de concorrência real**: o único teste de concorrência real (`ConcluirBootstrapConcurrencyTests.cs`, InMemory) usa BU nova em ambas as execuções concorrentes, cenário em que o RowVersion CAS de `BootstrapEstado` já resolve a corrida antes do índice de `Perfil` entrar em jogo. O cenário de duas conclusões reaproveitando a mesma BU existente concorrentemente (Work Order §13, item 2) não tem teste dedicado. Recomendação: adicionar teste de concorrência com BU pré-existente sem Administrador Sênior, ou validar via teste de integração real contra SQL Server.
- **MEDIUM — `AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo` chamado com valor hardcoded (`quantidadeAtivaAposOperacao: 1`)**: correto e sem risco nesta etapa (é sempre a primeira criação, a invariante é satisfeita por construção — conforme já documentado na Work Order §14), mas oferece proteção zero hoje. Ponto de atenção explícito para as Work Orders futuras que implementarem inativação/remoção de Administrador Sênior: a contagem deve ser sempre uma consulta real ao banco dentro da mesma transação, nunca uma constante.
- **LOW — `BootstrapSessao` não é revogada em falhas de payload dentro de `ConcluirBootstrapUseCase`** (só em sucesso ou falha de concorrência): permite retry com a mesma sessão dentro da janela de 15 min após uma falha de payload. Sem explorabilidade real (ainda exige sessão válida + rate limit próprio de `/bootstrap/concluir`), é uma leitura estrita divergente do texto do Security Design §20.7 quanto a "falha definitiva". Recomendação: decisão de produto/segurança sobre se deve revogar sempre; não bloqueante.
- **LOW — `DevelopmentRequestIdentity` não tem checagem própria de loopback** (só `IsDevelopment()`), assimétrico em relação a `DevelopmentHeaderAuthenticationHandler`/`/dev/otp` (que têm dupla barreira). Sem bypass demonstrável hoje (mesmo registro condicional do handler já barra o cenário), recomendação de adicionar a checagem por consistência defensiva.
- **INFORMATIONAL (múltiplos, sem ação):** CSRF valida apenas presença do header (desenho intencional, defesa em profundidade com SameSite=Strict como primária); comparação de tamanho antes de `FixedTimeEquals` no Bootstrap Secret (não vaza conteúdo, apenas "vazio ou não"); ausência de `appsettings.Production.json` (depende de disciplina operacional externa, não de código); componentes de Development permanecem fisicamente no assembly publicado, mas nunca registrados/roteados fora de Development.

**Confirmações positivas relevantes (sem finding, verificadas adversarialmente):** Bootstrap Secret nunca comparado por `==`/`Equals` (sempre `FixedTimeEquals`) e secret vazio nunca tratado como válido; allowlist vazia nunca autoriza nenhum e-mail; nenhuma resposta de `/bootstrap/iniciar` distingue secret inválido de e-mail não autorizado (com equalização de custo de CPU via hash dummy); OTP de Bootstrap com uso único/expiração/limite de tentativas/consumo atômico via RowVersion, mesmo mecanismo já usado no login normal; rate limiting em camada dupla (IP via middleware + e-mail via throttle persistido) resistente a chamada direta ao endpoint; `BootstrapSession` estruturalmente isolada de `SessionCookie`/`DevelopmentHeader` (esquemas de autenticação distintos, política `BootstrapAuthenticated` restrita ao esquema `BootstrapSession`); nenhum endpoint de negócio aceita a política `BootstrapAuthenticated`; dupla checagem de `BootstrapEstado.Concluido` (na política de autorização e novamente em cada caso de uso, fail-closed em linha ausente); CSRF aplicado nos 3 endpoints mutáveis (`/bootstrap/iniciar`, `/bootstrap/otp/verificar`, `/bootstrap/concluir`); cookie `mc_bootstrap_sid` com `HttpOnly`/`Secure`/`SameSite=Strict` sem exceção condicional; nenhum token/OTP/secret vazado em log ou resposta em nenhum ambiente, inclusive Development; e-mail do Administrador Sênior estruturalmente impossível de vir do payload (`AdministradorSeniorBootstrapPayload` não tem propriedade `Email`); `bootstrapSessaoId` vem exclusivamente de claim autenticada, nunca do corpo da requisição; atomicidade real via `SaveChangesAsync` único (comprovada por teste de concorrência real `Task.WhenAll`, exatamente uma conclusão vence, zero entidades órfãs); migration `AddBootstrapConclusaoConcurrency` contém exclusivamente a coluna `RowVersion`; índice único de `Perfil` por (`UnidadeNegocioId`, `Nome`) e de `UnidadeNegocio.Slug` corretamente configurados; superfícies de Development (`DevelopmentHeaderAuthenticationHandler`, `/dev/otp`, seleção de `IOtpEmailSender`) protegidas por dupla barreira (ambiente + origem loopback real via TCP, sem confiar em headers forjáveis), sem fallback silencioso fora de Development.

**Testes adversariais mapeados:** 13 arquivos de teste dedicados a Bootstrap já existentes (`BootstrapAuthorizationPipelineTests`, `BootstrapConcluirEndpointTests`, `BootstrapControllerEndpointsTests`, `BootstrapSessionAuthenticationHandlerTests`, `BootstrapAllowedCandidatesOptionsTests`, `BootstrapUseCasesTests`, `ConcluirBootstrapConcurrencyTests`, `ConcluirBootstrapUseCaseTests`, `BootstrapEstadoTests`, `BootstrapSessaoTests`, `CodigoVerificacaoOtpTests`, `BootstrapRepositoriesTests`, `BootstrapSecretOptionsValidatorTests`, `FailClosedHostStartupTests`), cobrindo as 22 categorias do plano de testes da Work Order (seção 18), incluindo concorrência real via `Task.WhenAll`. Nenhum novo teste foi adicionado nesta etapa — os gaps identificados (MEDIUM #2 acima) foram registrados como recomendação, não corrigidos silenciosamente, por não serem bloqueantes.

**Validação completa executada nesta etapa:**
- `dotnet build backend/BlueprintOS.sln`: aprovado, 0 erros, 0 avisos.
- `dotnet test backend/BlueprintOS.sln`: aprovado, **388/388** (383 unitários + 5 integração) — sem regressão.
- `npx tsc -b` / `npm run build` (frontend): aprovado, 0 erros.
- `npm run test` (Vitest): aprovado, **53/53**.
- `dotnet ef migrations has-pending-model-changes`: "No changes have been made to the model since the last migration."
- `dotnet ef migrations script 0`: gerado e revisado — exatamente 1 `CREATE TABLE [Fornecedores]` (linha 12, histórica), zero duplicações; nenhuma migration histórica alterada.
- `git diff --check`: sem problemas de whitespace.
- `git status --short`: working tree com as alterações acumuladas das etapas anteriores (O1.3.x/O1.4.x), nenhuma descartada, limpa ou reorganizada nesta etapa.

**Estado real do banco (distinção explícita exigida):** o Bootstrap real já foi concluído através da aplicação na O1.4.3.3 (`BootstrapEstado.Concluido = true`; Unidade de Negócio "Grupo Soma"; Administrador Sênior "Julio Cesar"; vínculos correspondentes) — são alterações de **dados** legítimas feitas pela própria aplicação, não alterações de **migrations**. Nenhuma migration foi aplicada a nenhum banco nesta etapa; os dados do Bootstrap real não foram alterados nem revertidos.

**Nenhuma correção de código foi aplicada nesta etapa** — apenas diagnóstico, conforme exigido (findings MEDIUM/LOW apresentados, não corrigidos silenciosamente). Nenhum commit/push realizado. Nenhum `dotnet ef database update` executado.

### Handoff para Security Validation independente

**Escopo que o revisor independente deve validar:** Bootstrap takeover; OTP (Bootstrap); Bootstrap Secret; allowlist; BootstrapSession; cookies; CSRF; rate limiting; concorrência; transação; regra do último Administrador Sênior; authorization policies (`BootstrapAuthenticated` vs. `FallbackPolicy`); endpoints exclusivos de Development; migrations relacionadas (`AddBootstrapEstado`, `AddPerfilNomeUnidadeNegocioUniqueIndex`, `AddBootstrapConclusaoConcurrency`); comportamento pós-Bootstrap. Atenção especial recomendada aos 2 pontos MEDIUM/gap de teste registrados acima (detecção de violação de índice único por string; ausência de teste de concorrência real para reaproveitamento de BU existente).

**Evidências disponíveis para o revisor independente:** `docs/architecture/security-design-auth-o1.4.md` §20; `.ai/work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md` (seções 7–22; caminho atualizado após o fechamento formal — no momento deste registro, 10/08/2026, o arquivo ainda estava em `active/`); suíte de testes (13 arquivos dedicados a Bootstrap, ver acima); arquivos principais (`ConcluirBootstrapUseCase.cs`, `IniciarBootstrapUseCase.cs`, `ValidarOtpBootstrapUseCase.cs`, `BootstrapSessionAuthenticationHandler.cs`, `BootstrapController.cs`, `BootstrapEstado.cs`, `BootstrapSessao.cs`, `AdministradorSeniorInvariantService.cs`); migrations (`AddBootstrapEstado`, `AddPerfilNomeUnidadeNegocioUniqueIndex`, `AddBootstrapConclusaoConcurrency`); smoke test real ponta a ponta em Chrome já registrado e aprovado pelo Product Owner/CTO na O1.4.3.3. Esta Self-Review **não substitui** a Security Validation independente — é a preparação exigida por ela (ADR-0020, item 13; Work Order §19).

**Situação final:** **O1.4.3.4 formalmente CONCLUÍDA.** **A Work Order mãe O1.4.3 permanece ATIVA — AGUARDANDO SECURITY VALIDATION INDEPENDENTE** antes de poder ser declarada "Pronta" (ADR-0020, item 13; Work Order §23/24). Work Order não movida para `completed/`. Commit/push: não realizados.

---

## Encerramento formal da O1.4.3 — Security Validation independente, reconciliação e aceite do Product Owner (10/08/2026)

**Security Validation independente: ✅ CONCLUÍDA (10/08/2026).** Executada por um revisor logicamente isolado, sem acesso prévio aos achados da Security Self-Review (O1.4.3.4 acima) — leitura própria dos documentos canônicos (Work Order, `security-design-auth-o1.4.md`, ADR-0020 em `DECISIONS.md`, `definition-of-done.md`) e do código real, com 8 provas adversariais próprias (13 casos), cobrindo as 13 áreas obrigatórias de escopo (bootstrap takeover, Bootstrap Secret, allowlist, OTP, BootstrapSession, CSRF, rate limiting/abuso, atomicidade transacional, concorrência real, invariante do último Administrador Sênior, autorização/isolamento de esquemas, superfícies exclusivas de Development, estado pós-Bootstrap). **Resultado: 0 CRITICAL, 0 HIGH.** Confirmadas por prova adversarial direta: encerramento permanente do Bootstrap (4 barreiras fail-closed), impossibilidade de payload tampering do e-mail do Administrador, comparação do secret em tempo constante, anti-oráculo, isolamento total da `BootstrapSessao` frente à sessão normal (esquemas/cookies distintos), atomicidade via `RowVersion`+índice único sob concorrência real (`Task.WhenAll`), e tripla barreira das superfícies `/dev/*` fora de Development. Achados remanescentes: 4 MEDIUM, 5 LOW, 4 INFORMATIONAL (detalhe na Work Order, seção 23.1). `dotnet build`/`dotnet test` 388/388, `tsc`/`vite build`/`vitest` 53/53, `has-pending-model-changes` limpo, `migrations script 0` sem duplicação, `git diff --check` limpo — nenhuma migration aplicada, nenhum commit/push. **Parecer: APROVADA COM RESSALVAS.**

**Reconciliação das divergências Self-Review × Validação independente: ✅ CONCLUÍDA (10/08/2026).** Duas divergências foram investigadas por leitura direta adicional do código, sem alterar produção/testes: (1) `DevelopmentRequestIdentity` não possui checagem própria de loopback (fato confirmado — mesmo achado LOW já registrado acima na Self-Review), mas a proteção real é herdada de `DevelopmentHeaderAuthenticationHandler` (loopback estrito) + registro condicional a `IsDevelopment()` em `Program.cs` — nenhum caminho hoje alcança `DevelopmentRequestIdentity` sem essa barreira externa; reclassificado de LOW para **INFORMATIONAL**. (2) Detecção de violação de índice único por substring de mensagem (`BootstrapEstadoRepository.cs`, mesmo achado MEDIUM já registrado acima) é robustez técnica, não falha de segurança/consistência — o índice único real no banco e o CAS de `RowVersion` garantem a invariante independentemente da classificação do erro; rebaixado de **MEDIUM para LOW**. Verificação adicional sobre o gap de teste de concorrência do `Perfil` (MEDIUM #2 da Self-Review acima): o teste permanente (`ConcluirBootstrapConcurrencyTests.cs`) existe, mas exercita o CAS de `RowVersion`, não o índice único de `Perfil` (InMemory não aplica índices únicos) — classificado como **GAP DE TESTE NÃO BLOQUEANTE** (invariante garantida estruturalmente pelo índice real). **Nenhum achado subiu de severidade.** Parecer confirmado: **APROVADA COM RESSALVAS**.

**Aceite formal do Product Owner (10/08/2026):** o Product Owner aceitou o parecer **Security Validation independente: APROVADA COM RESSALVAS** e os 15 findings remanescentes consolidados (4 MEDIUM + 6 LOW + 5 INFORMATIONAL, registrados em `.ai/BACKLOG.md`, seção "Dívida técnica registrada — Security Validation independente O1.4.3"). Nenhum finding é bloqueante para o fechamento da O1.4.3. MEDIUM 1 (`Cache-Control: no-store` ausente em `/bootstrap/*`) e MEDIUM 2 (Bootstrap Secret sem validação de entropia) **bloqueiam explicitamente a promoção do ambiente para Homologação** — este aceite não constitui autorização para Homologação.

**O1.4.3 — Bootstrap e Administrador Sênior: FORMALMENTE CONCLUÍDA (10/08/2026).** Work Order movida de `.ai/work-orders/active/` para `.ai/work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md`. Nenhuma migration aplicada nesta sessão de fechamento; nenhum código de produção/teste alterado; nenhum commit/push realizado — a consolidação para commit/push das alterações acumuladas desde O1.4.3.1 será feita em sessão separada.

**Próxima etapa:** decisão do Product Owner sobre a próxima frente de trabalho (nenhuma nova sprint iniciada nesta sessão). Antes de qualquer promoção para Homologação, tratar e revalidar MEDIUM 1 e MEDIUM 2 do backlog técnico.

---

## Sessão de Planejamento — Consolidação e Plano Executável de Conclusão da Onda 1 (10/08/2026)

**Nenhuma sprint funcional está em andamento.** Esta sessão foi exclusivamente de planejamento/governança: reconciliação dos 41 entregáveis oficiais da Onda 1, registro das decisões D1–D8 do Product Owner (ADR-0021, `.ai/DECISIONS.md`) e definição de 10 novas Work Orders (O1.5 a O1.14, todas Draft/Planejada) para conclusão da Onda 1. Detalhe completo em `docs/audits/Onda1-Reconciliacao-e-Plano-Execucao.md` e `.ai/BACKLOG.md`. **Nenhuma implementação foi iniciada; nenhuma Work Order foi aprovada/ativada; nenhum código, migration, frontend ou backend foi alterado; nenhum commit/push foi realizado.** A ativação da primeira Work Order do plano (O1.5 — RBAC Real) aguarda autorização explícita do Product Owner.

---

## Correção pontual — Regressão de autenticação em Development pós-O1.4.3 (10/08/2026)

**Contexto:** ao validar o login normal (não-Bootstrap) pela primeira vez após o encerramento formal da O1.4.3, o Product Owner reportou que o OTP validava mas o usuário retornava à tela de login. Investigação e correção tratadas fora do escopo de qualquer Work Order funcional — **a O1.4.3 não foi reaberta e a O1.5 não foi iniciada**.

**Correção A — Login OTP real em Development:** o esquema de autenticação default em Development usava exclusivamente `DevelopmentHeaderAuthenticationHandler`, que nunca examina o cookie `mc_sid` — uma sessão OTP real (que emite o cookie independentemente do ambiente) nunca autenticava `GET /auth/me` localmente. Corrigido com um `PolicyScheme` em `Program.cs`: cookie `mc_sid` presente → `SessionCookieAuthenticationHandler`; ausente → `DevelopmentHeaderAuthenticationHandler` (comportamento legado preservado); cookie inválido não cai para o header (fail-closed, sem fallback silencioso). Reproduzido e validado ao vivo no Chrome (Chrome DevTools MCP): `/auth/otp/verify` → 200, `/auth/me` → 200, Dashboard autenticado, sessão estável em navegação entre rotas protegidas.

**Correção B — `ICurrentIdentity` em Development:** mesmo com `/auth/me` corrigido, endpoints de negócio que dependem de `ICurrentIdentity` (ex.: `/fornecedores?q=...`) continuavam falhando com `IdentityUnavailableException`, porque `DevelopmentRequestIdentity` reparseava o header `X-Development-User-Id` diretamente, ignorando `HttpContext.User` já resolvido pela autenticação. Corrigido trocando a implementação registrada em Development para `SessionCurrentIdentity` (a mesma classe usada fora de Development), que lê exclusivamente as claims já publicadas pelo authentication handler que autenticou a requisição — sem duplicar parsing de cookie/header em nenhum caso de uso. A prioridade sessão-real-sobre-header é garantida estruturalmente pelo `PolicyScheme` da Correção A, não por lógica própria desta classe.

**Achado registrado como pendência separada (não corrigido nesta sessão):** com a identidade resolvida corretamente, `/fornecedores?q=...` passou a alcançar a consulta real ao banco (confirmado no log: parâmetro `@__userId_0` com o `UserId` real da sessão), mas retorna 500 por `SqlException: Invalid column name 'Cnpj'`/`'Nome'` — drift de schema entre o mapeamento EF e o banco local, sem relação com autenticação/identidade. Registrado em `.ai/BACKLOG.md` para tratamento em sessão dedicada; nenhuma migration/`database update` executada.

**Arquivos alterados:** `backend/src/BlueprintOS.Api/Program.cs` (as duas correções). **Testes novos:** `backend/tests/BlueprintOS.UnitTests/Api/Auth/DevelopmentSessionCookiePipelineTests.cs` (4 testes — sessão real, header legado, cookie inválido, ausência de credencial) e `backend/tests/BlueprintOS.UnitTests/Api/Identity/DevelopmentCurrentIdentityPipelineTests.cs` (6 testes — inclui sessão real prevalecendo sobre header conflitante e o caso fail-closed de cookie inválido + header válido). **Validação final:** backend **393 testes unitários + 5 de integração aprovados** (398 total, sem regressão); frontend **53/53** testes, `tsc -b` e `vite build` sem erros; `git diff --check` limpo. Nenhuma migration aplicada, nenhum RBAC alterado, nenhuma alteração no Bootstrap.

**Situação final:** duas correções de fundação de autenticação em Development publicadas por commit dedicado. **A O1.4.3 permanece FORMALMENTE CONCLUÍDA (não reaberta). A O1.5 permanece NÃO INICIADA.**

---

## Sprint O1.5 — RBAC Real (Perfis, Permissões, Policies, Enforcement) — aberta em 11/08/2026 — ✅ CONCLUÍDA (11/08/2026)

Status:
**✅ CONCLUÍDA (11/08/2026).** Implementação concluída, Security Validation independente executada (APROVADA COM RESSALVAS; 0 CRITICAL / 0 HIGH remanescentes) e **ressalvas aceitas formalmente pelo Product Owner em 11/08/2026** — ver "Encerramento formal da Sprint O1.5" ao final deste documento. A Work Order foi movida para `.ai/work-orders/completed/O1.5-RbacReal.md`, seguindo exatamente o precedente da O1.4.3.

Objetivo:
Transformar a fundação visual/mockada de Perfis e Permissões em RBAC real e efetivamente aplicado, fechando o ciclo Perfil → Permissões → Usuário×Perfil → Identidade autenticada → Policies → Enforcement (ADR-0020 itens 7/8/9/10; ADR-0021 decisão D2).

### Estado de entrada validado

`git status` limpo; branch `main`; `origin/main...main = 0 0`; último commit `2c717f2`, com `4338469`, `ea263e8`, `e27de66` e `f5d8e3f` no histórico — exatamente o estado consolidado esperado. Baseline de testes medida antes de qualquer alteração: **393 unitários + 5 integração backend**, **53 frontend**.

### Descoberta técnica registrada

- `Perfil`/`Permissao`/`PerfilPermissao`/`UsuarioPerfil` **já existiam** como entidades e tabelas (migration de Identity, O1.4.3.1), porém **vazias** e usadas apenas pelo Bootstrap. Não havia nenhum vínculo Perfil×Permissão persistido em lugar algum.
- `administration/profiles` era **100% mockado**: `perfisMockApi.ts` (dados em memória) + catálogo estático de permissões no frontend.
- A identidade autenticada (`SessionCookieAuthenticationHandler` → `SessionCurrentIdentity`) publicava `NameIdentifier`, `Email`, `Name`, `unidade_negocio_id` e um `Role` **fixo em "Buyer"** — nenhuma permissão.
- Mecanismos de Authorization existentes: `AuthorizationOptions.FallbackPolicy` (secure-by-default, O1.4.2.1) e a policy `BootstrapAuthenticated` com `IAuthorizationRequirement` customizado — padrão idiomático reaproveitado pela O1.5.
- **Nenhum** endpoint administrativo tinha enforcement além de autenticação. `Perfil.Gerenciar` e os demais códigos existiam apenas como texto em `ComprasFuncional.md`.
- Testes existentes de auth/autorização: `BootstrapAuthorizationPipelineTests`, `DevelopmentSessionCookiePipelineTests`, `SessionCookieAuthenticationHandlerTests`, `PerfilUniqueIndexTests` e as suítes de OTP/Bootstrap.
- **Divergência documental encontrada:** `PROJECT_STATE.md`/`BACKLOG.md` afirmam que as migrations de Identity/Bootstrap foram "validadas mas **não aplicadas**" ao banco compartilhado. `dotnet ef migrations list` contra o banco real mostra **todas aplicadas** (até `20260810120746_AddBootstrapConclusaoConcurrency`) — coerente com o smoke test de Bootstrap concluído com sucesso na O1.4.3.3. O registro documental estava desatualizado.

### Entregas

**Backend**
- `Domain/Identity/PermissaoCatalogo.cs` — fonte central única dos 14 códigos de permissão, com Ids estáveis. Policies, seed de banco, endpoints e catálogo do frontend derivam todos dele; nenhum código de permissão é escrito literalmente em outro lugar.
- `Perfil` ganhou `Descricao`, `CriadoEm`/`AtualizadoEm` e comportamento real (`Atualizar`, `Ativar`, `Inativar`). Sem exclusão física, conforme `ComprasFuncional.md`.
- `Application/Identity/PerfilUseCases.cs` — Listar/Obter/Criar/Atualizar/AlterarStatus + `NaoEscalonamento` + `PerfilAdministrativoInvariante`.
- `Infrastructure/Identity` — `PerfilRepository` estendido, `PermissaoRepository`, `PermissoesEfetivasResolver` (união dos Perfis ativos da BU da sessão, deduplicada).
- `ObterIdentidadeAtualUseCase` passou a resolver as permissões efetivas **a cada requisição**, junto da revalidação de sessão — revogação tem efeito imediato.
- `Api/Authorization/RbacAuthorization.cs` — `PermissaoRequirement`, `PermissaoAuthorizationHandler`, `RbacPolicies` (uma policy por permissão; `For()` lança na inicialização para código fora do catálogo).
- `Api/Administration/PerfisController.cs` — 6 endpoints sob `/api/administracao`, todos exigindo `Perfil.Gerenciar`, CSRF no nível do grupo.
- `ConcluirBootstrapUseCase` passou a conceder o catálogo completo ao Perfil "Administrador Sênior" — sem isso, um ambiente novo nasceria com o administrador sem nenhuma permissão.
- `CsrfHeaderFilter` passou a ignorar métodos seguros (RFC 9110), viabilizando aplicá-lo no nível de `MapGroup`. Nenhum endpoint pré-existente muda de comportamento (o filtro só estava anexado a POSTs).

**Migration** — `20260811143355_AddRbacPerfilPermissaoCatalogo`: colunas novas em `Perfis`; seed das 14 permissões com Ids estáveis; FKs `RESTRICT` em `PerfisPermissoes` e `UsuariosPerfis` + índices; dois blocos de SQL manual documentados (backfill de timestamps; concessão idempotente do catálogo aos Perfis "Administrador Sênior" existentes, para não bloquear o ambiente).

**Frontend** — padrão visual aprovado **preservado**; só mudanças funcionalmente necessárias: `perfisMockApi.ts` e o catálogo estático **removidos**; `perfisApi.ts` real; `ConfirmExclusaoModal` → `ConfirmStatusModal` (o backend não expõe exclusão); campos "Unidade de Negócio" e "Status" removidos do formulário (o primeiro seria ignorado pelo backend; o segundo virou ação própria com confirmação); estados de carregando/sucesso/vazio/erro/**acesso negado** tratados; `AppShell` esconde "Perfis" sem a permissão (UX apenas); `authTypes`/`AuthContext` carregam `permissoes` vindas de `/auth/me`.

### Validações executadas

- `dotnet build backend/BlueprintOS.sln`: **0 erros, 0 avisos**.
- `dotnet test backend/BlueprintOS.sln`: **472 unitários + 5 integração = 477 aprovados, 0 falhas** (baseline 398; +79 testes).
- `npx tsc -b`: 0 erros. `npm run build`: aprovado. `npm run test` (Vitest): **61 aprovados, 0 falhas** (baseline 53).
- `dotnet ef migrations has-pending-model-changes`: sem alterações pendentes. `migrations script 0`: exatamente 1 `CREATE TABLE [Fornecedores]` histórico, inalterado.
- **Migration aplicada** ao banco de desenvolvimento `MaisCompras` (`dotnet ef database update`), necessária para o smoke test real.

### Smoke test real (backend + frontend + SQL Server reais)

Executado com `scripts/start-dev.sh` (API em `:5262`, Vite em `:5173`), login OTP real de `julio.cesar@somagrupo.com.br`, e Chrome via Chrome DevTools MCP.

Enforcement, via HTTP real:
- sem sessão → **401**; esquema de Development (autenticado, sem permissões) → **403**; sessão com `Perfil.Gerenciar` → **200**.
- `GET /auth/me` devolveu as **14 permissões efetivas resolvidas do banco** (backfill da migration funcionou).
- CRUD real: criar → **201**; nome duplicado → **409**; permissão fora do catálogo (`Sistema.Root`) → **400**; editar substituindo o conjunto → **200**; inativar/reativar → **200**; Id inexistente → **404**; POST sem header CSRF → **403**; GET sem header CSRF → **200** (método seguro).
- Invariante anti-auto-bloqueio: remover `Perfil.Gerenciar` do último Perfil administrativo → **409**; inativá-lo → **409**; Perfil permaneceu ativo com 14 permissões.
- Não-escalonamento: o Administrador Sênior (catálogo completo) concede `Sistema.Gerenciar` normalmente → **201**.

Interface, em Chrome real: `/administracao/perfis` renderizou os Perfis reais do banco; o formulário carregou o catálogo de **14 permissões vindo da API**, agrupado por recurso; criação pela tela persistiu e apareceu na listagem; persistência confirmada após **reload**; tela de detalhes exibiu a permissão correta; **zero erros/avisos de console**; padrão visual AZZAS/SOMA inalterado.

**Problema real encontrado e corrigido durante o smoke test:** o proxy do Vite em `/administracao` sombreava as rotas da SPA, fazendo `/administracao/perfis` devolver JSON da API em vez da tela. A API foi movida para `/api/administracao` — mesmo cuidado já documentado para `/bootstrap` em `vite.config.ts`.

**Segunda regressão introduzida e corrigida na mesma sessão:** aplicar `CsrfHeaderFilter` no nível do grupo passou a exigir o header também em `GET`, quebrando as leituras. Corrigido tornando o filtro ciente de métodos seguros.

### Security Validation independente (11/08/2026)

Executada por revisor logicamente isolado, sem acesso ao raciocínio do implementador — mesmo padrão da O1.4.3. Auditou 14 classes de ataque/falha contra o código real.

**Veredito: APROVADA COM RESSALVAS.** Achados originais: 0 CRITICAL, **1 HIGH**, 3 MEDIUM, 4 LOW, 4 INFORMATIONAL.

**Corrigidos nesta sessão, com teste de regressão para cada um:**
- **HIGH — escalonamento de privilégio sem limite.** Qualquer portador de `Perfil.Gerenciar` podia editar o próprio Perfil anexando todo o catálogo e, como as permissões são reresolvidas a cada requisição, já teria acesso total na chamada seguinte, sem novo login e sem rastro. Corrigido pela regra de não-escalonamento (`NaoEscalonamento`): ninguém concede permissão que não possui → **403 `escalonamento_de_privilegio`**.
- **MEDIUM — permissões efetivas não escopadas por Unidade de Negócio.** Permissão concedida na BU-B autorizaria ação sobre dados da BU-A (latente até a O1.6 permitir vínculos multi-BU). Corrigido: o resolver agora filtra pela BU da sessão.
- **MEDIUM — invariante satisfeita por Perfil administrativo com zero usuários.** Bastava criar um Perfil "Temp" com `Perfil.Gerenciar` e ninguém vinculado para a invariante autorizar remover a permissão do Perfil realmente em uso — e o Bootstrap nunca reabre, então a recuperação exigiria SQL direto. Corrigido: a invariante agora exige ao menos um usuário vinculado.
- **LOW — CSRF opt-in por rota.** Corrigido: filtro no nível do grupo, ciente de métodos seguros.

**Ressalvas remanescentes (aceitas como diagnóstico, nenhuma corrigida silenciosamente) — dependem de decisão do Product Owner:**
1. **MEDIUM — checagem de invariante não serializada com a escrita.** Duas requisições concorrentes inativando os dois últimos Perfis administrativos podem, em teoria, passar ambas. Correção adequada: transação serializável ou `RowVersion` em `Perfil` (padrão já usado em `BootstrapEstado`). Não corrigido por exigir migration e mudança de padrão transacional, fora do que a Work Order previa.
2. **LOW — backfill do catálogo é por nome de Perfil** ("Administrador Sênior") e vale para todas as Unidades de Negócio, em vez de usar o Id registrado em `BootstrapEstado`. **Não corrigido deliberadamente**: a migration já foi aplicada ao banco, e editar migration aplicada recriaria exatamente o tipo de drift histórico que a O1.4.3.1 teve de reconciliar.
3. **LOW — nenhuma auditoria de alterações de RBAC**, embora `ComprasFuncional.md` exija registro append-only de toda alteração de Perfil/Permissão.
4. **LOW — sem rate limiting** no grupo administrativo (as rotas de `/auth` têm).
5. **INFORMATIONAL — `ClaimTypes.Role` fixo em "Buyer"** e, em Development, vindo de header. Nenhuma decisão de autorização usa role (nenhum `RequireRole`/`IsInRole` no backend) — resíduo, não vetor.
6. **INFORMATIONAL — `RequestIdentity.Permissoes` documentado como defesa em profundidade, mas nenhum caso de uso o lê**: a policy é a única checagem.
7. **INFORMATIONAL — testes de pipeline usam endpoints `/probe-*` sintéticos**; não detectam a remoção de `.RequireAuthorization(...)` do controller real.
8. **INFORMATIONAL — `Fornecedor.*` e `Pedido.*` estão no catálogo mas nenhum endpoint os exige.** Os endpoints de Fornecedores/Negociações seguem protegidos apenas por autenticação: **a decisão D2 não está satisfeita para essas superfícies**, o que está fora do escopo declarado da O1.5.

### Pendências e limitações honestas

- **Enforcement com sessão real de um usuário sem permissão não foi validado manualmente**: exigiria criar um segundo usuário, e a API de Usuários só existe na O1.6. O cenário está coberto por teste automatizado de pipeline HTTP real (401/403/200 reais) e pelo 403 real obtido via esquema de Development.
- **Progresso técnico da Onda 1 mantido em 17%** durante a execução, deliberadamente: a regra oficial só conta entregável "Concluído", e a O1.5 não estava formalmente concluída (aguardava aceite das ressalvas). Nenhum percentual foi inflado por abertura ou por implementação de sprint. *(Recalculado no fechamento de 11/08/2026 — ver "Encerramento formal da Sprint O1.5" ao final deste documento.)*
- **O comando `[atualizar dashboard]` NÃO foi executado**: sua rotina exige atualização do workflow n8n e validação da URL publicada, fora do alcance desta sessão. `DASHBOARD_STATE.md` recebeu apenas atualização documental mínima das observações dos entregáveis #9/#17, sem alterar status nem percentual.
- Resíduo de smoke test no banco de desenvolvimento: quatro Perfis **inativos** (`Analista (O1.5 smoke)`, `Aprovador (smoke UI)`, `Pos-hardening`, `Verificacao pos-hardening`). Não removidos porque o modelo não tem exclusão física, por decisão de produto.
- Commit/push: commits criados **localmente**; **push NÃO realizado**, pendente de autorização explícita do Product Owner.

### O1.6 não foi iniciada

`O1.6-GestaoDeUsuariosBackendReal.md` permanece em `.ai/work-orders/backlog/`, status Draft/Planejada. Nenhum escopo de O1.6 ou de sprints posteriores foi antecipado.

---

## Encerramento formal da Sprint O1.5 — aceite das ressalvas pelo Product Owner (11/08/2026)

**Natureza desta sessão:** exclusivamente documental/governança. Nenhum código funcional (backend `.cs`, frontend `.ts`/`.tsx`), migration, configuração, banco de dados ou dado de banco foi alterado. Nenhuma sprint nova foi iniciada.

### Aceite formal do Product Owner (Julio Cesar, 11/08/2026)

O Product Owner analisou o relatório final da sprint e registrou seis decisões formais:

1. **Ressalvas remanescentes ACEITAS** — parecer APROVADA COM RESSALVAS aceito, com 0 CRITICAL / 0 HIGH remanescentes (HIGH e MEDIUM apontados foram corrigidos na própria sprint, com teste de regressão). O aceite **não** remove nem oculta as pendências: O1.5-M1, O1.5-L1..L3 e O1.5-I1..I4 permanecem **explicitamente rastreadas e abertas** em `.ai/BACKLOG.md`.
2. **Enforcement de `Fornecedor.*` e `Pedido.*` fica FORA da O1.5** — escopo não expandido agora; pendência mantida formalmente rastreada como **O1.5-I4** (D2 da ADR-0021 satisfeita apenas parcialmente para essas superfícies).
3. **Migration `20260811143355_AddRbacPerfilPermissaoCatalogo` ACEITA** como já aplicada ao banco de desenvolvimento compartilhado `MaisCompras`. **Produção não foi tocada.**
4. **Cobertura automatizada do cenário "usuário autenticado sem permissões" ACEITA como suficiente** — nenhum usuário artificial foi criado para repetir o cenário manualmente; a **O1.6 não foi iniciada**.
5. **Os quatro Perfis inativos de smoke test PERMANECEM no banco de desenvolvimento** (`Analista (O1.5 smoke)`, `Aprovador (smoke UI)`, `Pos-hardening`, `Verificacao pos-hardening`), por serem dados técnicos reutilizáveis em testes futuros. **Não removidos**; **nenhuma exclusão física criada**; **nenhuma migration de limpeza criada**. Registrados apenas como dados de teste existentes no ambiente. O saneamento é responsabilidade de uma **atividade futura anterior à promoção para HOMOLOGAÇÃO/REVIEW** — **não** pertence à O1.5 e **não** foi executada agora.
6. **Pendências de produto seguem PENDENTES** — catálogo definitivo de Perfis de negócio e nomenclatura de `CentroCusto.Acessar` **não** foram decididos nem implementados.

### Sprint O1.5 — FORMALMENTE CONCLUÍDA

Work Order movida de `.ai/work-orders/active/` para **`.ai/work-orders/completed/O1.5-RbacReal.md`**. Todos os cinco critérios de aceite marcados como atendidos, com nota explícita sobre as ressalvas aceitas (ver tabela de critérios na Work Order). Este aceite **não** constitui autorização para promoção a Homologação — os gates de Homologação já registrados permanecem válidos.

### Recálculo do Progresso Técnico da Onda 1 (regra documental aplicada)

**Regra-fonte:** `.ai/dashboard/DASHBOARD_STATE.md`, seção "Política dos percentuais" — Progresso Técnico de uma Onda = (entregáveis com status "Concluído" ÷ total de entregáveis), somando-se fração de entregáveis "Em desenvolvimento" **apenas** quando houver percentual individual explicitamente registrado (sem percentual individual, contribuem 0 — nunca estimado).

Duas reclassificações de entregável, ambas com regra documental prévia e explícita:

- **#17 "Perfis, papéis e permissões": Em desenvolvimento → Concluído.** A observação do próprio `DASHBOARD_STATE.md` registrava que o item "permanece 'Em desenvolvimento' **porque a O1.5 aguarda o aceite formal das ressalvas** da Security Validation independente" — e a mesma condição estava registrada em `.ai/PROJECT_STATE.md` e nesta `CURRENT_SPRINT.md`. Essa era a **única** condição pendente registrada, e ela foi satisfeita pelo aceite formal de 11/08/2026. RBAC real, persistido, com enforcement comprovado (401/403/200) em teste de pipeline HTTP e smoke test real.
- **#9 "Perfis de usuário simulados": Planejado → Em desenvolvimento.** Aplica-se exatamente a reclassificação já **recomendada por escrito** no `DASHBOARD_STATE.md` de 11/08/2026, incluindo o efeito ali previsto. Sem percentual individual registrado, **contribui 0** ao Progresso Técnico — nenhum progresso parcial foi estimado.
- **#11 "Módulo de Administração" permanece "Em desenvolvimento"** — apenas `profiles` deixou de ser mockada; `users`, `branches`, `cost-centers` e `allocation-units` seguem mockadas (O1.6–O1.8).

| Métrica | Antes do fechamento | Depois do fechamento |
|---|---|---|
| Total de entregáveis da Onda 1 | 41 | 41 (nenhum criado, retirado, absorvido ou substituído) |
| Concluído | 7 | **8** (+#17) |
| Em desenvolvimento | 11 | **11** (+#9, −#17) |
| Planejado | 23 | **22** (−#9) |
| Progresso Técnico | 17% (17,07% exato) | **20%** (19,51% exato = 8 ÷ 41) |
| Contribuição da Onda 1 ao MVP | 3,4 pontos | **3,9 pontos** (20% × 19,51%) |
| Percentual Global do MVP 1.0 | 30,41% (exibido 30%) | **30,90%** (exibido **31%**) |

### O1.6 continua NÃO iniciada

`O1.6-GestaoDeUsuariosBackendReal.md` permanece em `.ai/work-orders/backlog/`, status Draft/Planejada — **não** movida, **não** aberta, **não** implementada. É apenas a próxima candidata do caminho crítico (O1.5 → O1.6 → …).

### Estado final: NENHUMA SPRINT ATIVA

`.ai/work-orders/active/` está **vazio** (somente `.gitkeep`). Não há Work Order aprovada/em execução. A próxima frente de trabalho depende de decisão e autorização explícitas do Product Owner.

**Ressalva de escopo sobre o Dashboard:** o comando permanente `[atualizar dashboard]` **não** foi executado neste fechamento — sua rotina exige também atualizar o workflow n8n e validar a URL publicada, o que não foi feito. O `DASHBOARD_STATE.md` foi atualizado como parte deste fechamento documental autorizado pelo Product Owner (a própria nota de cabeçalho do documento admite edição por Work Order explícita que a autorize); o Dashboard HTML publicado permanece exibindo os valores anteriores até a próxima execução do comando.

---

## Abertura e execução da Sprint O1.6 — Usuários (Backend Real) — 11/08/2026

Work Order movida de `.ai/work-orders/backlog/` para `.ai/work-orders/active/` e, na mesma sessão, executada integralmente e encerrada — ver seção seguinte. Data de aprovação preenchida como 11/08/2026. Nenhum percentual técnico foi alterado apenas pela abertura.

## Encerramento formal da Sprint O1.6 — Usuários (Backend Real) — 11/08/2026

**Objetivo:** substituir o mock de `administration/users` por backend e persistência reais, com vínculo de Perfis (O1.5) e Centros de Custo, flag "Acesso a todos" e a regra do Administrador Sênior (D1, ADR-0021).

### Implementação

- **Backend** (`BlueprintOS.Domain/Application/Infrastructure/Api`, projeto Identity): `Usuario` estendido com `TodosCentrosCusto`/`CriadoEm`/`AtualizadoEm` e comportamentos `Atualizar`/`Ativar`/`Inativar`; `IUsuarioRepository`/`UsuarioRepository` estendidos com listagem, leitura, vínculos (`UsuariosPerfis`/`UsuariosCentrosCusto`, ambos já existentes desde O1.4.2) e contagem de Administradores Sênior ativos; `IPerfilRepository` estendido com leitura em lote por Ids e Unidade de Negócio; casos de uso novos em `UsuarioUseCases.cs` (Criar/Atualizar/AlterarStatus/Listar/Obter); `UsuariosController` (Api/Administration), mesmo padrão físico e de enforcement de `PerfisController` (O1.5) — policy `permissao:Usuario.Gerenciar`, CSRF no grupo, 401/403/404/409 tratados explicitamente.
- **Migration** `20260811165339_AddUsuarioGestaoO16`: colunas novas em `Usuarios`; FK de `UsuariosCentrosCusto` → `Usuarios` (ausente desde a criação da tabela em O1.4.2); backfill de auditoria (mesmo padrão da migration O1.5). Aplicada ao banco de desenvolvimento `MaisCompras`; `has-pending-model-changes` limpo.
- **Frontend** (`administration/users`): `usuariosMockApi.ts` excluído; `usuariosApi.ts` novo (cliente HTTP real, mesmo padrão de `perfisApi.ts`); tipos, hook (`useUsuarios`, com estado `acessoNegado`), componentes (`UsuarioTable`, `PerfisResumo`, `UsuarioForm`, `ConfirmToggleAtivoUsuarioModal`) e páginas ajustados para a forma real do `UsuarioDto` (perfis como `{id, nome, ativo}[]`, `ativo: boolean` em vez de `status` textual); e-mail não editável após a criação.
- **Não-escalonamento de privilégio estendido ao vínculo:** um ator sem uma permissão não pode vinculá-la a nenhum usuário via Perfil, mesmo possuindo `Usuario.Gerenciar` — sem esta checagem, `Usuario.Gerenciar` seria um caminho indireto para qualquer permissão do sistema.
- **Regra do Administrador Sênior:** reaproveita `AdministradorSeniorInvariantService` (O1.4.3.2) sem duplicar a lógica; bloqueia com 409 a inativação que deixaria a Unidade de Negócio sem nenhum Administrador Sênior ativo, escopada corretamente por Unidade de Negócio.

### Testes

- Backend: **493 aprovados** (488 unitários + 5 integração; baseline O1.5 477 → +16 novos em `UsuarioUseCasesTests.cs`), 0 falhas. `dotnet build` 0 erros/0 avisos.
- Frontend: **67 aprovados** (baseline O1.5 61 → +6 líquidos, com 10 testes novos de integração HTTP real substituindo os 4 do mock), 0 falhas. `tsc -b`/`vite build` limpos.

### Smoke test real (Chrome DevTools MCP, backend + frontend + SQL Server de dev)

Login OTP real como `julio.cesar@somagrupo.com.br` (Administrador Sênior criado pelo Bootstrap) via `GET /dev/otp` (exclusivo de Development). Fluxo completo executado e aprovado:

1. Listagem real de Usuários (`GET /api/administracao/usuarios`) — usuário "Julio Cesar" exibido com 1 Perfil, 0 Centro de Custo, Ativo.
2. Criação de usuário "Maria Teste O1.6" vinculando o Perfil "Analista (O1.5 smoke)" e o Centro de Custo `CC-001` — `POST /api/administracao/usuarios` → 201, refletido imediatamente na listagem (1 Perfil, 1 Centro de Custo).
3. Inativação do usuário criado (`PATCH .../status` `{ativo: false}`) → 200, status "Inativo" na interface.
4. Reativação do mesmo usuário → 200, status "Ativo".
5. **Tentativa de inativar "Julio Cesar" (único Administrador Sênior ativo)** → **409 real do backend**, mensagem "A operação deixaria a Unidade de Negócio sem nenhum Administrador Sênior ativo." exibida na interface; usuário permanece Ativo. Confirma a regra do Administrador Sênior (D1, ADR-0021) end-to-end.

Massa de teste ("Maria Teste O1.6") **permanece no banco de desenvolvimento**, mesmo precedente de dados técnicos de smoke test aceito na O1.5 — não removida, sem exclusão física criada, sem migration de limpeza.

### Revisão de segurança

Revisão própria (não houve Security Validation independente dedicada nesta sprint — dívida não bloqueante registrada em `.ai/BACKLOG.md`, O1.6-M1). Nenhum achado CRITICAL/HIGH. Pontos verificados: enforcement real (não apenas schema) via policy `Usuario.Gerenciar`; escopo por Unidade de Negócio em toda leitura/escrita; não-escalonamento de privilégio no vínculo de Perfil; regra do Administrador Sênior escopada corretamente por Unidade de Negócio (testada inclusive contra um segundo Administrador Sênior ativo em outra Unidade de Negócio, que não deve contar como salvaguarda); e-mail imutável após a criação; ausência de exclusão física.

### Reconciliação dos entregáveis oficiais da Onda 1

Entregáveis **#15 "Usuários"** e **#16 "Usuário por Unidade de Negócio"** passam de **"Em desenvolvimento"**/**"Planejado"** para **"Concluído"**: o mock de `administration/users` foi substituído por persistência real, com `Usuario.UnidadeNegocioId` como escopo obrigatório de toda leitura/escrita (isolamento entre Unidades de Negócio comprovado por teste), satisfazendo ambos os entregáveis. Nenhum outro entregável foi alterado por esta sprint. **Nota:** por instrução expressa do Product Owner nesta sessão, o arquivo `.ai/dashboard/DASHBOARD_STATE.md` **não** foi editado — a reconciliação e o recálculo abaixo são registrados apenas nesta CURRENT_SPRINT.md, em PROJECT_STATE.md e em BACKLOG.md; a atualização do Dashboard oficial permanece com o Product Owner (rotina `[atualizar dashboard]`, não executada).

**Recálculo do Progresso Técnico da Onda 1** (metodologia oficial de `DASHBOARD_STATE.md`, "Política dos percentuais" — só entregável "Concluído" conta; "Em desenvolvimento" sem percentual individual contribui 0):

| Métrica | Antes (fechamento O1.5) | Depois (fechamento O1.6) |
|---|---|---|
| Total de entregáveis da Onda 1 | 41 | 41 (inalterado) |
| Concluído | 8 | **10** (+#15, +#16) |
| Em desenvolvimento | 11 | **10** (−#15) |
| Planejado | 22 | **21** (−#16) |
| Progresso Técnico | 20% (19,5122% exato) | **24%** (10 ÷ 41 = 24,3902% exato) |
| Contribuição da Onda 1 ao MVP | 3,9 pontos | **4,9 pontos** (20% × 24,3902%) |
| Percentual Global do MVP 1.0 | 30,90% (exibido 31%) | **31,90%** exato (Foundation 20,0 + Onda 1 4,88 + Onda 2 7,0), exibido **32%** |

### Estado final: NENHUMA SPRINT ATIVA

`.ai/work-orders/active/` está vazio. `O1.6-GestaoDeUsuariosBackendReal.md` movida para `.ai/work-orders/completed/`. **`O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md` permanece em `.ai/work-orders/backlog/`, status Draft/Planejada — não iniciada.**

**Ressalva de escopo sobre o Dashboard:** por instrução expressa do Product Owner nesta sessão, a rotina `[atualizar dashboard]` **não** foi executada e `dashboard/DASHBOARD_STATE.md` **não** foi tocado manualmente. O Dashboard publicado permanece exibindo os valores do fechamento da O1.5 até que o Product Owner execute o comando.

---

## Abertura e execução da Sprint O1.7 — Filiais e Centros de Custo Integrados ao ERP — 11/08/2026

Work Order movida de `.ai/work-orders/backlog/` para `.ai/work-orders/active/` e, na mesma sessão, executada integralmente e encerrada — ver seção seguinte. Status atualizado para "Ativa (em execução)"; data de aprovação preenchida como 11/08/2026. Nenhum percentual técnico foi alterado apenas pela abertura.

## Encerramento formal da Sprint O1.7 — Filiais e Centros de Custo Integrados ao ERP — 11/08/2026

**Objetivo:** implementar integração ERP real de Filiais e Centros de Custo (D3, ADR-0021), com o ERP permanecendo fonte canônica e o +Compras mantendo apenas metadados locais (Descrição, ativação/inativação), resolvendo também a dívida O1.6-L2 (vínculo Usuário×Centro de Custo por código ERP em texto, sem validação real).

### Descoberta técnica

Confirmado por leitura da Work Order, ADR-0020/ADR-0021 e código: `administration/branches`/`cost-centers` eram 100% mock em ambas as pontas (frontend com `filiaisMockApi.ts`/`centrosCustoMockApi.ts`; nenhum backend/persistência/migration para Filial/Centro de Custo). O catálogo RBAC (O1.5) já reservava `PermissaoCatalogo.FilialGerenciar`/`CentroCustoGerenciar` (Guids estáveis, nenhuma migration de permissão nova necessária). Padrão de referência para os readers ERP: `IFornecedorErpReader`/`SomaFornecedorReader` (B2.1/B2.1.2) — introspecção dinâmica de schema via `INFORMATION_SCHEMA.COLUMNS`, paginação `OFFSET/FETCH`, validação de `InitialCatalog == SOMA_DESENV`, teste de integração com early-return quando `ErpConnection` não está configurada.

### Implementação

- **Backend** (`BlueprintOS.Domain/Application/Infrastructure/Api`): `IFilialErpReader`/`SomaFilialReader` e `ICentroCustoErpReader`/`SomaCentroCustoReader` (introspecção dinâmica, mesmo padrão de `SomaFornecedorReader`); entidades de metadados locais `FilialMetadado`/`CentroCustoMetadado` (`DescricaoMaisCompras` opcional, `AtivoNoMaisCompras`, `UnidadeNegocioId`); repositórios EF (`IFilialMetadadoRepository`/`ICentroCustoMetadadoRepository`); use cases `Listar`/`AtualizarMetadado` para cada domínio (join em memória ERP×metadados locais; código ERP sem metadado local é considerado Ativo por padrão); `FiliaisController`/`CentrosCustoController` (Api/Administration), mesmo padrão físico/enforcement de `PerfisController`/`UsuariosController` — policy `permissao:Filial.Gerenciar`/`CentroCusto.Gerenciar`, CSRF no grupo. Nenhum endpoint de criação/edição/exclusão do dado ERP — apenas leitura combinada e atualização de metadados locais.
- **Migration** `20260811173904_AddFilialCentroCustoMetadadosO17`: tabelas `FiliaisMetadados` (índice único por `UnidadeNegocioId`+`CodigoErp` — cada Unidade de Negócio pode ter seu próprio metadado local para a mesma Filial) e `CentrosCustoMetadados` (índice único **global** por `CodigoErp` — um Centro de Custo só pode estar ancorado a **uma** Unidade de Negócio por vez, fechando deliberadamente o vetor cross-BU do vínculo Usuário×Centro de Custo). Gerada com `dotnet ef migrations add`; **não aplicada** ao banco (sem VPN/SQL Server corporativo disponível nesta sessão — geração de migration não exigiu conexão real).
- **Resolução da dívida O1.6-L2:** nova abstração `ICentroCustoVinculoValidator`/`CentroCustoVinculoValidator` (Infrastructure), injetada em `CriarUsuarioUseCase`/`AtualizarUsuarioUseCase`. Para cada código informado no vínculo Usuário×Centro de Custo: se já ancorado a outra Unidade de Negócio → rejeitado (`RbacFalha.CentroCustoInvalido`); se inexistente no ERP → rejeitado; se existente e ainda não ancorado → cria o `CentroCustoMetadado` "sob demanda", ancorado à Unidade de Negócio do ator. Decisão explícita: validação em tempo de execução no caso de uso em vez de FK física em `UsuariosCentrosCusto` — a FK exigiria que o metadado já existisse antes do primeiro vínculo, o que não é garantido; a validação sob demanda evita impor uma ordem de operações artificial ao usuário final, com a integridade garantida pelo índice único global.
- **Frontend** (`administration/branches`/`cost-centers`): `filiaisMockApi.ts`/`centrosCustoMockApi.ts` excluídos; `filiaisApi.ts`/`centrosCustoApi.ts` novos (clientes HTTP reais, mesmo padrão de `usuariosApi.ts`); tipos/hooks/páginas ajustados para a forma real dos DTOs (`temMetadadoLocal`, `id` sempre igual ao código ERP); nenhum redesign.
- **Correção de regressão identificada em revisão pós-implementação (ainda dentro desta sprint):** `administration/users` (formulário de vínculo Usuário×Centro de Custo) consumia `services/costCenterCatalog.ts`, catálogo mockado local (`cc-001`…`cc-005`) — esses códigos seriam rejeitados pelo novo `ICentroCustoVinculoValidator` real, quebrando a criação/edição de usuário com Centro de Custo específico. `costCenterCatalog.ts` removido; `UsuarioFormPage`/`UsuarioForm`/`UsuarioDetalhesPage` passaram a consumir `GET /api/administracao/centros-custo` real (registrado como O1.7-L2, **Resolvida**, em `.ai/BACKLOG.md`).
- **Correção de concorrência identificada em revisão pós-implementação (ainda dentro desta sprint):** a corrida entre duas requisições concorrentes ancorando o mesmo Centro de Custo pela primeira vez (vínculo de usuário, ou edição de metadado por duas Unidades de Negócio diferentes) resultaria em `DbUpdateException` não tratada (500) ao violar o índice único global — a integridade dos dados nunca esteve em risco, mas o erro não era traduzido para uma resposta de negócio limpa. `CentroCustoMetadadoRepository.SalvarAlteracoesAsync` agora traduz a violação para `DuplicateRecordException`; `CentroCustoVinculoValidator` e o novo `AtualizarMetadadoCentroCustoUseCase` (verificação prévia de âncora por outra Unidade de Negócio) capturam essa exceção e retornam `RbacFalha.CentroCustoInvalido`/`ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio` (novo, mapeado para HTTP 409 em `CentrosCustoController`). Registrado como O1.7-M1, **Resolvida**, em `.ai/BACKLOG.md`.

### Testes

- Backend: **500 aprovados** (499 unitários + 1 novo teste de regressão cross-BU; baseline O1.6 493 → +7 novos em `FilialCentroCustoUseCasesTests.cs`) + **7 integração** (early-return sem VPN/`ErpConnection`), 0 falhas. `dotnet build` 0 erros/0 avisos.
- Frontend: **68 aprovados** (baseline O1.6 67 → +1 líquido, com reescrita completa dos testes de Filiais/Centros de Custo para interceptar `fetch` real e ajuste do teste de `administration/users` para consumir o endpoint real de Centros de Custo), 0 falhas. `tsc -b`/`vite build` limpos.

### Decisão sobre uso do Chrome/MCP

**Dispensado.** A cobertura automatizada (testes unitários dos use cases/validador, incluindo o cenário cross-BU e a corrida de concorrência; testes de integração HTTP real do frontend simulando 401/403/200; builds limpos de backend e frontend) foi considerada suficiente para os critérios de aceite da Work Order. Nenhum comportamento visual/interativo introduzido exigia validação manual, e o fluxo de autenticação/autorização não foi alterado por esta sprint. Smoke test real contra o ERP `SOMA_DESENV` (listar Filiais/Centros de Custo reais, ativar/inativar localmente) **não foi executado** — ambiente sem VPN corporativa disponível nesta sessão, mesma dependência ambiental já registrada em B2.1.3 e na própria Work Order (seção "Riscos").

### Segurança

Revisão própria (não houve Security Validation independente dedicada — mesma dívida não bloqueante já registrada em O1.6-M1). Revisão de código pós-implementação (multi-dimensional: correção, segurança, simplificação, reuso, eficiência) identificou e corrigiu, ainda dentro desta sprint, os dois achados de maior relevância (regressão de `administration/users` e corrida de concorrência, ambos acima). Pontos verificados sem achado CRITICAL/HIGH remanescente: enforcement real via policy `Filial.Gerenciar`/`CentroCusto.Gerenciar`; nenhum endpoint de criação/edição/exclusão do dado ERP; escopo por Unidade de Negócio na autoridade de escrita dos metadados locais (Centro de Custo com âncora global única — não é possível duas Unidades de Negócio escreverem metadados conflitantes para o mesmo código; Filial com metadado único por Unidade de Negócio); vínculo Usuário×Centro de Custo validado contra o ERP real e cross-BU explicitamente rejeitado; ausência de exclusão física. Registrada como dívida não bloqueante (O1.7-I1, INFORMATIONAL) a decisão de produto ainda pendente sobre se a **leitura** do catálogo ERP de Filiais/Centros de Custo deve ser visível a todas as Unidades de Negócio (padrão atual, replicando o já usado para Fornecedores) ou filtrada por Unidade de Negócio de origem — sem risco, pois nenhuma escrita cross-BU é possível.

### Reconciliação dos entregáveis oficiais da Onda 1

Entregáveis **#14 "Empresas e filiais"** e **#18 "Centros de Custo"** passam de **"Em desenvolvimento"** para **"Concluído"**: os mocks de `administration/branches`/`cost-centers` foram substituídos por integração ERP real combinada com metadados locais reais, satisfazendo ambos os entregáveis. **#11 "Módulo de Administração"** permanece "Em desenvolvimento" (apenas `allocation-units` segue mockado, escopo da O1.8). Nenhum outro entregável foi alterado por esta sprint. **Nota:** por instrução expressa do Product Owner nesta sessão, o arquivo `.ai/dashboard/DASHBOARD_STATE.md` **não** foi editado — a reconciliação e o recálculo abaixo são registrados apenas nesta CURRENT_SPRINT.md, em PROJECT_STATE.md e em BACKLOG.md; a atualização do Dashboard oficial permanece com o Product Owner (rotina `[atualizar dashboard]`, não executada).

**Recálculo do Progresso Técnico da Onda 1** (metodologia oficial de `DASHBOARD_STATE.md`, "Política dos percentuais" — só entregável "Concluído" conta; "Em desenvolvimento" sem percentual individual contribui 0):

| Métrica | Antes (fechamento O1.6) | Depois (fechamento O1.7) |
|---|---|---|
| Total de entregáveis da Onda 1 | 41 | 41 (inalterado) |
| Concluído | 10 | **12** (+#14, +#18) |
| Em desenvolvimento | 10 | **8** (−#14, −#18) |
| Planejado | 21 | **21** (inalterado) |
| Progresso Técnico | 24% (24,3902% exato) | **29%** (12 ÷ 41 = 29,2683% exato) |
| Contribuição da Onda 1 ao MVP | 4,9 pontos | **5,85 pontos** (20% × 29,2683%) |
| Percentual Global do MVP 1.0 | 31,90% exato, exibido 32% | **32,85%** exato (Foundation 20,0 + Onda 1 5,85 + Onda 2 7,0), exibido **33%** |

### Estado final: NENHUMA SPRINT ATIVA

`.ai/work-orders/active/` está vazio. `O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md` movida para `.ai/work-orders/completed/`. **`O1.8-UnidadesDeAlocacaoPersistenciaReal.md` permanece em `.ai/work-orders/backlog/`, status Draft/Planejada — não iniciada.**

**Ressalva de escopo sobre o Dashboard:** por instrução expressa do Product Owner nesta sessão, a rotina `[atualizar dashboard]` **não** foi executada e `dashboard/DASHBOARD_STATE.md` **não** foi tocado manualmente. O Dashboard publicado permanece exibindo os valores do fechamento da O1.6 até que o Product Owner execute o comando.

## Abertura e execução da Sprint O1.8 — Unidades de Alocação, Persistência Real — 11/08/2026

Work Order movida de `.ai/work-orders/backlog/` para `.ai/work-orders/active/` e, na mesma sessão, executada integralmente e encerrada — ver seção seguinte. Status atualizado para "Concluída"; data de aprovação preenchida como 11/08/2026. Nenhum percentual técnico foi alterado apenas pela abertura.

## Encerramento formal da Sprint O1.8 — Unidades de Alocação, Persistência Real — 11/08/2026

### Objetivo canônico

Substituir o CRUD mockado de `administration/allocation-units` por domínio e persistência reais, mantendo Unidade de Alocação exclusiva do +Compras, sem integração ERP (ADR-0020, item 4) e sem o relacionamento N:N com Centro de Custo (ADR-0020, item 5 — escopo explícito da O1.9, fora de escopo aqui).

### Implementação

- **Backend** (`BlueprintOS.Domain/Application/Infrastructure/Api.Identity`): entidade `UnidadeAlocacao` (Nome, Descrição, `UnidadeNegocioId`, Status), mesmo padrão físico de `Usuario` (O1.6) — construtor de fábrica, mutadores controlados (`Atualizar`/`Ativar`/`Inativar`), sem exclusão física. `UnidadeAlocacaoConfiguration` (EF Core, índice único `UnidadeNegocioId`+`Nome`); `IUnidadeAlocacaoRepository`/`UnidadeAlocacaoRepository`; casos de uso `Listar/Obter/Criar/Atualizar/AlterarStatusUnidadeAlocacaoUseCase`; `UnidadesAlocacaoController` (Api/Administration), mesmo padrão físico/enforcement de `UsuariosController` — policy `UnidadeAlocacao.Gerenciar` (permissão já reservada no catálogo desde a O1.5), CSRF no grupo, `UnidadeNegocioId` sempre resolvido da sessão autenticada, nunca do payload.
- **Migration** `AddUnidadeAlocacaoO18`: `CREATE TABLE UnidadesAlocacao` + índice único `(UnidadeNegocioId, Nome)`. Gerada via `dotnet ef migrations add` real e **aplicada** ao banco de desenvolvimento via `dotnet ef database update` (ambiente com conectividade disponível nesta sessão — junto com as migrations O1.6/O1.7 que ainda estavam pendentes de aplicação).
- **Frontend** (`administration/allocation-units`): `unidadesAlocacaoMockApi.ts` excluído; `unidadesAlocacaoApi.ts` novo (cliente HTTP real, mesmo padrão de `centrosCustoApi.ts`/O1.7); tipo `UnidadeAlocacao.unidadeNegocioId` (era `unidadeNegocio`, texto livre no mock); campo "Unidade de Negócio" removido do formulário (`UnidadeAlocacaoForm`) e da tabela/detalhes — correção necessária identificada durante a conversão: a Unidade de Negócio é sempre resolvida pelo backend a partir da sessão, nunca escolhida pelo cliente (mesmo cuidado de Usuário/Perfil), não um redesign. Nenhuma outra alteração visual.

### Testes

- Backend: **512 aprovados** (baseline O1.7 500 → +13 novos em `UnidadeAlocacaoUseCasesTests.cs`: criação, unicidade por Unidade de Negócio, isolamento cross-BU, ativação/inativação, listagem escopada) + **7 integração**, 0 falhas. `dotnet build` 0 erros/0 avisos.
- Frontend: **72 aprovados** (baseline O1.7 68 → +4 líquido, com reescrita completa do teste de `UnidadesAlocacaoPage` para interceptar `fetch` real em vez do mock em memória), 0 falhas. `tsc -b`/`vite build` limpos.

### Decisão sobre uso do Chrome/MCP

**Dispensado.** A cobertura automatizada (testes unitários dos casos de uso, incluindo unicidade por Unidade de Negócio e isolamento cross-BU; testes de integração HTTP real do frontend simulando 401/403/200/409; builds limpos de backend e frontend) foi considerada suficiente para os critérios de aceite da Work Order. Nenhuma interação visual complexa foi introduzida (sem vínculo com Centro de Custo nesta sprint) e nenhum comportamento exigia validação manual.

### Segurança

Revisão proporcional ao risco (sem Security Validation independente dedicada). Pontos verificados sem achado CRITICAL/HIGH: enforcement real via policy `UnidadeAlocacao.Gerenciar` (nunca por nome de Perfil); `UnidadeNegocioId` sempre da sessão, nunca do payload (impede vínculo/leitura/edição/ativação cross-BU, coberto por teste); DTOs de entrada (`UnidadeAlocacaoUpsertRequest`) expõem apenas Nome/Descrição — sem mass assignment de Id/UnidadeNegocioId/Status; ativação/inativação em endpoint dedicado, nunca junto do upsert; migration aditiva, sem exclusão de dados. Registrada como nota não bloqueante: ausência de token de concorrência otimista (`RowVersion`) em `UnidadeAlocacao` — mesmo padrão já vigente em `Usuario`/`Perfil`, não é uma regressão introduzida por esta sprint.

### Reconciliação dos entregáveis oficiais da Onda 1

Entregável **#19 "Unidades de Alocação"** passa de **"Em desenvolvimento"** para **"Concluído"**: o mock de `administration/allocation-units` foi substituído por backend/persistência reais. Nenhum outro entregável foi alterado por esta sprint. **Nota:** por instrução expressa do Product Owner, o arquivo `.ai/dashboard/DASHBOARD_STATE.md` **não** foi editado — a reconciliação e o recálculo abaixo são registrados apenas nesta CURRENT_SPRINT.md, em PROJECT_STATE.md e em BACKLOG.md; a atualização do Dashboard oficial permanece com o Product Owner (rotina `[atualizar dashboard]`, não executada).

**Recálculo do Progresso Técnico da Onda 1** (metodologia oficial de `DASHBOARD_STATE.md`, "Política dos percentuais" — só entregável "Concluído" conta; "Em desenvolvimento" sem percentual individual contribui 0):

| Métrica | Antes (fechamento O1.7) | Depois (fechamento O1.8) |
|---|---|---|
| Total de entregáveis da Onda 1 | 41 | 41 (inalterado) |
| Concluído | 12 | **13** (+#19) |
| Em desenvolvimento | 8 | **7** (−#19) |
| Planejado | 21 | **21** (inalterado) |
| Progresso Técnico | 29% (29,2683% exato) | **32%** (13 ÷ 41 = 31,7073% exato) |
| Contribuição da Onda 1 ao MVP | 5,85 pontos | **6,34 pontos** (20% × 31,7073%) |
| Percentual Global do MVP 1.0 | 32,85% exato, exibido 33% | **33,34%** exato (Foundation 20,0 + Onda 1 6,34 + Onda 2 7,0), exibido **33%** |

### Estado final: NENHUMA SPRINT ATIVA

`.ai/work-orders/active/` está vazio. `O1.8-UnidadesDeAlocacaoPersistenciaReal.md` movida para `.ai/work-orders/completed/`. **`O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md` permanece em `.ai/work-orders/backlog/`, status Draft/Planejada — não iniciada.**

**Ressalva de escopo sobre o Dashboard:** por instrução expressa do Product Owner, a rotina `[atualizar dashboard]` **não** foi executada e `dashboard/DASHBOARD_STATE.md` **não** foi tocado manualmente. O Dashboard publicado permanece D-1, atualizado pelo Product Owner ao final do dia.
