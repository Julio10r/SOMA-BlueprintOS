using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministracaoWorkflowAlcadaOrcamentoO112 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlcadasAprovacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Criterio = table.Column<int>(type: "int", nullable: false),
                    ValorMinimo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CentroCustoMetadadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    AprovadorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadorPerfilId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlcadasAprovacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasOrcamentarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroCustoMetadadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorLimite = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Periodo = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasOrcamentarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasWorkflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoProcesso = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasWorkflow", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Codigo", "Descricao" },
                values: new object[,]
                {
                    { new Guid("b1a5c4e0-0001-4a10-9f01-00000000000f"), "Workflow.Gerenciar", "Criar, editar e ativar/inativar Regras de Workflow por Unidade de Negócio" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000010"), "Alcada.Gerenciar", "Criar, editar e ativar/inativar Alçadas de Aprovação por Unidade de Negócio" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000011"), "Orcamento.Gerenciar", "Criar, editar e ativar/inativar Regras Orçamentárias por Unidade de Negócio" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlcadasAprovacao_UnidadeNegocioId",
                table: "AlcadasAprovacao",
                column: "UnidadeNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegrasOrcamentarias_UnidadeNegocioId_CentroCustoMetadadoId_Periodo",
                table: "RegrasOrcamentarias",
                columns: new[] { "UnidadeNegocioId", "CentroCustoMetadadoId", "Periodo" });

            migrationBuilder.CreateIndex(
                name: "IX_RegrasWorkflow_UnidadeNegocioId",
                table: "RegrasWorkflow",
                column: "UnidadeNegocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlcadasAprovacao");

            migrationBuilder.DropTable(
                name: "RegrasOrcamentarias");

            migrationBuilder.DropTable(
                name: "RegrasWorkflow");

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-00000000000f"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000010"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000011"));
        }
    }
}
