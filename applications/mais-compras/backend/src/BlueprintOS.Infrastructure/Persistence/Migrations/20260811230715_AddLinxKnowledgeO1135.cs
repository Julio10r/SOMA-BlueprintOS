using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLinxKnowledgeO1135 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinxConhecimentoEntradas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoRaizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntradaAnteriorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    Especialista = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Proveniencia = table.Column<int>(type: "int", nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ator = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinxConhecimentoEntradas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Codigo", "Descricao" },
                values: new object[,]
                {
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000012"), "ConhecimentoLinx.Gerenciar", "Registrar descobertas/inferências e validar conhecimento dos Agents Especialistas Linx" },
                    { new Guid("b1a5c4e0-0001-4a10-9f01-000000000013"), "ConhecimentoLinx.Aprovar", "Promover conhecimento dos Agents Especialistas Linx a 'Aprovado'" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinxConhecimentoEntradas_Especialista_Categoria",
                table: "LinxConhecimentoEntradas",
                columns: new[] { "Especialista", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_LinxConhecimentoEntradas_UnidadeNegocioId",
                table: "LinxConhecimentoEntradas",
                column: "UnidadeNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_LinxConhecimentoEntradas_VersaoRaizId",
                table: "LinxConhecimentoEntradas",
                column: "VersaoRaizId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinxConhecimentoEntradas");

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000012"));

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: new Guid("b1a5c4e0-0001-4a10-9f01-000000000013"));
        }
    }
}
