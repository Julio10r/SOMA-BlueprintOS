using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawFornecedorDominiosSnapshotB3Bloco5A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RAW_LinxFornecedorDominiosSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoDominio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxFornecedorDominiosSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxFornecedorDominiosSnapshot_TipoDominio_CodigoErp",
                table: "RAW_LinxFornecedorDominiosSnapshot",
                columns: new[] { "TipoDominio", "CodigoErp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAW_LinxFornecedorDominiosSnapshot");
        }
    }
}
