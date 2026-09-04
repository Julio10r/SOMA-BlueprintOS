using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarMetadadosApoioPorUnidadeNegocioOnda2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnidadesMedidaMetadados_CodigoErp",
                table: "UnidadesMedidaMetadados");

            migrationBuilder.DropIndex(
                name: "IX_ContasContabeisMetadados_CodigoErp",
                table: "ContasContabeisMetadados");

            migrationBuilder.DropIndex(
                name: "IX_CentrosCustoMetadados_CodigoErp",
                table: "CentrosCustoMetadados");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedidaMetadados_UnidadeNegocioId_CodigoErp",
                table: "UnidadesMedidaMetadados",
                columns: new[] { "UnidadeNegocioId", "CodigoErp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasContabeisMetadados_UnidadeNegocioId_CodigoErp",
                table: "ContasContabeisMetadados",
                columns: new[] { "UnidadeNegocioId", "CodigoErp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoMetadados_UnidadeNegocioId_CodigoErp",
                table: "CentrosCustoMetadados",
                columns: new[] { "UnidadeNegocioId", "CodigoErp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnidadesMedidaMetadados_UnidadeNegocioId_CodigoErp",
                table: "UnidadesMedidaMetadados");

            migrationBuilder.DropIndex(
                name: "IX_ContasContabeisMetadados_UnidadeNegocioId_CodigoErp",
                table: "ContasContabeisMetadados");

            migrationBuilder.DropIndex(
                name: "IX_CentrosCustoMetadados_UnidadeNegocioId_CodigoErp",
                table: "CentrosCustoMetadados");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedidaMetadados_CodigoErp",
                table: "UnidadesMedidaMetadados",
                column: "CodigoErp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasContabeisMetadados_CodigoErp",
                table: "ContasContabeisMetadados",
                column: "CodigoErp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoMetadados_CodigoErp",
                table: "CentrosCustoMetadados",
                column: "CodigoErp",
                unique: true);
        }
    }
}
