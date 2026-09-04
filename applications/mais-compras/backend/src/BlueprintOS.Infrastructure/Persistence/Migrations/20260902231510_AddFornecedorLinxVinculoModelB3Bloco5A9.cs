using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFornecedorLinxVinculoModelB3Bloco5A9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JustificativaEncerramento",
                table: "SincronizacoesFornecedores",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioRecuperacaoId",
                table: "SincronizacoesFornecedores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FornecedorLinxVinculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErpSistema = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NomeClifor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InativoFornecedores = table.Column<bool>(type: "bit", nullable: false),
                    InativoCadastroCliFor = table.Column<bool>(type: "bit", nullable: false),
                    DataParaTransferencia = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedorLinxVinculos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorLinxVinculos_ErpSistema_CodigoErp",
                table: "FornecedorLinxVinculos",
                columns: new[] { "ErpSistema", "CodigoErp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorLinxVinculos_FornecedorId",
                table: "FornecedorLinxVinculos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorLinxVinculos_FornecedorId_PrincipalAtivo",
                table: "FornecedorLinxVinculos",
                column: "FornecedorId",
                unique: true,
                filter: "[Principal] = 1 AND [InativoFornecedores] = 0 AND [InativoCadastroCliFor] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FornecedorLinxVinculos");

            migrationBuilder.DropColumn(
                name: "JustificativaEncerramento",
                table: "SincronizacoesFornecedores");

            migrationBuilder.DropColumn(
                name: "UsuarioRecuperacaoId",
                table: "SincronizacoesFornecedores");
        }
    }
}
