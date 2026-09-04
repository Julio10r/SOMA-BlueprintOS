using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawLinxFornecedoresSnapshotAndDatasetLoadStateB3Bloco5A9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinxDatasetLoadState",
                columns: table => new
                {
                    Dataset = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CargaFullInicialValidada = table.Column<bool>(type: "bit", nullable: false),
                    IncrementalLiberado = table.Column<bool>(type: "bit", nullable: false),
                    BaselineExecucaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaselineHomologadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimaExecucaoValidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UltimoWatermarkValido = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinxDatasetLoadState", x => x.Dataset);
                });

            migrationBuilder.CreateTable(
                name: "RAW_LinxFornecedoresSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoFornecedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CnpjCpf = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RazaoSocial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TipoPessoa = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    InativoFornecedores = table.Column<bool>(type: "bit", nullable: false),
                    InativoCadastroCliFor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxFornecedoresSnapshot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RAW_LinxFornecedoresSnapshotExecucoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dataset = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IniciadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Completa = table.Column<bool>(type: "bit", nullable: false),
                    LinhasLidas = table.Column<long>(type: "bigint", nullable: false),
                    LinhasGravadas = table.Column<long>(type: "bigint", nullable: false),
                    IsolamentoUtilizado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Erro = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WatermarkInicial = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WatermarkFinal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReconciliacaoStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReconciliadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxFornecedoresSnapshotExecucoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxFornecedoresSnapshot_CnpjCpf",
                table: "RAW_LinxFornecedoresSnapshot",
                column: "CnpjCpf");

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxFornecedoresSnapshotExecucoes_Dataset_IniciadoEm",
                table: "RAW_LinxFornecedoresSnapshotExecucoes",
                columns: new[] { "Dataset", "IniciadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxFornecedoresSnapshotExecucoes_Dataset_Modo_ReconciliacaoStatus",
                table: "RAW_LinxFornecedoresSnapshotExecucoes",
                columns: new[] { "Dataset", "Modo", "ReconciliacaoStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinxDatasetLoadState");

            migrationBuilder.DropTable(
                name: "RAW_LinxFornecedoresSnapshot");

            migrationBuilder.DropTable(
                name: "RAW_LinxFornecedoresSnapshotExecucoes");
        }
    }
}
