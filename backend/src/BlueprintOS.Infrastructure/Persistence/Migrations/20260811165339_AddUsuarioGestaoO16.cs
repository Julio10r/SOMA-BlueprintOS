using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioGestaoO16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AtualizadoEm",
                table: "Usuarios",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CriadoEm",
                table: "Usuarios",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "TodosCentrosCusto",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosCentrosCusto_Usuarios_UsuarioId",
                table: "UsuariosCentrosCusto",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---- Ajuste acrescentado manualmente a esta migration (O1.6), não gerado por `dotnet ef` ----
            //
            // Backfill das novas colunas de auditoria de Usuario, mesmo cuidado da migration O1.5
            // (AddRbacPerfilPermissaoCatalogo) para Perfis: o `defaultValue` gerado pelo EF para colunas
            // NOT NULL é 0001-01-01, que satisfaz o schema mas seria um dado sem significado para o
            // usuário "Administrador Sênior" já criado pelo Bootstrap (O1.4.3.2). Nenhuma linha é criada,
            // removida ou reinterpretada — apenas as colunas novas recebem um valor plausível.
            migrationBuilder.Sql(@"
UPDATE [Usuarios]
   SET [CriadoEm] = SYSDATETIMEOFFSET(),
       [AtualizadoEm] = SYSDATETIMEOFFSET()
 WHERE [CriadoEm] = '0001-01-01 00:00:00 +00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosCentrosCusto_Usuarios_UsuarioId",
                table: "UsuariosCentrosCusto");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TodosCentrosCusto",
                table: "Usuarios");
        }
    }
}
