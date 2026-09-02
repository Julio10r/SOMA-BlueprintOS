using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFiscalReferenciaFornecedorB3Bloco4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensFiscaisReferenciasFornecedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemFiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoItemFornecedor = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensFiscaisReferenciasFornecedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensFiscaisReferenciasFornecedor_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensFiscaisReferenciasFornecedor_ItensFiscais_ItemFiscalId",
                        column: x => x.ItemFiscalId,
                        principalTable: "ItensFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensFiscaisReferenciasFornecedor_FornecedorId_CodigoItemFornecedor",
                table: "ItensFiscaisReferenciasFornecedor",
                columns: new[] { "FornecedorId", "CodigoItemFornecedor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensFiscaisReferenciasFornecedor_ItemFiscalId_FornecedorId",
                table: "ItensFiscaisReferenciasFornecedor",
                columns: new[] { "ItemFiscalId", "FornecedorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensFiscaisReferenciasFornecedor");
        }
    }
}
