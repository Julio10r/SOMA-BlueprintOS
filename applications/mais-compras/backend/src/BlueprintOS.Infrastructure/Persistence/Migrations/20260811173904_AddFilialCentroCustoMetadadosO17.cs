using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilialCentroCustoMetadadosO17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CentrosCustoMetadados",
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
                    table.PrimaryKey("PK_CentrosCustoMetadados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiliaisMetadados",
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
                    table.PrimaryKey("PK_FiliaisMetadados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoMetadados_CodigoErp",
                table: "CentrosCustoMetadados",
                column: "CodigoErp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiliaisMetadados_UnidadeNegocioId_CodigoErp",
                table: "FiliaisMetadados",
                columns: new[] { "UnidadeNegocioId", "CodigoErp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentrosCustoMetadados");

            migrationBuilder.DropTable(
                name: "FiliaisMetadados");
        }
    }
}
