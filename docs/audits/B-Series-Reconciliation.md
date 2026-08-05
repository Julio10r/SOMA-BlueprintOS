# B-Series — Reconciliação de Status das Sprints (Procurement / +Compras)

> Auditoria de reconciliação documental. Escopo: A13 Vertical Slice e sprints B1, B2, B2.1, B2.1.1, B2.1.2, B2.1.3 do módulo Procurement/+Compras.
> Data: 02/08/2026. Branch auditada: `feature/a13-procurement-vertical-slice`.
> Regra desta auditoria: somente documentação. Nenhum código, migration, endpoint ou dado foi criado, alterado ou removido.

## Visão geral

O objetivo desta auditoria foi comparar o que a documentação oficial (`.ai/BACKLOG.md`, `.ai/CURRENT_SPRINT.md`, `.ai/PROJECT_STATE.md`, `.ai/ROADMAP.md`, `.ai/DECISIONS.md`, `docs/engineering/`) declara sobre as sprints B1 a B2.1.3 com o que realmente existe em `backend/src`, `backend/tests` e `backend/database`/migrations.

Conclusão geral: a documentação já estava, no geral, bem alinhada com o código — as sprints B1, B2, B2.1.2 e B2.1.3 têm evidência real de implementação (entidades, endpoints, migrations e testes existem exatamente como descrito). A principal lacuna encontrada não é "documentação otimista demais", mas sim **desatualização pontual**: dois bugs reais de paginação foram descobertos e corrigidos na sprint B2.1.3 *depois* que a documentação declarou a sprint concluída, e essas correções não haviam sido propagadas para `BACKLOG.md`, `PROJECT_STATE.md` e `ROADMAP.md`. Além disso, a tabela de qualidade (contagem de testes) em `PROJECT_STATE.md` está desatualizada desde antes da B2.1.3 e nunca foi confirmada por uma execução real de `dotnet test` neste histórico recente — nenhum ambiente disponível desde então teve SDK .NET instalado.

Nenhuma sprint foi encontrada "concluída no papel mas sem código" ou "código pronto sem nenhum registro". A distinção que a documentação já fazia entre "concluída em código" e "validação operacional real pendente (VPN/SQL Server corporativo)" se mostrou precisa e foi preservada/reforçada nesta reconciliação.

## Tabela de sprints

| Sprint | Status anterior (documentação) | Status real (código/testes) | Evidência |
|---|---|---|---|
| B1 — Cadastro e Perfil de Fornecedores | Concluída | **Confirmado.** Implementado e evidenciado. | `Fornecedor.cs` (Domain), `BlueprintOSDbContext` com `DbSet<Fornecedor>`, migration `202607300001_B1FornecedorPersistence.cs`, `FornecedorRepository`/`IFornecedorRepository` (CRUD completo), endpoints `POST/GET/PUT/DELETE /fornecedores` em `FornecedoresController.cs`, testes `FornecedorUseCasesTests` + `FornecedorRepositoryIntegrationTests`. |
| B2 — Descoberta Inicial de Fornecedores | Concluída (score é estrutura inicial) | **Confirmado.** Implementado e evidenciado. | `FornecedorDescoberto.cs`, `ScoreFornecedor.cs` (pesos `ItemExato=100/Familia=80/Categoria=60/Historico=40`), migration `202607300002_B2FornecedorDiscovery.cs`, endpoints `POST /api/fornecedores/descobrir`, `GET /api/fornecedores/descobertas[/{id}]`, testes `FornecedorDiscoveryUseCaseTests` + `FornecedorDiscoveryIntegrationTests`. |
| B2.1 — Validação Operacional e Sincronização com ERP | Concluída | **Confirmado o código; validação operacional real (VPN/SQL corporativo) segue sem evidência de execução.** | `SomaFornecedorReader.cs` valida `InitialCatalog == "SOMA_DESENV"`; `B1ConnectivityValidator.ValidateErpAsync()` existe mas sem registro de execução bem-sucedida no repositório; `docs/engineering/FornecedorErpSynchronization.md` e `.ai/BACKLOG.md` já documentam essa pendência corretamente. |
| B2.1.1 — Completar Mapeamento Canônico ERP → +Compras | Concluída | **Confirmado.** Mapeamento canônico presente em `FornecedorCanonico` e uso consistente nos readers/use cases. | Migrations `202608010001_B21CanonicalSupplierSynchronization.cs` e `202608010002_B212FornecedorLinxCanonicalModel.cs`. |
| B2.1.2 — Sincronização Inicial de Fornecedores com ERP | Concluída | **Confirmado.** Implementado e evidenciado. | `IFornecedorErpReader` (`.../Integrations/ERP/Contracts/`), `SomaFornecedorReader` (`.../Integrations/ERP/Soma/`), endpoint confirmado no código-fonte: `group.MapGet("/sincronizar-erp", SyncErp)` em `FornecedorSyncController.cs` → **`GET /api/fornecedores/sincronizar-erp`** (query params `businessUnit`, `limite`, `correlationId`). Registro DI confirmado em `ServiceCollectionExtensions.cs`. |
| B2.1.3 — Endurecimento da Integração ERP de Fornecedores | Concluída em código | **Confirmado o código, incluindo paginação, entidades e migration — mas com duas correções pós-entrega não refletidas na documentação até esta auditoria.** | `SincronizacaoFornecedor.cs`, `ErroSincronizacaoFornecedor.cs`, migration `202608020001_B213FornecedorErpSyncHardening.cs` (cria `SincronizacoesFornecedores` e `ErrosSincronizacoesFornecedores`), 6 testes em `SincronizarFornecedoresErpUseCaseTests.cs`. **Divergência corrigida nesta auditoria:** os bugs de paginação (ver seção abaixo) foram corrigidos em código (`21f1a67`, `ca48dc3`) mas não estavam registrados em `BACKLOG.md`/`PROJECT_STATE.md`/`ROADMAP.md` antes desta reconciliação. |

## Divergências encontradas

1. **B2.1.3 — correções de paginação não propagadas para `BACKLOG.md`, `PROJECT_STATE.md` e `ROADMAP.md`.**
   Dois bugs reais foram encontrados pelo teste `SincronizarFornecedoresErpUseCaseTests.Execute_Should_Process_Multiple_Batches_And_Calculate_Totals`, ambos no loop de paginação de `SincronizarFornecedoresErpUseCase.ExecuteAsync`:
   - Parada prematura quando o lote retornado era menor que o solicitado (`if (lote.Count < tamanhoLote) break;`), presumindo incorretamente que um lote parcial sempre significa "última página". Corrigido no commit `21f1a67`, mantendo a parada apenas em `lote.Count == 0`.
   - Cálculo não determinístico do offset (`skip += lote.Count` em vez de `skip += tamanhoLote`), que fazia a próxima página começar na posição errada sempre que um lote intermediário retornasse menos itens que o solicitado. Corrigido no commit `ca48dc3`.

   `.ai/CURRENT_SPRINT.md` já registrava a primeira correção, mas não a segunda até esta auditoria. `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md` e `.ai/ROADMAP.md` não mencionavam nenhuma das duas. **Ação tomada:** as três referências foram atualizadas nesta reconciliação, preservando o histórico anterior (nada foi apagado, apenas complementado).

2. **Tabela de qualidade em `PROJECT_STATE.md` desatualizada e não reverificável no momento.**
   A tabela declarava "269 unitários + 4 integração = 273, todos aprovados, 0 falhos" como se fosse o estado corrente. Essa contagem é anterior à B2.1.3 (que adicionou `SincronizarFornecedoresErpUseCaseTests`, 6 `[Fact]`, e um teste de integração condicionado a VPN). Mais relevante: sabemos por fato que a suíte teve pelo menos uma falha real (o teste de múltiplos lotes, duas vezes, antes de cada correção) — logo, a afirmação implícita de "0 falhos" não pode ser tratada como vigente sem uma nova execução real de `dotnet test`. Nenhum ambiente disponível desde a B2.1.3 teve SDK .NET instalado para reexecutar a suíte. **Ação tomada:** a tabela foi anotada como desatualizada/não reverificada, sem apagar os números históricos, e a estimativa por inspeção estática desta auditoria (~266 unitários + 5 integração ≈ 271 total) foi registrada ao lado, também como estimativa não confirmada — diverge da expectativa anterior de 280 testes mencionada em conversas recentes sobre a sprint.

3. **`ROADMAP.md` não mencionava a B2.1.3.**
   O parágrafo de "Estado real" da Fase 3 descrevia B2.1, B2.1.1, B2.1.2 e B2.2 em detalhe, mas parava antes da B2.1.3 (que é posterior à última edição desse parágrafo). **Ação tomada:** adicionada uma frase resumindo a B2.1.3 e suas duas correções pós-entrega, com referência a este relatório.

4. **Nenhuma inconsistência arquitetural relevante encontrada nas ADRs.**
   As ADRs relacionadas a fornecedores/ERP (ADR-0013, ADR-0015, ADR-0016) permanecem consistentes com o código atual; nenhuma decisão antiga contradiz a implementação real de B1/B2/B2.1.x. A ADR-0018 (Desenvolvimento Local, registrada em sessão anterior) também permanece consistente e não precisou de ajuste nesta auditoria.

5. **B2.1/B2.1.1 — validação operacional real permanece sem evidência de execução, como a documentação já indicava.**
   Não é uma divergência nova: `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md` e `docs/engineering/FornecedorErpSynchronization.md` já eram honestos sobre essa pendência (código pronto, execução real contra VPN/SQL Server corporativo ainda não comprovada no repositório). Esta auditoria apenas confirma que a lacuna continua real e não foi fechada.

## Registro de status (formato solicitado)

**B1**
Status: Concluída
Data: 30/07/2026 (persistência); reconciliado 02/08/2026
Entrega: Entidade `Fornecedor`, `BlueprintOSDbContext`, migration `202607300001_B1FornecedorPersistence`, `FornecedorRepository` (CRUD), endpoints `POST/GET/PUT/DELETE /fornecedores`.
Evidências: código, migration, testes unitários e de integração.
Testes: `FornecedorUseCasesTests` + `FornecedorRepositoryIntegrationTests` aprovados no último ciclo real de execução (anterior à B2.1.3; não reexecutado nesta auditoria).
Pendências: aplicação da migration em ambiente real segue pendente de autorização (já registrado em `BACKLOG.md`).
Próximo passo: nenhum específico de B1; acompanhar quando B3 depender de dados reais de fornecedor.

**B2**
Status: Concluída
Data: 30/07/2026; reconciliado 02/08/2026
Entrega: Descoberta somente leitura no ERP `SOMA_DESENV`, score explicável (`ScoreFornecedor`), persistência de descobertas.
Evidências: `FornecedorDescoberto`, `ScoreFornecedor`, migration `202607300002_B2FornecedorDiscovery`, endpoints de descoberta.
Testes: `FornecedorDiscoveryUseCaseTests` + `FornecedorDiscoveryIntegrationTests`.
Pendências: validação operacional real do ERP pendente de ambiente com VPN (já registrado).
Próximo passo: nenhum específico; score permanece estrutura inicial, sem bloquear entregas seguintes.

**B2.1**
Status: Concluída em código; validação operacional real pendente
Data: 01/08/2026; reconciliado 02/08/2026
Entrega: Sincronização bidirecional com contrato canônico, adaptadores por BU, regra temporal, inativação, idempotência, auditoria imutável.
Evidências: commits `b08769f`, `3b6d54b`; CLIFORs 315501/315502/315503/315505 confirmados no Linx (validação pontual já realizada anteriormente, distinta da pendência de VPN/ambiente completo).
Testes: cobertura unitária existente; teste de integração condicionado a VPN.
Pendências: execução completa contra ambiente corporativo real.
Próximo passo: nenhum específico; acompanhar junto com B2.1.3.

**B2.1.1**
Status: Concluída
Data: 01/08/2026; reconciliado 02/08/2026
Entrega: Mapeamento canônico ERP → +Compras completo (identificação, endereço, contato, banco, comercial, fiscal, fornecimento).
Evidências: commit `0240c35`; migrations `202608010001` e `202608010002`.
Testes: cobertos pela suíte de sincronização/canônico.
Pendências: nenhuma nova identificada nesta auditoria.
Próximo passo: nenhum específico.

**B2.1.2**
Status: Concluída
Data: 01/08/2026; reconciliado 02/08/2026
Entrega: `IFornecedorErpReader`, `SomaFornecedorReader`, `SincronizarFornecedoresErpUseCase` (versão inicial), endpoint `GET /api/fornecedores/sincronizar-erp`.
Evidências: commit `77861eb`; registro DI em `ServiceCollectionExtensions.cs`.
Testes: testes unitários e teste de integração condicionado à VPN/configuração.
Pendências: mesma validação operacional real de B2.1.
Próximo passo: nenhum específico; evoluiu para B2.1.3.

**B2.1.3**
Status: Concluída em código
Data: 02/08/2026; correções pós-sprint em 02/08/2026 (mesma data); reconciliado 02/08/2026
Entrega: Hardening da sincronização ERP — paginação real, processamento em lote, histórico de execução (`SincronizacaoFornecedor`), erros parciais persistidos (`ErroSincronizacaoFornecedor`), logs estruturados, migration `202608020001_B213FornecedorErpSyncHardening`.
Evidências: paginação (`skip`/`take`), rastreabilidade via entidades de execução, tratamento de erros parciais sem interromper o lote.
Testes: 6 `[Fact]` em `SincronizarFornecedoresErpUseCaseTests`; dois bugs de paginação encontrados e corrigidos (commits `21f1a67`, `ca48dc3`) — ver seção de divergências.
Pendências: `dotnet test` completo não executado desde a B2.1.3 (sem SDK .NET disponível no ambiente de revisão); validação operacional real (VPN/SQL Server corporativo) pendente.
Próximo passo: executar `dotnet test backend/BlueprintOS.sln` em ambiente local e confirmar 0 falhas; depois, avaliar início de B2.2.5 (se mantida no roadmap) ou B3.

## Próximos passos recomendados

1. Rodar `dotnet test backend/BlueprintOS.sln` em ambiente local (com SDK .NET) para confirmar a contagem real de testes e 0 falhas — nem os "273" históricos nem os "~271" estimados por esta auditoria devem ser tratados como confirmados até essa execução.
2. Validar operacionalmente B2.1/B2.1.1/B2.1.2/B2.1.3 contra o SQL Server corporativo real via VPN, incluindo o endpoint `GET /api/fornecedores/sincronizar-erp`.
3. Decidir se B2.2.5 (mencionada como "preservada caso siga no roadmap" em `BACKLOG.md`) deve ser formalmente aberta ou definitivamente descartada, para não deixar uma pendência ambígua no catálogo.
4. Após validação real, atualizar novamente `PROJECT_STATE.md` com a contagem definitiva de testes e remover a anotação de "tabela desatualizada".
