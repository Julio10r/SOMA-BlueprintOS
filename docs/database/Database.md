# Banco de Dados

O backend possui um `DbContext` real: `BlueprintOSDbContext` (`backend/src/BlueprintOS.Infrastructure/Persistence/`), usando Entity Framework Core com SQL Server. Ele persiste o domínio de Fornecedores (cadastro, descoberta, sincronização com o ERP e histórico de consulta de CNPJ — ver [Procurement.md](../backend/procurement/Procurement.md)), com migrations reais aplicadas nesse mesmo projeto (`Persistence/Migrations/`).

O banco é sempre externo — bancos corporativos `MAISCOMPRAS`/`SOMA_DESENV`, acessados via VPN — nunca um SQL Server local ou em container. Não há pasta `database/` na raiz do repositório nem scripts/seeds de banco separados; a persistência dos demais módulos (ex.: `Documentation`, `Knowledge`) permanece em memória ou em arquivos Markdown.

Este documento é atualizado conforme novos módulos passarem a persistir dados.

## Blueprint administrativo — fatia de Identidade e RBAC

> Blueprint **incremental** (ADR-0021, decisão D7): cada Work Order de domínio documenta a sua própria
> fatia ao concluir a implementação real, nunca como documento desconectado. As fatias de Usuários,
> Filiais, Centros de Custo e Unidades de Alocação serão acrescentadas por O1.6–O1.9.

### Autenticação e Bootstrap (O1.4.2 / O1.4.3)

| Tabela | Chave | Função |
|---|---|---|
| `UnidadesNegocio` | `Id` | Unidade de Negócio; `Slug` único |
| `Usuarios` | `Id` | Usuário do +Compras; `Email` (minúsculo), `Nome`, `UnidadeNegocioId`, `Status` |
| `CodigosVerificacaoOtp` | `Id` | Códigos OTP (hash), uso único |
| `OtpRequestThrottles` | — | Controle de taxa de solicitação de OTP por e-mail |
| `SessoesAutenticacao` | `Id` | Sessão server-side; identificador opaco por hash |
| `BootstrapEstados` | `Id` | Estado do Bootstrap Mode; `Concluido` + `RowVersion` (compare-and-swap) |
| `BootstrapSessoes` | `Id` | Sessão de Bootstrap, distinta e de privilégio limitado |
| `UsuariosCentrosCusto` | (`UsuarioId`,`CentroCustoCodigoErp`) | Escopo de dados operacionais (modelo preparado; gestão em O1.6/O1.7) |

### RBAC (O1.5) — implementado e persistido

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `Perfis` | `Id` | `Nome` (120), `Descricao` (400), `UnidadeNegocioId`, `Ativo`, `CriadoEm`, `AtualizadoEm` | Único `IX_Perfis_UnidadeNegocioId_Nome`. Sem exclusão física (só ativação/inativação) |
| `Permissoes` | `Id` | `Codigo` (100), `Descricao` (400) | `Codigo` único. Dado de **referência**: 14 linhas semeadas com Ids estáveis; sem tela de criação |
| `PerfisPermissoes` | (`PerfilId`,`PermissaoId`) | — | FK → `Perfis`, FK → `Permissoes`, ambas `ON DELETE RESTRICT`; índice em `PermissaoId` |
| `UsuariosPerfis` | (`UsuarioId`,`PerfilId`) | — | FK → `Usuarios`, FK → `Perfis`, ambas `ON DELETE RESTRICT`; índice em `PerfilId` |

Cardinalidades:

```
UnidadeNegocio 1 ──── n Perfil
Perfil         n ──── n Permissao   (via PerfisPermissoes)
Usuario        n ──── n Perfil      (via UsuariosPerfis)  ← usuário pode ter VÁRIOS Perfis
Usuario        ─╳─ Permissao        ← PROIBIDO por ADR-0020 (itens 7/8/10): não existe, e não deve existir,
                                      nenhuma tabela de permissão individual por usuário
```

**Permissões efetivas de um usuário** = união (`DISTINCT`) das permissões de todos os seus Perfis
**ativos** da Unidade de Negócio da sessão. Resolvida no backend a cada requisição — ver
[rbac-o1.5.md](../architecture/rbac-o1.5.md), seção 4.

Migration correspondente: `20260811143355_AddRbacPerfilPermissaoCatalogo`, aplicada ao banco de
desenvolvimento `MaisCompras` em 11/08/2026. Detalhe do conteúdo e dos dois blocos de SQL manual
(backfill de timestamps e concessão do catálogo ao Administrador Sênior já existente) em
[rbac-o1.5.md](../architecture/rbac-o1.5.md), seção 9.

### Filiais e Centros de Custo integrados ao ERP (O1.7) — implementado e persistido

> ERP canônico × +Compras (ADR-0020, item 2/3; D3, ADR-0021): Filial e Centro de Custo são dado mestre
> do ERP `SOMA_DESENV`, lido em tempo real por `IFilialErpReader`/`SomaFilialReader` e
> `ICentroCustoErpReader`/`SomaCentroCustoReader` (introspecção dinâmica de schema via
> `INFORMATION_SCHEMA.COLUMNS`, mesmo padrão de `SomaFornecedorReader`, B2.1/B2.1.2) — **nunca**
> persistido como cópia local. O +Compras persiste **apenas** os dois metadados locais permitidos
> (`DescricaoMaisCompras` opcional, `AtivoNoMaisCompras`), em uma tabela própria por domínio, correlacionada
> ao ERP unicamente pelo código (`CodigoErp`/`CodigoCliFor`) — nunca pelo conteúdo (descrição/nome), que
> permanece somente leitura, de origem ERP.

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `FiliaisMetadados` | `Id` | `CodigoErp` (correlação com `CADASTRO_CLI_FOR`/ERP), `DescricaoMaisCompras` (opcional), `AtivoNoMaisCompras`, `UnidadeNegocioId`, `CriadoEm`, `AtualizadoEm` | Único `IX_FiliaisMetadados_UnidadeNegocioId_CodigoErp` — cada Unidade de Negócio mantém seu próprio metadado local independente para a mesma Filial ERP. Sem exclusão física (só ativação/inativação) |
| `CentrosCustoMetadados` | `Id` | `CodigoErp` (correlação com `CENTRO_CUSTO`/ERP), `DescricaoMaisCompras` (opcional), `AtivoNoMaisCompras`, `UnidadeNegocioId`, `CriadoEm`, `AtualizadoEm` | Único **global** `IX_CentrosCustoMetadados_CodigoErp` — um Centro de Custo só pode estar ancorado a **uma** Unidade de Negócio por vez (decisão deliberada: fecha o vetor cross-BU do vínculo Usuário×Centro de Custo, O1.6-L2). Sem exclusão física |

Ausência de metadado local para um código ERP retornado pela leitura em tempo real é tratada como
**Ativo por padrão** (decisão da O1.7, ver `.ai/BACKLOG.md`) — o metadado só é criado na primeira
edição/ativação manual pela tela de gestão, ou "sob demanda" pelo validador de vínculo Usuário×Centro
de Custo (abaixo).

**Resolução da dívida O1.6-L2** (`UsuariosCentrosCusto.CentroCustoCodigoErp`, texto livre desde O1.4.2,
sem tabela local): a coluna continua texto (sem FK física — ver decisão em `.ai/BACKLOG.md`, O1.6-L2),
mas passa a ser **validada em tempo de execução** por `ICentroCustoVinculoValidator`/
`CentroCustoVinculoValidator` antes de qualquer persistência em `UsuariosCentrosCusto`: código
inexistente no ERP é rejeitado; código já ancorado a outra Unidade de Negócio (`CentrosCustoMetadados`)
é rejeitado; código válido e ainda não ancorado tem seu `CentroCustoMetadado` criado "sob demanda",
ancorado à Unidade de Negócio do ator. A integridade do vínculo é garantida pelo índice único global de
`CentrosCustoMetadados.CodigoErp`, não por FK física.

Migration correspondente: `20260811173904_AddFilialCentroCustoMetadadosO17`, **gerada mas não aplicada**
ao banco de desenvolvimento `MaisCompras` — sem VPN/acesso ao SQL Server corporativo disponível na
sessão de implementação (mesma dependência ambiental de B2.1.3/O1.7, ver Work Order, seção "Riscos").
