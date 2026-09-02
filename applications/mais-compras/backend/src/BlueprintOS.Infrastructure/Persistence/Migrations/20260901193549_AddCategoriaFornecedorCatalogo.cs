using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaFornecedorCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasFornecedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasFornecedor", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CategoriasFornecedor",
                columns: new[] { "Id", "Ativo", "Codigo", "Descricao" },
                values: new object[,]
                {
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000001"), true, "MATERIA_PRIMA", "Matéria-Prima" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000002"), true, "EMBALAGEM", "Embalagem" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000003"), true, "SERVICOS_GERAIS", "Serviços Gerais" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000004"), true, "TRANSPORTE_LOGISTICA", "Transporte e Logística" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000005"), true, "MARKETING_PUBLICIDADE", "Marketing e Publicidade" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000006"), true, "TECNOLOGIA_INFORMACAO", "Tecnologia da Informação" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000007"), true, "MANUTENCAO_FACILITIES", "Manutenção e Facilities" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000008"), true, "CONSULTORIA", "Consultoria" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-000000000009"), true, "EQUIPAMENTOS", "Equipamentos" },
                    { new Guid("8a9f1b1a-0001-4a00-9a00-00000000000a"), true, "OUTROS", "Outros" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasFornecedor_Codigo",
                table: "CategoriasFornecedor",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriasFornecedor");
        }
    }
}
