using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCentroCustoUnidadeAlocacaoO19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CentrosCustoUnidadesAlocacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroCustoMetadadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeAlocacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Padrao = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosCustoUnidadesAlocacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentrosCustoUnidadesAlocacao_CentrosCustoMetadados_CentroCustoMetadadoId",
                        column: x => x.CentroCustoMetadadoId,
                        principalTable: "CentrosCustoMetadados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CentrosCustoUnidadesAlocacao_UnidadesAlocacao_UnidadeAlocacaoId",
                        column: x => x.UnidadeAlocacaoId,
                        principalTable: "UnidadesAlocacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoUnidadesAlocacao_CentroCustoMetadadoId_Padrao",
                table: "CentrosCustoUnidadesAlocacao",
                column: "CentroCustoMetadadoId",
                unique: true,
                filter: "[Padrao] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoUnidadesAlocacao_CentroCustoMetadadoId_UnidadeAlocacaoId",
                table: "CentrosCustoUnidadesAlocacao",
                columns: new[] { "CentroCustoMetadadoId", "UnidadeAlocacaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCustoUnidadesAlocacao_UnidadeAlocacaoId",
                table: "CentrosCustoUnidadesAlocacao",
                column: "UnidadeAlocacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentrosCustoUnidadesAlocacao");
        }
    }
}
