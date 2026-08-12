using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogoInicialPerfisDeNegocioO1Gate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gate Final da Onda 1 (entregável #9) — backfill do catálogo inicial de Perfis de negócio
            // (Administrador de BU, Comprador, Aprovador, Requisitante) em toda Unidade de Negócio já
            // existente. Sem código C# gerado por `dotnet ef` porque não há alteração de schema — apenas
            // dados. Deliberadamente NÃO toca em "Administrador Sênior" (ciclo de vida exclusivo do
            // Bootstrap). Idempotente via `NOT EXISTS` sobre o índice único (UnidadeNegocioId, Nome) — o
            // mesmo padrão já usado por `AddRbacPerfilPermissaoCatalogo` para o backfill de permissões do
            // Administrador Sênior. Novas Unidades de Negócio criadas a partir desta versão recebem o
            // catálogo em código (`CatalogoInicialPerfisDeNegocioUseCase`), nunca por migration.

            // (1) Cria os 4 Perfis em toda BU que ainda não os tenha.
            migrationBuilder.Sql(@"
INSERT INTO [Perfis] ([Id], [Nome], [Descricao], [UnidadeNegocioId], [Ativo], [CriadoEm], [AtualizadoEm])
SELECT NEWID(), catalogo.[Nome], catalogo.[Descricao], un.[Id], 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
  FROM [UnidadesNegocio] un
 CROSS JOIN (VALUES
    (N'Administrador de BU', N'Administração de negócio da própria Unidade de Negócio: usuários, perfis, estruturas administrativas e cadastros administrativos.'),
    (N'Comprador', N'Operação de compras conforme permissões atribuídas.'),
    (N'Aprovador', N'Aprovações conforme permissões e alçadas configuradas.'),
    (N'Requisitante', N'Requisições e acompanhamento das próprias operações, conforme funcionalidades disponíveis.')
 ) AS catalogo([Nome], [Descricao])
 WHERE NOT EXISTS (
        SELECT 1 FROM [Perfis] p
         WHERE p.[UnidadeNegocioId] = un.[Id] AND p.[Nome] = catalogo.[Nome]);");

            // (2) Vincula as permissões NEGÓCIO do 'Administrador de BU' — nunca UnidadeNegocio.Gerenciar,
            // ConfiguracaoErp.Gerenciar ou Sistema.Gerenciar (PRODUTO, reservadas ao Administrador Sênior).
            migrationBuilder.Sql(@"
INSERT INTO [PerfisPermissoes] ([PerfilId], [PermissaoId])
SELECT p.[Id], perm.[Id]
  FROM [Perfis] p
 CROSS JOIN [Permissoes] perm
 WHERE p.[Nome] = N'Administrador de BU'
   AND perm.[Codigo] IN (
        N'Usuario.Gerenciar', N'Perfil.Gerenciar', N'Filial.Gerenciar', N'CentroCusto.Gerenciar',
        N'UnidadeAlocacao.Gerenciar', N'Workflow.Gerenciar', N'Alcada.Gerenciar', N'Orcamento.Gerenciar')
   AND NOT EXISTS (
        SELECT 1 FROM [PerfisPermissoes] pp
         WHERE pp.[PerfilId] = p.[Id] AND pp.[PermissaoId] = perm.[Id]);");

            // (3) Vincula as permissões do 'Comprador' (Fornecedor.Criar/Editar).
            migrationBuilder.Sql(@"
INSERT INTO [PerfisPermissoes] ([PerfilId], [PermissaoId])
SELECT p.[Id], perm.[Id]
  FROM [Perfis] p
 CROSS JOIN [Permissoes] perm
 WHERE p.[Nome] = N'Comprador'
   AND perm.[Codigo] IN (N'Fornecedor.Criar', N'Fornecedor.Editar')
   AND NOT EXISTS (
        SELECT 1 FROM [PerfisPermissoes] pp
         WHERE pp.[PerfilId] = p.[Id] AND pp.[PermissaoId] = perm.[Id]);");

            // (4) Vincula a permissão do 'Aprovador' (Fornecedor.Aprovar).
            migrationBuilder.Sql(@"
INSERT INTO [PerfisPermissoes] ([PerfilId], [PermissaoId])
SELECT p.[Id], perm.[Id]
  FROM [Perfis] p
 CROSS JOIN [Permissoes] perm
 WHERE p.[Nome] = N'Aprovador'
   AND perm.[Codigo] = N'Fornecedor.Aprovar'
   AND NOT EXISTS (
        SELECT 1 FROM [PerfisPermissoes] pp
         WHERE pp.[PerfilId] = p.[Id] AND pp.[PermissaoId] = perm.[Id]);");

            // (5) 'Requisitante' nasce sem nenhuma permissão do catálogo atual — módulo de Pedido ainda
            // sem enforcement (GAP-01). Nada a vincular; o Perfil já foi criado no passo (1).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove primeiro os vínculos de permissão (mesmo cuidado de AddRbacPerfilPermissaoCatalogo),
            // depois os Perfis do catálogo inicial — nunca toca em 'Administrador Sênior'.
            migrationBuilder.Sql(@"
DELETE pp FROM [PerfisPermissoes] pp
 INNER JOIN [Perfis] p ON p.[Id] = pp.[PerfilId]
 WHERE p.[Nome] IN (N'Administrador de BU', N'Comprador', N'Aprovador', N'Requisitante');");

            migrationBuilder.Sql(@"
DELETE FROM [Perfis]
 WHERE [Nome] IN (N'Administrador de BU', N'Comprador', N'Aprovador', N'Requisitante');");
        }
    }
}
