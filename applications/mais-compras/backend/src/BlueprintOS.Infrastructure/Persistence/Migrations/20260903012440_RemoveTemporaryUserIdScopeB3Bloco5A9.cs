using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTemporaryUserIdScopeB3Bloco5A9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FornecedoresDescobertos_TemporaryUserId_DescobertoEm",
                table: "FornecedoresDescobertos");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_TemporaryUserId",
                table: "Fornecedores");

            migrationBuilder.AlterColumn<Guid>(
                name: "TemporaryUserId",
                table: "FornecedoresDescobertos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "TemporaryUserId",
                table: "Fornecedores",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedoresDescobertos_CodigoItem",
                table: "FornecedoresDescobertos",
                column: "CodigoItem");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedoresDescobertos_DescobertoEm",
                table: "FornecedoresDescobertos",
                column: "DescobertoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FornecedoresDescobertos_CodigoItem",
                table: "FornecedoresDescobertos");

            migrationBuilder.DropIndex(
                name: "IX_FornecedoresDescobertos_DescobertoEm",
                table: "FornecedoresDescobertos");

            migrationBuilder.AlterColumn<Guid>(
                name: "TemporaryUserId",
                table: "FornecedoresDescobertos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TemporaryUserId",
                table: "Fornecedores",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedoresDescobertos_TemporaryUserId_DescobertoEm",
                table: "FornecedoresDescobertos",
                columns: new[] { "TemporaryUserId", "DescobertoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_TemporaryUserId",
                table: "Fornecedores",
                column: "TemporaryUserId");
        }
    }
}
