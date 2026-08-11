# RBAC Real — Perfis, Permissões, Policies e Enforcement (O1.5)

> Documentação técnica incremental do que foi **efetivamente implementado** pela Sprint O1.5 (11/08/2026).
> Fonte de decisão: ADR-0020 (itens 7, 8, 9, 10) e ADR-0021 (D1, D2). Esta sprint **implementa** essas
> decisões; não cria decisão arquitetural nova, e por isso não gerou ADR própria.

## 1. O que muda em relação ao estado anterior

Antes da O1.5, `Perfil`/`Permissao`/`PerfilPermissao`/`UsuarioPerfil` existiam como **tabelas vazias**
criadas pela migration de Identity (O1.4.3.1) e usadas apenas pelo Bootstrap, e a tela
`administration/profiles` era **100% mockada** (`perfisMockApi.ts`, dados em memória, catálogo de
permissões estático no frontend). Nenhuma permissão produzia qualquer efeito de acesso.

Depois da O1.5, o ciclo fecha de ponta a ponta e uma permissão só existe se produz efeito real:

```
PERFIL → PERMISSÕES → USUÁRIO×PERFIL → IDENTIDADE AUTENTICADA → POLICIES → ENFORCEMENT (401/403/200)
```

## 2. Arquitetura

```
┌─ Domain/Identity ────────────────────────────────────────────────────────────┐
│ PermissaoCatalogo   ← FONTE CENTRAL ÚNICA dos 14 códigos, com Ids estáveis   │
│ Perfil              ← Nome, Descricao, UnidadeNegocioId, Ativo, timestamps   │
│ Permissao           ← Codigo (Recurso.Acao), Descricao                       │
│ PerfilPermissao     ← Perfil × Permissão                                      │
│ UsuarioPerfil       ← Usuário × Perfil (N:N — usuário pode ter vários)        │
└──────────────────────────────────────────────────────────────────────────────┘
┌─ Application/Identity ───────────────────────────────────────────────────────┐
│ PerfilUseCases       Listar / Obter / Criar / Atualizar / AlterarStatus       │
│                      + NaoEscalonamento  + PerfilAdministrativoInvariante    │
│ ObterIdentidadeAtual revalida sessão E resolve permissões a cada requisição   │
└──────────────────────────────────────────────────────────────────────────────┘
┌─ Infrastructure/Identity ────────────────────────────────────────────────────┐
│ PerfilRepository / PermissaoRepository                                       │
│ PermissoesEfetivasResolver  ← a consulta que define o acesso real            │
└──────────────────────────────────────────────────────────────────────────────┘
┌─ Api ────────────────────────────────────────────────────────────────────────┐
│ Authorization/RbacAuthorization.cs                                           │
│   RbacClaims / PermissaoRequirement / PermissaoAuthorizationHandler          │
│   RbacPolicies.AddRbacPolicies()  ← 1 policy por permissão do catálogo       │
│ Auth/SessionCookieAuthenticationHandler  ← publica as claims de permissão     │
│ Administration/PerfisController          ← endpoints protegidos              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Fonte central única de permissões

`PermissaoCatalogo` é a única declaração de códigos de permissão do sistema. Derivam dele, sem repetir
nenhuma string: as policies registradas em `Program.cs`, o seed de banco (`PermissaoConfiguration.HasData`),
a permissão exigida por cada endpoint, e o catálogo devolvido ao frontend. `RbacPolicies.For(codigo)`
**lança na inicialização** para um código fora do catálogo — um erro de digitação derruba o host em vez de
deixar um endpoint acidentalmente desprotegido.

## 3. Modelo de dados

| Tabela | Chave | Colunas relevantes | Observações |
|---|---|---|---|
| `Perfis` | `Id` | `Nome` (120), `Descricao` (400), `UnidadeNegocioId`, `Ativo`, `CriadoEm`, `AtualizadoEm` | Índice único `IX_Perfis_UnidadeNegocioId_Nome` (de O1.4.3.1, reaproveitado). Sem exclusão física. |
| `Permissoes` | `Id` | `Codigo` (100, único), `Descricao` (400) | Dado de referência: 14 linhas semeadas com Ids estáveis. Não existe tela de criação de Permissão. |
| `PerfisPermissoes` | (`PerfilId`,`PermissaoId`) | — | FKs `RESTRICT` para `Perfis` e `Permissoes` + índice em `PermissaoId`. |
| `UsuariosPerfis` | (`UsuarioId`,`PerfilId`) | — | FKs `RESTRICT` para `Usuarios` e `Perfis` + índice em `PerfilId`. N:N: um usuário tem 1..n Perfis. |

**Não existe tabela de permissão por usuário, deliberadamente** (ADR-0020, itens 7/8/10). Um teste de
modelo (`Model_Should_Not_Contain_Any_Per_User_Permission_Entity`) falha se alguém introduzir uma.

### Catálogo de permissões (14)

Derivado exclusivamente dos códigos já escritos em `docs/product/ComprasFuncional.md` — nenhum nome
inventado:

`UnidadeNegocio.Gerenciar`, `Usuario.Gerenciar`, `Perfil.Gerenciar`, `Filial.Gerenciar`,
`CentroCusto.Gerenciar`, `UnidadeAlocacao.Gerenciar`, `ConfiguracaoErp.Gerenciar`, `Sistema.Gerenciar`,
`Fornecedor.Criar`, `Fornecedor.Editar`, `Fornecedor.Aprovar`, `Pedido.Criar`, `Pedido.Aprovar`,
`Pedido.Cancelar`.

`CentroCusto.Acessar` (acesso do usuário a Centros de Custo) foi **deliberadamente omitido**: o próprio
`ComprasFuncional.md` registra o nome dessa permissão como pendência de produto não confirmada.

## 4. Fluxo de resolução das permissões efetivas

`PermissoesEfetivasResolver` (`Infrastructure/Identity/PerfilRepository.cs`):

```
UsuariosPerfis (do usuário)
  ⨝ Perfis  WHERE Ativo = 1 AND UnidadeNegocioId = <BU da sessão>
  ⨝ PerfisPermissoes
  ⨝ Permissoes
  → DISTINCT Codigo
```

Consequências, todas cobertas por teste:

- **União** das permissões de todos os Perfis vinculados (ADR-0020, itens 8/10), deduplicada.
- **Perfil inativo não concede nada** — é o mecanismo de revogação em massa, sem apagar vínculos
  (preserva auditabilidade).
- **Escopo por Unidade de Negócio**: permissão concedida na BU-B não autoriza ação sobre dados da BU-A,
  já que todas as leituras são escopadas à BU da sessão.
- **Usuário sem Perfil → nenhuma permissão** (fail-closed).

A resolução acontece **a cada requisição**, junto da revalidação da sessão
(`ObterIdentidadeAtualUseCase`, o mesmo princípio de §2.5 do Security Design da O1.4). Portanto inativar
um Perfil ou desvincular um usuário tem **efeito imediato**, sem esperar expiração de sessão — comprovado
pelo teste `Revoking_The_Permission_Should_Take_Effect_On_The_Next_Request_Of_The_Same_Session`.

## 5. Integração com autenticação / identidade

1. `SessionCookieAuthenticationHandler` já era a única resolução assíncrona de sessão por requisição
   (O1.4.2.1). Agora publica também **uma claim `maiscompras_permissao` por permissão efetiva**.
2. `SessionCurrentIdentity` lê essas claims (sem I/O) e preenche `RequestIdentity.UnidadeNegocioId` e
   `RequestIdentity.Permissoes`.
3. `GET /auth/me` devolve `usuario.permissoes` — **exclusivamente para o frontend refletir o acesso**
   (esconder menu/ação). Não é fonte de autorização.
4. O esquema exclusivo de Development (`X-Development-User-Id`) **não emite** claim de permissão nem de
   Unidade de Negócio: autentica, mas nunca satisfaz uma policy de RBAC. Fail-closed.

## 6. Policies e enforcement

- Uma policy por permissão, nome `permissao:<Codigo>`, registrada por iteração sobre o catálogo.
- `PermissaoAuthorizationHandler` decide apenas sobre as claims já publicadas — nenhum I/O por decisão.
- `AuthorizationOptions.FallbackPolicy` (secure-by-default, de O1.4.2.1) continua exigindo autenticação
  em todo endpoint; a policy de RBAC é a camada adicional.

Códigos HTTP:

| Situação | Resultado | Produzido por |
|---|---|---|
| Sem sessão | **401** | `FallbackPolicy` (antes do controller) |
| Sessão válida, sem a permissão | **403** | policy de RBAC (antes do controller) |
| Sessão válida, com a permissão | **200/201** | endpoint |
| Sessão sem Unidade de Negócio resolvida | **403** | `PerfisController.TryResolverUnidadeNegocio` (fail-closed) |
| Perfil inexistente, ou de outra BU | **404** | caso de uso (não vaza existência alheia) |
| Nome duplicado na BU | **409** | caso de uso |
| Concessão acima das permissões do ator | **403** | regra de não-escalonamento |
| Operação que deixaria a BU sem admin | **409** | invariante anti-auto-bloqueio |
| Código de permissão fora do catálogo | **400** | caso de uso (rejeita, nunca ignora) |
| Verbo mutante sem header CSRF | **403** | `CsrfHeaderFilter` (nível de grupo) |

## 7. Endpoints

Todos sob `/api/administracao`, todos exigindo `Perfil.Gerenciar`, todos com `CsrfHeaderFilter` no nível
do grupo (inerte para métodos seguros).

| Método | Rota | Efeito |
|---|---|---|
| GET | `/api/administracao/permissoes` | Catálogo de permissões (banco + metadados de apresentação) |
| GET | `/api/administracao/perfis` | Perfis da Unidade de Negócio da sessão |
| GET | `/api/administracao/perfis/{id}` | Um Perfil da BU da sessão |
| POST | `/api/administracao/perfis` | Cria Perfil com conjunto de permissões |
| PUT | `/api/administracao/perfis/{id}` | Edita nome/descrição e **substitui** o conjunto de permissões |
| PATCH | `/api/administracao/perfis/{id}/status` | Ativa/inativa (não existe exclusão) |

**Prefixo `/api` é obrigatório**: `/administracao/*` é espaço de rotas da SPA, e um proxy de
desenvolvimento que encaminhasse esse prefixo ao backend faria o React Router perder as telas de
Administração — mesmo cuidado já registrado para `/bootstrap` em `vite.config.ts`.

## 8. Regras de segurança implementadas

1. **Unidade de Negócio nunca vem do cliente.** `PerfilInput` não tem `unidadeNegocioId`; o valor vem da
   claim da sessão. Id válido de outra BU responde 404.
2. **Não-escalonamento de privilégio.** Ninguém concede uma permissão que não possui. Sem essa regra,
   `Perfil.Gerenciar` seria super-admin de fato: o portador editaria o próprio Perfil anexando todo o
   catálogo e, como as permissões são reresolvidas a cada requisição, já teria acesso total na chamada
   seguinte. A ADR-0020 (item 8) trata `Perfil.Gerenciar` como permissão atômica, não como acesso
   irrestrito. O Administrador Sênior possui o catálogo completo e nunca é afetado.
3. **Invariante anti-auto-bloqueio.** Nenhuma operação pode deixar a BU sem um Perfil ativo, **com ao
   menos um usuário vinculado**, portador de `Perfil.Gerenciar`. A exigência de usuário vinculado é
   essencial: um Perfil administrativo sem ninguém vinculado não preserva acesso a pessoa alguma, e o
   Bootstrap Mode nunca reabre (ADR-0020, item 12) — recuperação exigiria SQL direto.
4. **Códigos desconhecidos são rejeitados**, nunca ignorados: ignorar criaria um Perfil com menos acesso
   do que o operador acredita ter concedido.
5. **Claims de permissão só o servidor emite.** Header/cookie/body forjado é inerte (teste dedicado).
6. **Integridade referencial** por FK `RESTRICT` — nenhum vínculo órfão que "desapareceria" num JOIN de
   caminho de autorização.
7. **Frontend nunca autoriza.** `usePermissao` e o filtro de menu são UX; a barreira é a policy.

## 9. Migration

`20260811143355_AddRbacPerfilPermissaoCatalogo` — gerada por `dotnet ef migrations add`, com dois blocos
de SQL acrescentados manualmente e comentados no próprio arquivo:

- Colunas novas em `Perfis`; `Permissoes.Descricao` estreitada; seed das 14 permissões; FKs e índices.
- **SQL manual 1** — backfill de `CriadoEm`/`AtualizadoEm` nas linhas de Perfil pré-existentes (evita o
  sentinela `0001-01-01`). Só toca linhas com o sentinela; nada é removido ou reinterpretado.
- **SQL manual 2** — backfill **idempotente** (`NOT EXISTS`) concedendo o catálogo completo aos Perfis
  `Administrador Sênior` existentes. Sem ele, o único administrador do ambiente ficaria com 403
  permanente e o RBAC nasceria irrecuperável pela própria aplicação.
- `Down` remove primeiro os vínculos, porque as FKs `RESTRICT` impedem apagar as permissões semeadas.

Aplicada ao banco de desenvolvimento `MaisCompras` em 11/08/2026. `has-pending-model-changes`: limpo.
`migrations script 0`: exatamente 1 `CREATE TABLE [Fornecedores]` histórico, inalterado.

## 10. Frontend

Vertical Slice `administration/profiles` **preservada** no padrão visual aprovado (AZZAS/SOMA). Mudanças
estritamente funcionais:

- `perfisMockApi.ts` **removido**; `perfisApi.ts` (real) criado. Catálogo estático de permissões do
  frontend **removido** — vem da API.
- `ConfirmExclusaoModal` → `ConfirmStatusModal`: o backend não expõe exclusão, e `ComprasFuncional.md`
  lista Criar/Editar/Ativar-Inativar como as ações oficiais. O modal avisa quantos usuários perdem acesso.
- Campos removidos do formulário: "Unidade de Negócio" (o backend ignoraria o valor digitado) e "Status"
  (virou ação própria, com confirmação, porque revoga acesso em massa).
- Coluna "Unidade de Negócio" removida da tabela (o Id cru não é informação útil).
- Estados reais tratados: carregando, sucesso, vazio, erro e **acesso negado** (403 distinto de erro).
- `AppShell` esconde o item "Perfis" para quem não tem `Perfil.Gerenciar` — UX apenas.

## 11. Limitações conhecidas e pendências

1. **`Fornecedor.*` e `Pedido.*` estão declarados no catálogo, mas nenhum endpoint os exige ainda.** Os
   endpoints de Fornecedores/Negociações continuam protegidos apenas pela `FallbackPolicy`
   (autenticação). Aplicar RBAC a eles é mudança de comportamento em módulos fora do escopo da O1.5.
   A decisão D2 (ADR-0021), portanto, **não está satisfeita para essas superfícies**.
2. **Verificação de invariante não é serializada com a escrita.** Duas requisições concorrentes inativando
   os dois últimos Perfis administrativos podem, em teoria, passar ambas pela checagem. Correção adequada
   exige transação serializável ou `RowVersion` em `Perfil` (padrão já usado em `BootstrapEstado`).
3. **Nenhuma auditoria de alterações de RBAC.** `ComprasFuncional.md` exige registro append-only de toda
   alteração de Perfil/Permissão; a O1.5 não o implementa.
4. **Sem rate limiting** no grupo administrativo (as rotas de `/auth` têm).
5. **Backfill do catálogo é por nome de Perfil** (`Administrador Sênior`), não pelo Id registrado em
   `BootstrapEstado`, e vale para todas as Unidades de Negócio.
6. `ClaimTypes.Role` permanece fixo em `"Buyer"` e, em Development, vindo de header. **Nenhuma decisão de
   autorização usa role** (nenhum `RequireRole`/`IsInRole` no backend) — é resíduo, não vetor.
7. `RequestIdentity.Permissoes` existe como defesa em profundidade, mas **nenhum caso de uso o lê hoje**:
   a policy é a única checagem.
8. Testes de pipeline usam endpoints `/probe-*` sintéticos com a mesma composição de `Program.cs` — não
   detectam a remoção de `.RequireAuthorization(...)` do controller real.
9. Enforcement com **sessão real de usuário sem permissão** não foi validado manualmente: exigiria criar
   um segundo usuário, e a API de Usuários só existe na O1.6. Coberto por teste automatizado de pipeline
   HTTP real e pelo 403 real obtido via esquema de Development.
10. Catálogo de **Perfis** (quais perfis existem) permanece pendência de conteúdo do Product Owner.

## 12. Testes que sustentam este documento

| Suíte | Cobre |
|---|---|
| `Domain/Identity/PermissaoCatalogoTests` | Unicidade de códigos/Ids, derivação Recurso.Acao, presença dos códigos documentados, rejeição de código desconhecido |
| `Domain/Identity/PerfilTests` (mesmo arquivo) | Criação, edição, ativação/inativação, idempotência, timestamps |
| `Application/Identity/PerfilUseCasesTests` | CRUD, unicidade por BU, isolamento entre BUs, rejeição de código desconhecido, substituição de conjunto, **não-escalonamento**, **invariante com/sem usuário vinculado** |
| `Infrastructure/Identity/PermissoesEfetivasResolverTests` | União multi-perfil, deduplicação, Perfil inativo, escopo por BU, isolamento entre usuários, ausência de entidade de permissão por usuário |
| `Infrastructure/Identity/PermissaoSeedTests` | Seed declara o catálogo completo com Ids estáveis |
| `Api/Authorization/RbacEnforcementPipelineTests` | **401/403/200 reais** em host Kestrel, multi-perfil, revogação imediata, header forjado ignorado, esquema de Development não autoriza, 404 preservado |
| `Api/Authorization/RbacPoliciesTests` | Uma policy por permissão, rejeição de código fora do catálogo, claims emitidas |
| `administration/profiles/tests/PerfisPage.test.tsx` | Integração HTTP do slice: sucesso, vazio, erro, **403 → acesso negado**, criação sem `unidadeNegocioId`, conflito, inativação |
