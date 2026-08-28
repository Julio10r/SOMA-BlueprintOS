using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracaoNotificacaoO111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesNotificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeNegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailAtivado = table.Column<bool>(type: "bit", nullable: false),
                    EmailRemetente = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    NomeRemetente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesNotificacao", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesNotificacao_UnidadeNegocioId",
                table: "ConfiguracoesNotificacao",
                column: "UnidadeNegocioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesNotificacao");
        }
    }
}
