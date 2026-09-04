using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFiscalErpFieldsAndFornecedorErpUniqueB3Bloco5A1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // B3 — Bloco 5A (diagnóstico ErpFornecedorId): índice antigo `IX_Fornecedores_BusinessUnit_
            // ErpSistema_ErpFornecedorId` (migration B21FornecedorSynchronization, 31/07/2026) nunca foi
            // removido do banco físico por nenhuma migration — desapareceu apenas do modelo C#
            // (FornecedorConfiguration) em uma regressão acidental (commit 7bf3bf4, remoção de Docker,
            // 03/08/2026), confirmado por leitura de código/git log. `dotnet ef database update` só
            // descobriu essa divergência ao tentar alterar a coluna ErpSistema, dependente desse índice
            // órfão — removê-lo aqui explicitamente é seguro (nenhuma migration posterior o recria) e
            // corrige o drift antes de criar a proteção de unicidade nova e correta.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fornecedores_BusinessUnit_ErpSistema_ErpFornecedorId' AND object_id = OBJECT_ID('[Fornecedores]'))
                    DROP INDEX [IX_Fornecedores_BusinessUnit_ErpSistema_ErpFornecedorId] ON [Fornecedores];");

            migrationBuilder.AlterColumn<string>(
                name: "UnidadeMedidaCodigoErp",
                table: "ItensFiscais",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ContaContabilCodigoErp",
                table: "ItensFiscais",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "OrigemInformacao",
                table: "ItensFiscais",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MaisCompras");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UltimaAlteracaoErp",
                table: "ItensFiscais",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErpSistema",
                table: "Fornecedores",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErpFornecedorId",
                table: "Fornecedores",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_ErpSistema_ErpFornecedorId",
                table: "Fornecedores",
                columns: new[] { "ErpSistema", "ErpFornecedorId" },
                unique: true,
                filter: "[ErpFornecedorId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_ErpSistema_ErpFornecedorId",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemInformacao",
                table: "ItensFiscais");

            migrationBuilder.DropColumn(
                name: "UltimaAlteracaoErp",
                table: "ItensFiscais");

            migrationBuilder.AlterColumn<string>(
                name: "UnidadeMedidaCodigoErp",
                table: "ItensFiscais",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContaContabilCodigoErp",
                table: "ItensFiscais",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErpSistema",
                table: "Fornecedores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErpFornecedorId",
                table: "Fornecedores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
