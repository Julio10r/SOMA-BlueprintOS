using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202608020001_B213FornecedorErpSyncHardening")]
public partial class B213FornecedorErpSyncHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SincronizacoesFornecedores",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SistemaOrigem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                BusinessUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                DataInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DataFim = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                TotalConsultado = table.Column<int>(type: "int", nullable: false),
                TotalIncluido = table.Column<int>(type: "int", nullable: false),
                TotalAtualizado = table.Column<int>(type: "int", nullable: false),
                TotalSemAlteracao = table.Column<int>(type: "int", nullable: false),
                TotalErro = table.Column<int>(type: "int", nullable: false),
                TempoExecucaoMs = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SincronizacoesFornecedores", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ErrosSincronizacoesFornecedores",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SincronizacaoFornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FornecedorIdentificacao = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                Mensagem = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                StackTrace = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ErrosSincronizacoesFornecedores", x => x.Id);
                table.ForeignKey(
                    name: "FK_ErrosSincronizacoesFornecedores_SincronizacoesFornecedores_SincronizacaoFornecedorId",
                    column: x => x.SincronizacaoFornecedorId,
                    principalTable: "SincronizacoesFornecedores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ErrosSincronizacoesFornecedores_DataHora",
            table: "ErrosSincronizacoesFornecedores",
            column: "DataHora");

        migrationBuilder.CreateIndex(
            name: "IX_ErrosSincronizacoesFornecedores_SincronizacaoFornecedorId",
            table: "ErrosSincronizacoesFornecedores",
            column: "SincronizacaoFornecedorId");

        migrationBuilder.CreateIndex(
            name: "IX_SincronizacoesFornecedores_BusinessUnit_SistemaOrigem_DataInicio",
            table: "SincronizacoesFornecedores",
            columns: new[] { "BusinessUnit", "SistemaOrigem", "DataInicio" });

        migrationBuilder.CreateIndex(
            name: "IX_SincronizacoesFornecedores_Status",
            table: "SincronizacoesFornecedores",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ErrosSincronizacoesFornecedores");
        migrationBuilder.DropTable(name: "SincronizacoesFornecedores");
    }
}
