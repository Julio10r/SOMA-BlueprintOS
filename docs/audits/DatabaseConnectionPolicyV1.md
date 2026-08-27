# Database Connection Policy V1 — Separação Linx Development/Production

Data: 2026-08-27
Escopo: fundação de conexões com bancos Linx/SOMA (DEV/PROD). **Não** retoma o caso funcional
PROG/OP/PED.

> **Correção (2026-08-27, etapa posterior):** todas as referências a `192.168.0.200` como servidor de
> produção neste documento **estavam incorretas** — não era um problema de firewall/VPN, era um
> endpoint mal configurado desde a origem. Evidência real de uma conexão funcional ao `SOMA` confirmou
> o endpoint correto: `192.168.9.200`, porta `1433`, TCP, sem instância nomeada (`@@SERVERNAME` =
> `SRV-SOMADB`). O profile `linx-production` (`LinxConnectionProfiles.Production`) e a documentação
> foram corrigidos. **Nenhum conteúdo abaixo foi apagado** — preserva o registro histórico exato do
> que foi investigado/observado com o endpoint então configurado. Ver
> `docs/audits/LinxProductionEndpointCorrectionV1.md` para o relato completo da correção.

## Baseline (antes desta tarefa)

- Uma única chave `ConnectionStrings:ErpConnection`, sem separação semântica de ambiente.
- `B1ConnectivityValidator` validava `ErpConnection` (rotulada "ERP SOMA_DESENV") com `SELECT 1` +
  `SELECT SUSER_SNAME()`, sem comparar servidor/banco resolvidos contra nenhum valor esperado.
- Nenhuma chave/profile equivalente existia para Production — `SOMA`/`192.168.0.200` não era
  conhecido pelo código.
- `agents/linx-database-specialist-agent/agent.yaml` declarava dois profiles (`linx-erp-read-only`,
  `linx-erp-governed-write`) apontando ambos para a mesma `ConnectionStrings:ErpConnection`.
- `ConnectivityStatus` tinha `Ready | NotConfigured | Failed | PermissionDenied` — sem
  `EnvironmentMismatch` nem `VpnRequired` (rede indisponível era classificada como `Failed`,
  indistinguível de qualquer outro erro).
- 6+ consumidores (`SomaFilialReader`, `SomaFornecedorReader`, `SomaCentroCustoReader`,
  `LinxSchemaDiscoveryReader`, `ErpFornecedorDiscoveryRepository`,
  `SomaGarantirFornecedorErpAdapter`, `SomaDesenvolErpFornecedorAdapter`) leem
  `ErpConnection` diretamente, cada um com sua própria guarda de placeholder duplicada.

## Problema

Ambiguidade operacional: `ErpConnection` não deixava explícito se apontava para DEV ou PROD, não
havia proteção determinística contra apontar para o servidor errado, e nenhum estado distinguia
"VPN desconectada" de "credencial inválida".

## Arquitetura DEV

- Profile `linx-development`: servidor `192.168.9.98`, banco `SOMA_DESENV`, porta `1433`,
  `vpn_required: true`, ambiente `Development`.
- Chave: `ConnectionStrings:LinxDevelopmentConnection`.
- Laboratório técnico: SELECT/INSERT/UPDATE/DELETE/CREATE/ALTER/DROP/TRUNCATE/EXEC podem ser
  válidos com contexto claro, finalidade clara, permissão efetiva e rastreabilidade — nunca
  execução arbitrária.

## Arquitetura PROD

- Profile `linx-production`: servidor `192.168.0.200`, banco `SOMA`, porta `1433`,
  `vpn_required: true`, ambiente `Production`.
- Chave: `ConnectionStrings:LinxProductionConnection`.
- Governança reforçada e conservadora, sem alteração de nenhuma proteção existente (ver
  `agents/DATABASE_CONNECTION_POLICY.md` § 12).

## Profiles

Implementados em
[`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs`](../../backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs)
como `LinxConnectionProfiles.Development` / `.Production` — metadado lógico (servidor, banco,
nome de chave, VPN), nunca credencial.

## Credenciais

Permanecem exclusivamente em User Secrets local, mesmo store existente (`UserSecretsId:
BlueprintOS-Development`), duas chaves distintas. Nenhum valor real foi commitado, logado ou
colocado em manifesto/YAML. Comandos exatos com placeholder documentados em
`agents/DATABASE_CONNECTION_POLICY.md` § 4.

## VPN

`ConnectivityStatus.VpnRequired` adicionado — classificação separada de `PermissionDenied` para
erros de rede (timeout, host inacessível, socket) em profiles com `vpn_required: true`. O CLI
`validate-b1-connectivity` agora imprime "Conecte-se à VPN corporativa e tente novamente." quando
esse estado ocorre.

## Proteção contra mismatch

`B1ConnectivityValidator` compara `SqlConnectionStringBuilder.DataSource`/`InitialCatalog` da
connection string configurada contra o profile esperado **antes de abrir qualquer conexão de
rede**. Divergência de servidor OU banco, em qualquer direção (DEV↔PROD), bloqueia como
`ConnectivityStatus.EnvironmentMismatch`. Coberto por 4 testes unitários que não abrem conexão
real (um deles mede que a resposta é imediata, não uma tentativa de rede com timeout).

## Governança DEV vs PROD

Sem mudança de comportamento de governança nesta tarefa — apenas a fundação de conexão/ambiente. As
regras de §11/§12 de `agents/DATABASE_CONNECTION_POLICY.md` documentam o que já era prática
esperada (Development mais permissivo com contexto; Production conservador), sem relaxar nenhuma
proteção de Production.

## Validator

`B1ConnectivityValidator.ValidateErpAsync(LinxEnvironment)` é o novo ponto de entrada explícito.
`ValidateErpAsync()` sem argumento é mantido, agora delegando para `Development` (compatibilidade).
Fallback: se `LinxDevelopmentConnection` não estiver configurada, tenta a chave legada
`ErpConnection` — ainda validada contra o profile Development (guarda de mismatch se aplica
igualmente ao fallback). Nunca imprime a connection string; expõe apenas `Server`, `Database`,
`EffectiveIdentity`.

## Clone / primeiro setup

`git clone` → nenhuma credencial vem junto → `validate-b1-connectivity` reporta `NotConfigured`
para qualquer profile sem secret → desenvolvedor roda o comando `dotnet user-secrets set` do
profile que precisa (DEV, PROD, ou ambos) → valida com o mesmo comando → identidade/permissão
efetiva do banco é respeitada como está.

## Testes

`B1ConnectivityValidatorTests.cs`: 12 testes (8 novos), todos sem rede/DB real, cobrindo:
- DEV resolve somente DEV (secret de DEV presente não vaza para resolução de PROD).
- Falta de secret DEV → `NotConfigured`; falta de secret PROD → `NotConfigured`.
- Fallback legado `ErpConnection` ainda validado contra o profile Development.
- Mismatch bloqueado nas duas direções (servidor e banco), sem tentativa de conexão de rede.
- Nenhum resultado (`NotConfigured` ou `EnvironmentMismatch`) carrega a connection string/senha.
- Suíte completa (904 testes) roda em ~2s, sem regressão.

## Secret scan

Revisado o diff completo desta tarefa (código, YAML, docs, JSON). Nenhuma senha/token/connection
string real presente — apenas valores de teste (`dev/dev`, `x/x`, `super-secret-user/password`
como fixture negativa) e placeholders `__SET_...__`. IPs e nomes de bancos (`192.168.9.98`,
`192.168.0.200`, `SOMA_DESENV`, `SOMA`) estão versionados conforme permitido pela política.

## Compatibilidade

`ErpConnection` mantida como fallback DEPRECATED/LEGACY apenas para Development — nenhum
consumidor existente (os 7 leitores/adapters listados no baseline) foi alterado ou quebrado
silenciosamente. Migração desses consumidores para a chave canônica fica para uma tarefa futura,
fora do escopo funcional desta correção de fundação.

## Validação real (read-only)

- **DEV** (`192.168.9.98`/`SOMA_DESENV`): secret já existente localmente (`ErpConnection`); nova
  chave canônica `LinxDevelopmentConnection` também configurada nesta máquina, mesmo valor.
  Resultado: **READY** via `dotnet run --project backend/src/BlueprintOS.Api --
  validate-b1-connectivity` (`SELECT 1` + `SELECT SUSER_SNAME()` → identidade `ti.n8n`). Nenhuma
  escrita.
- **PROD** (`192.168.0.200`/`SOMA`): secret **não configurado** nesta máquina. Resultado:
  **NOT_CONFIGURED**, reportado corretamente sem inventar nada. Comando exato para configurar,
  se e quando desejado, está em `agents/DATABASE_CONNECTION_POLICY.md` § 4 — aguardando o usuário.

## Arquivos alterados

- `agents/DATABASE_CONNECTION_POLICY.md` (novo) — política canônica.
- `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs` (novo) — enum
  `LinxEnvironment`, record `LinxConnectionProfile`, `LinxConnectionProfiles.Development/Production`.
- `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs` — validação por
  profile, mismatch guard, `VpnRequired`/`EnvironmentMismatch`.
- `backend/src/BlueprintOS.Api/appsettings.json` — chaves `LinxDevelopmentConnection`/
  `LinxProductionConnection` (placeholder), `ErpConnection` mantida.
- `backend/src/BlueprintOS.Api/Program.cs` — `validate-b1-connectivity` valida DEV e PROD; mensagens
  de orientação VPN/mismatch.
- `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` —
  8 testes novos.
- `agents/linx-database-specialist-agent/agent.yaml` — profiles `linx-development`/`linx-production`.
- `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` — nota de atualização (§25.7).
- `docs/audits/AgentLearningV1-LinxProgOpPed.md` — nota pontual, caso funcional não reaberto.
- `docs/audits/DatabaseConnectionPolicyV1.md` (este arquivo).

## Riscos

- Consumidores não migrados (7 arquivos) continuam lendo `ErpConnection` diretamente, não o profile
  Development formal — dívida técnica documentada, não corrigida nesta tarefa por decisão explícita
  de minimizar blast radius.
- Classificação `VpnRequired` por número de erro SQL (`53`, `-2`, `-1`, `2`, `258`, `10060`) é uma
  heurística; alguns ambientes/drivers podem emitir números diferentes para "rede indisponível" —
  não testado contra um cenário real de VPN desconectada (fora do alcance desta sessão).

## Próximos passos (fora de escopo desta tarefa)

- Adicionar comando CLI dedicado por ambiente (`validate-linx-development`,
  `validate-linx-production`) se o uso separado se mostrar necessário.
- Remover de vez a chave legada `ErpConnection`/fallback quando toda máquina de desenvolvimento
  tiver `LinxDevelopmentConnection` configurada.
- Retomar o caso PROG/OP/PED (gaps 7.9.a/7.9.b) quando o Product Owner responder.

---

## Atualização 2026-08-27 (continuação) — PROD local, MAISCOMPRAS e migração dos consumidores

### PROD secret

`ConnectionStrings:LinxProductionConnection` **não estava configurada** nesta máquina no início
desta etapa. Por instrução explícita da tarefa, a validação PROD foi pausada e o comando exato foi
solicitado ao usuário no chat (com placeholder, sem pedir usuário/senha):

```bash
dotnet user-secrets set "ConnectionStrings:LinxProductionConnection" "Server=192.168.0.200;Database=SOMA;User Id=<SEU_USUARIO_PROD>;Password=<SUA_SENHA_PROD>;Encrypt=True;TrustServerCertificate=True" --project backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj
```

Nenhum valor foi inventado ou assumido. Resultado nesta etapa: **NOT_CONFIGURED**, igual à etapa
anterior — aguardando confirmação do usuário para revalidar.

### MAISCOMPRAS formalizado como profile DEV

`LinxConnectionProfiles.MaisComprasDevelopment` (mesmo arquivo `LinxConnectionProfile.cs`): servidor
`192.168.9.98` (igual a `linx-development`), banco `MAISCOMPRAS`, chave
`ConnectionStrings:MaisComprasConnection` (já existente, não é nova — é a mesma usada por
`BlueprintOSDbContext` desde antes desta política). `B1ConnectivityValidator.ValidateMaisComprasAsync()`
agora aplica a mesma proteção de mismatch dos demais profiles antes de abrir conexão. Nenhum segredo
novo foi criado nem duplicado — o profile reaproveita a chave/identidade DEV local já configurada.
Validado read-only nesta máquina: **READY**, servidor `192.168.9.98`, banco `MAISCOMPRAS`,
identidade `ti.n8n` (mesma identidade efetiva do profile `linx-development`).

### Migração dos consumidores legados

Auditados novamente no código atual (não a partir do relatório anterior) e confirmados: exatamente
7 consumidores de produção liam `ConnectionStrings:ErpConnection` diretamente —
`SomaFornecedorReader`, `SomaFilialReader`, `SomaCentroCustoReader`, `LinxSchemaDiscoveryReader`,
`ErpFornecedorDiscoveryRepository`, `SomaGarantirFornecedorErpAdapter`,
`SomaDesenvolErpFornecedorAdapter`. Todos os 7 foram migrados para
`LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development)` — novo
ponto único de resolução (`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionStringResolver.cs`)
que centraliza a guarda de placeholder e a checagem de mismatch já usada pelo validator, eliminando
a duplicação de guarda que existia em cada arquivo. Nenhum consumidor decide ambiente pelo conteúdo
da connection string — todos declaram o profile explicitamente. `Program.cs` (`ProbeErpSupplierIntegrityAsync`,
uma ferramenta de diagnóstico CLI, não um consumidor de produção) não foi alterado nesta etapa —
fora do escopo dos 7 consumidores auditados.

### ErpConnection restante

Único uso restante de `ErpConnection` no código é o fallback de compatibilidade dentro do próprio
`LinxConnectionStringResolver`, exclusivo do profile `linx-development`, documentado como
DEPRECATED — nenhum consumidor volta a lê-la diretamente. `appsettings.json` mantém o placeholder da
chave por compatibilidade.

### Testes

6 testes novos em `B1ConnectivityValidatorTests.cs`: `ValidateMaisComprasAsync` sem secret →
`NotConfigured`; mismatch MAISCOMPRAS ↔ SOMA_DESENV; mismatch MAISCOMPRAS ↔ servidor PROD;
profiles `Development`/`MaisComprasDevelopment` compartilham servidor mas têm bancos/chaves
distintos; `LinxConnectionStringResolver` lança sem conectar quando não configurado; resolver
rejeita uma connection string de Development passada como Production. Suíte completa:
**910/910** (era 904), ~2s, sem regressão — incluindo `ErpReadersReadOnlyTests.cs`, que continua
passando sem alteração após a migração dos readers.

### Validação real (read-only, sem escrita)

- **DEV SOMA_DESENV**: READY — servidor `192.168.9.98`, banco `SOMA_DESENV`, identidade `ti.n8n`.
- **DEV MAISCOMPRAS**: READY — servidor `192.168.9.98`, banco `MAISCOMPRAS`, identidade `ti.n8n`.
- **PROD SOMA**: NOT_CONFIGURED — aguardando o usuário configurar o secret localmente.

### Arquivos alterados nesta etapa

- `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs` — adiciona
  `MaisComprasDevelopment`; `Label` passa a ser campo explícito do record (antes computado só para
  Linx Development/Production).
- `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionStringResolver.cs` (novo) —
  ponto único de resolução/guarda/mismatch reaproveitado por validator e pelos 7 consumidores.
- `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs` —
  `ValidateMaisComprasAsync()` passa a usar `LinxConnectionProfiles.MaisComprasDevelopment` com
  proteção de mismatch.
- `backend/src/BlueprintOS.Infrastructure/Integrations/ERP/Soma/SomaFornecedorReader.cs`,
  `SomaFilialReader.cs`, `SomaCentroCustoReader.cs`, `LinxSchemaDiscoveryReader.cs` — migrados para
  `LinxConnectionStringResolver`.
- `backend/src/BlueprintOS.Infrastructure/Persistence/Repositories/ErpFornecedorDiscoveryRepository.cs`,
  `SomaGarantirFornecedorErpAdapter.cs`, `SomaDesenvolErpFornecedorAdapter.cs` — idem.
- `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` —
  6 testes novos.
- `agents/DATABASE_CONNECTION_POLICY.md` — § 2/3/6/7/13/15 atualizados com MAISCOMPRAS DEV e o
  resolver compartilhado.
- `docs/audits/DatabaseConnectionPolicyV1.md` (este arquivo).

### Riscos atualizados

- O item "consumidores não migrados" do relatório anterior está **resolvido** — os 7 arquivos foram
  migrados.
- Migração de `SomaGarantirFornecedorErpAdapter` precisou converter a exceção genérica do resolver
  (`InvalidOperationException`) para o tipo de exceção de domínio esperado
  (`ErpFornecedorEscritaException`) via `try/catch` — comportamento externo preservado, mas é um
  ponto de atenção para revisão de código (a mensagem do resolver agora é reaproveitada como
  mensagem da exceção de domínio).
- `Program.cs` (`ProbeErpSupplierIntegrityAsync`, ferramenta CLI de diagnóstico) continua lendo
  `ErpConnection`/validação própria — não migrado nesta etapa por não ser um dos 7 consumidores de
  produção auditados; considerar na remoção final do fallback.

---

## Atualização 2026-08-27 (continuação) — validação read-only de PROD

### PROD secret

O usuário confirmou ter configurado `ConnectionStrings:LinxProductionConnection` localmente. O
valor **não foi exibido nem registrado** em nenhum momento desta etapa.

### Resultado real (read-only)

`dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity`:

- **servidor efetivo**: `192.168.0.200` (bate com o profile `linx-production`).
- **banco efetivo**: `SOMA` (bate com o profile `linx-production`).
- **ENVIRONMENT_MISMATCH**: não ocorreu — servidor e banco resolvidos batem exatamente com o
  esperado, então o guard de mismatch (que roda antes de qualquer tentativa de rede) deixou a
  tentativa de conexão prosseguir.
- **Conexão de rede**: falhou. `SqlException` (`Number = 0`, mensagem "A network-related or
  instance-specific error occurred... The server was not found or was not accessible... (provider:
  TCP Provider, error: 35)"). Um teste independente de rede (`nc -z -w 3 192.168.0.200 1433`) nesta
  mesma máquina confirma a porta **inacessível** — coerente com VPN corporativa não conectada
  (ou não roteável) nesta sessão de execução.
- **CONNECTION STATUS = VPN_REQUIRED** (não `READY`, não `PermissionDenied`) — a classificação
  correta segundo a política: rede indisponível nunca deve ser confundida com credencial inválida.

### Gap de classificação encontrado e corrigido

A primeira execução real classificou essa falha como `Failed` genérico, não como `VpnRequired`: o
driver `Microsoft.Data.SqlClient` reporta essa família de erro de rede com `SqlException.Number ==
0` (nenhum código nativo do SQL Server — a conexão TCP nunca chegou a se estabelecer), fora da lista
de números já tratados (`53, -2, -1, 2, 258, 10060`). Corrigido em
`B1ConnectivityValidator.IsNetworkUnreachable`: quando `Number == 0`, a mensagem é inspecionada para
os textos característicos desse cenário (`network-related`, `TCP Provider`,
`was not found or was not accessible`) antes de classificar como `VpnRequired`. Também adicionado
`Código SQL: {sqlException.Number}` na saída do CLI (`Program.cs`) — não é segredo, é apenas o
número de erro nativo do SQL Server, útil para diagnosticar VPN vs. credencial no futuro. Suíte
completa revalidada: **910/910**, sem regressão.

### Conclusão desta etapa

`linx-production` **não pode ser marcado READY** ainda — o profile está correto (servidor/banco
batem, sem mismatch, credencial aceita pelo guard de configuração), mas a rede/VPN não está
alcançável a partir desta sessão de execução do agente. Isto não é uma falha de configuração da
policy nem da credencial: é exatamente o estado `VPN_REQUIRED` que a política pede para reportar
sem inventar sucesso. Para concluir a validação real de PROD, é necessário rodar
`validate-b1-connectivity` (ou o app) a partir de uma máquina/sessão com a VPN corporativa
efetivamente conectada e roteando até `192.168.0.200:1433`.

### Arquivos alterados nesta etapa

- `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs` — classifica
  `SqlException` com `Number == 0` e mensagem de rede como `VpnRequired` em vez de `Failed`.
- `backend/src/BlueprintOS.Api/Program.cs` — imprime o código de erro SQL nativo (não sensível) na
  saída de diagnóstico de conectividade.
- `docs/audits/DatabaseConnectionPolicyV1.md` (este arquivo).

---

## Atualização 2026-08-27 (continuação) — retry único e `CONNECTIVITY_UNAVAILABLE`

### Ajuste solicitado

O status `VpnRequired` estava específico demais: uma única falha de rede não prova "VPN
desconectada" — a VPN corporativa pode estar conectada e a conectividade com o SQL Server cair
momentaneamente (precisa apenas ser restabelecida). Ajuste pedido: ao detectar falha de
rede/conectividade, não concluir a causa; fazer exatamente 1 retry automático (com pequeno
intervalo) antes de reportar qualquer coisa ao usuário.

### Mudança implementada

- `ConnectivityStatus.VpnRequired` → renomeado para `ConnectivityStatus.ConnectivityUnavailable`
  (fato observável — "conectividade não pôde ser restabelecida" — não mais um diagnóstico de causa).
- `vpn_required` continua existindo nos profiles (`LinxConnectionProfiles`) como característica do
  profile (este banco depende de VPN), não como diagnóstico automático de causa de falha — exatamente
  como pedido no item 7 da tarefa.
- Retry único: ao detectar falha de rede/conectividade (nunca para `PermissionDenied`,
  `NotConfigured` ou `EnvironmentMismatch` — esses retornam antes de qualquer tentativa de rede ou
  são erros de credencial/config, nunca de rede), aguarda 750ms e tenta novamente exatamente 1 vez.
  - Retry bem-sucedido → `Ready`, `RecoveredAfterRetry = true` (registrado internamente; CLI mostra
    uma nota discreta, sem incomodar o usuário).
  - Retry também falha → `ConnectivityUnavailable`, mensagem exata solicitada: "Não foi possível
    acessar o servidor após uma tentativa de restabelecimento. Verifique/reconecte a VPN ou a
    conexão com o servidor e tente novamente."
  - Nenhum loop: no máximo 1 retry por chamada de validação, sempre.
- Para tornar o retry testável sem SQL Server real, a abertura de conexão foi extraída para
  `ISqlConnectivityProbe` (produção: `SqlConnectivityProbe`, o comportamento real inalterado) e um
  seam de teste `ISimulatedSqlFailure` permite que um fake anuncie "isto é permissão negada" sem
  precisar construir um `SqlException` real (construtores internos ao driver).

### Testes novos

6 testes cobrindo exatamente os cenários pedidos: primeira tentativa falha + retry funciona →
`Ready`/`RecoveredAfterRetry`; duas tentativas falham → `ConnectivityUnavailable`; permission denied
→ zero chamadas ao probe (zero retry); not configured → zero chamadas (nunca chega a tentar rede);
environment mismatch → zero chamadas; guarda adicional confirmando no máximo 2 chamadas ao probe
mesmo que o fake seja configurado para falhar repetidamente (trava contra um futuro loop de retry).
Suíte completa: **916/916** (era 910), sem regressão — o teste de retry bem-sucedido é o único que
paga o delay fixo de 750ms; os demais continuam em milissegundos.

### Revalidação read-only de `linx-production`

`dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity`:

- **servidor efetivo**: `192.168.0.200` — correto, sem mismatch.
- **banco efetivo**: `SOMA` — correto, sem mismatch.
- Primeira tentativa falhou por rede (mesmo `SqlException` "network-related..., TCP Provider, error:
  35"); o validator aguardou 750ms e tentou novamente automaticamente — segunda tentativa também
  falhou pelo mesmo motivo de rede.
- **CONNECTION STATUS = CONNECTIVITY_UNAVAILABLE** (não `READY`, não inventado). Mensagem exibida:
  "Não foi possível acessar o servidor após uma tentativa de restabelecimento. Verifique/reconecte a
  VPN ou a conexão com o servidor e tente novamente."
- Nenhuma escrita, nenhuma migration — a falha ocorreu antes de qualquer SQL além de `SELECT 1`
  (que nem chegou a ser emitido, pois a conexão TCP não se estabeleceu).

O resultado confirma que este ambiente de execução do agente continua sem rota de rede até
`192.168.0.200:1433` mesmo após o retry — condição de rede da sessão do agente, não do secret nem
do profile (ambos corretos). Para concluir `linx-production` como `READY`, a validação precisa
rodar a partir de uma máquina/sessão com VPN corporativa efetivamente conectada.

### Arquivos alterados nesta etapa

- `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs` — retry único,
  `ConnectivityStatus.ConnectivityUnavailable`, `RecoveredAfterRetry`, `ISqlConnectivityProbe`,
  `ISimulatedSqlFailure`.
- `backend/src/BlueprintOS.Api/Program.cs` — mensagem `CONNECTIVITY_UNAVAILABLE` e nota de
  recuperação após retry.
- `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` —
  6 testes novos de retry.
- `agents/DATABASE_CONNECTION_POLICY.md` — § 7/8/13/15 reescritos para o comportamento de retry único.
- `docs/audits/DatabaseConnectionPolicyV1.md` (este arquivo).
