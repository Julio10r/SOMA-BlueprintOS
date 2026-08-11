using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracaoTecnicaO111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesErp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SistemaErp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParametrosConexaoProtegidos = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesErp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagsUnidadesNegocio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureFlagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagsUnidadesNegocio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DominiosAutorizadosCsv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    ParametrosProtegidos = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesErp_UnidadeNegocioId",
                table: "ConfiguracoesErp",
                column: "UnidadeNegocioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Nome",
                table: "FeatureFlags",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagsUnidadesNegocio_FeatureFlagId_UnidadeNegocioId",
                table: "FeatureFlagsUnidadesNegocio",
                columns: new[] { "FeatureFlagId", "UnidadeNegocioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviders_UnidadeNegocioId",
                table: "IdentityProviders",
                column: "UnidadeNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_Chave_Global",
                table: "Parametros",
                column: "Chave",
                unique: true,
                filter: "[UnidadeNegocioId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_Chave_UnidadeNegocioId",
                table: "Parametros",
                columns: new[] { "Chave", "UnidadeNegocioId" },
                unique: true,
                filter: "[UnidadeNegocioId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesErp");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "FeatureFlagsUnidadesNegocio");

            migrationBuilder.DropTable(
                name: "IdentityProviders");

            migrationBuilder.DropTable(
                name: "Parametros");
        }
    }
}
