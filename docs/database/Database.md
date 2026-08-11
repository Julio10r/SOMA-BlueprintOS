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
