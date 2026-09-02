using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFiscalB3Bloco3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    UnidadeMedidaCodigoErp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContaContabilCodigoErp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensFiscais", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Codigo", "Descricao" },
                values: new object[,]
                {
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000016"), "ItemFiscal.Visualizar", "Consultar o cadastro de Item Fiscal" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000017"), "ItemFiscal.Criar", "Cadastrar novo Item Fiscal" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000018"), "ItemFiscal.Editar", "Editar Item Fiscal existente" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000019"), "ItemFiscal.Inativar", "Ativar/inativar Item Fiscal no +Compras" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensFiscais_Codigo",
                table: "ItensFiscais",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensFiscais_UnidadeNegocioId",
                table: "ItensFiscais",
                column: "UnidadeNegocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensFiscais");

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000016"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000017"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000018"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000019"));
        }
    }
}
