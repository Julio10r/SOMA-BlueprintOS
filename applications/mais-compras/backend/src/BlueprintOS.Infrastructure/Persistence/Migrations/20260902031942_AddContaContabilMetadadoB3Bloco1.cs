using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContaContabilMetadadoB3Bloco1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContasContabeisMetadados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescricaoMaisCompras = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AtivoNoMaisCompras = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasContabeisMetadados", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Codigo", "Descricao" },
                values: new object[] { new Guid("b1a5c4e0-0001-4a10-9f01-000000000014"), "ContaContabil.Gerenciar", "Ativar/inativar Contas Contábeis no +Compras e manter a Descrição +Compras" });

            migrationBuilder.CreateIndex(
                name: "IX_ContasContabeisMetadados_CodigoErp",
                table: "ContasContabeisMetadados",
                column: "CodigoErp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasContabeisMetadados");

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000014"));
        }
    }
}
