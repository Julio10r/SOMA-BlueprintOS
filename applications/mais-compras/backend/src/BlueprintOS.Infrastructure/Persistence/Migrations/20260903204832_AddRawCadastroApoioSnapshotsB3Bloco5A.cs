using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawCadastroApoioSnapshotsB3Bloco5A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RAW_LinxCentrosCustoSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoErp = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DescricaoErp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InativoErp = table.Column<bool>(type: "bit", nullable: false),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxCentrosCustoSnapshot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RAW_LinxContasContabeisSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoErp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescricaoErp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InativoErp = table.Column<bool>(type: "bit", nullable: false),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxContasContabeisSnapshot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RAW_LinxUnidadesMedidaSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoErp = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DescricaoErp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InativoErp = table.Column<bool>(type: "bit", nullable: true),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxUnidadesMedidaSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxCentrosCustoSnapshot_CodigoErp",
                table: "RAW_LinxCentrosCustoSnapshot",
                column: "CodigoErp");

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxContasContabeisSnapshot_CodigoErp",
                table: "RAW_LinxContasContabeisSnapshot",
                column: "CodigoErp");

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxUnidadesMedidaSnapshot_CodigoErp",
                table: "RAW_LinxUnidadesMedidaSnapshot",
                column: "CodigoErp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAW_LinxCentrosCustoSnapshot");

            migrationBuilder.DropTable(
                name: "RAW_LinxContasContabeisSnapshot");

            migrationBuilder.DropTable(
                name: "RAW_LinxUnidadesMedidaSnapshot");
        }
    }
}
