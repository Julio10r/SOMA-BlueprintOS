using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacPerfilPermissaoCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "Permissoes",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AtualizadoEm",
                table: "Perfis",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CriadoEm",
                table: "Perfis",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Perfis",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Codigo", "Descricao" },
                values: new object[,]
                {
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000001"), "UnidadeNegocio.Gerenciar", "Criar, editar e inativar Unidades de Negócio" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000002"), "Usuario.Gerenciar", "Criar, editar, ativar/inativar usuários e vincular Perfis e Centros de Custo" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000003"), "Perfil.Gerenciar", "Criar, editar e ativar/inativar Perfis e suas permissões" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000004"), "Filial.Gerenciar", "Ativar/inativar Filiais no +Compras e manter a Descrição +Compras" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000005"), "CentroCusto.Gerenciar", "Ativar/inativar Centros de Custo no +Compras e manter a Descrição +Compras" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000006"), "UnidadeAlocacao.Gerenciar", "Criar, editar e ativar/inativar Unidades de Alocação" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000007"), "ConfiguracaoErp.Gerenciar", "Configurar a integração de ERP por Unidade de Negócio" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000008"), "Sistema.Gerenciar", "Acessar Administração do Sistema (integrações, monitor, filas, logs, saúde)" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000009"), "Fornecedor.Criar", "Cadastrar novo fornecedor" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000a"), "Fornecedor.Editar", "Atualizar dados de fornecedor" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000b"), "Fornecedor.Aprovar", "Aprovar divergências de enriquecimento de fornecedor" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000c"), "Pedido.Criar", "Criar pedido de compra" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000d"), "Pedido.Aprovar", "Aprovar pedido de compra" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000e"), "Pedido.Cancelar", "Cancelar pedido de compra" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPerfis_PerfilId",
                table: "UsuariosPerfis",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisPermissoes_PermissaoId",
                table: "PerfisPermissoes",
                column: "PermissaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfisPermissoes_Perfis_PerfilId",
                table: "PerfisPermissoes",
                column: "PerfilId",
                principalTable: "Perfis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PerfisPermissoes_Permissoes_PermissaoId",
                table: "PerfisPermissoes",
                column: "PermissaoId",
                principalTable: "Permissoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPerfis_Perfis_PerfilId",
                table: "UsuariosPerfis",
                column: "PerfilId",
                principalTable: "Perfis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPerfis_Usuarios_UsuarioId",
                table: "UsuariosPerfis",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---- Ajustes acrescentados manualmente a esta migration (O1.5), não gerados por `dotnet ef` ----

            // (1) Backfill das novas colunas de auditoria. O `defaultValue` gerado pelo EF para as colunas
            // NOT NULL é 0001-01-01, que satisfaz o schema mas seria um dado sem significado nas linhas de
            // Perfil já existentes (o Perfil "Administrador Sênior" criado pelo Bootstrap, O1.4.3.2).
            // Nenhuma linha é criada, removida ou reinterpretada aqui — apenas duas colunas novas recebem
            // um valor plausível em vez do mínimo do tipo.
            migrationBuilder.Sql(@"
UPDATE [Perfis]
   SET [CriadoEm] = SYSDATETIMEOFFSET(),
       [AtualizadoEm] = SYSDATETIMEOFFSET()
 WHERE [CriadoEm] = '0001-01-01 00:00:00 +00:00';");

            // (2) Concede o catálogo completo de permissões aos Perfis "Administrador Sênior" já existentes.
            //
            // Necessário para não bloquear o ambiente: até a O1.5 nenhum vínculo Perfil×Permissão existia,
            // então o Administrador Sênior criado pelo Bootstrap tem zero permissões. Como a O1.5 passa a
            // exigir `Perfil.Gerenciar` para a Gestão de Perfis, sem este backfill o único administrador do
            // ambiente ficaria permanentemente com 403 e não haveria caminho pela aplicação para se conceder
            // acesso — um bloqueio irrecuperável. O `NOT EXISTS` torna a operação idempotente e ela nunca
            // remove um vínculo existente nem toca em qualquer outro Perfil.
            migrationBuilder.Sql(@"
INSERT INTO [PerfisPermissoes] ([PerfilId], [PermissaoId])
SELECT p.[Id], perm.[Id]
  FROM [Perfis] p
 CROSS JOIN [Permissoes] perm
 WHERE p.[Nome] = N'Administrador Sênior'
   AND NOT EXISTS (
        SELECT 1 FROM [PerfisPermissoes] pp
         WHERE pp.[PerfilId] = p.[Id] AND pp.[PermissaoId] = perm.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove primeiro os vínculos Perfil×Permissão, porque as FKs `Restrict` acrescentadas por esta
            // migration impedem a exclusão das linhas de `Permissoes` enquanto houver vínculo apontando para
            // elas (inclusive os criados pelo backfill do `Up`).
            migrationBuilder.Sql(@"
DELETE FROM [PerfisPermissoes]
 WHERE [PermissaoId] IN (SELECT [Id] FROM [Permissoes]);");

            migrationBuilder.DropForeignKey(
                name: "FK_PerfisPermissoes_Perfis_PerfilId",
                table: "PerfisPermissoes");

            migrationBuilder.DropForeignKey(
                name: "FK_PerfisPermissoes_Permissoes_PermissaoId",
                table: "PerfisPermissoes");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPerfis_Perfis_PerfilId",
                table: "UsuariosPerfis");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPerfis_Usuarios_UsuarioId",
                table: "UsuariosPerfis");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosPerfis_PerfilId",
                table: "UsuariosPerfis");

            migrationBuilder.DropIndex(
                name: "IX_PerfisPermissoes_PermissaoId",
                table: "PerfisPermissoes");

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000002"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000003"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000004"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000005"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000006"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000007"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000008"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000009"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000a"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000b"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000c"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000d"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000e"));

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Perfis");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Perfis");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Perfis");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "Permissoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);
        }
    }
}
