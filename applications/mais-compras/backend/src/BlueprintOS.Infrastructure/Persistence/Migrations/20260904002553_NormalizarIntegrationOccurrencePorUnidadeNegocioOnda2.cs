using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarIntegrationOccurrencePorUnidadeNegocioOnda2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): mesma disciplina de
            // backfill seguro da migration de Fornecedor — nunca Guid.Empty. A tabela IntegrationOccurrences
            // é recente (Bloco 5A.9) e pode estar vazia em muitos ambientes, mas o backfill abaixo é seguro
            // de qualquer forma (UPDATE sobre zero linhas é inócuo).
            migrationBuilder.DropIndex(
                name: "IX_IntegrationOccurrences_Dataset_Status",
                table: "IntegrationOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationOccurrences_Dedup",
                table: "IntegrationOccurrences");

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "IntegrationOccurrences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
DECLARE @Total INT = (SELECT COUNT(*) FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');
IF @Total <> 1
BEGIN
    DECLARE @Msg NVARCHAR(400) = N'Onda 2 — backfill de IntegrationOccurrences.UnidadeNegocioId exige exatamente 1 Unidade de Negocio com Slug = ''grupo-soma'' para resolver Grupo Soma automaticamente; encontrado ' + CAST(@Total AS NVARCHAR(10)) + N'. Resolva manualmente antes de reexecutar esta migration.';
    THROW 51000, @Msg, 1;
END

DECLARE @GrupoSomaId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');

UPDATE [IntegrationOccurrences] SET [UnidadeNegocioId] = @GrupoSomaId WHERE [UnidadeNegocioId] IS NULL;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "IntegrationOccurrences",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_Dedup",
                table: "IntegrationOccurrences",
                columns: new[] { "UnidadeNegocioId", "ExecutionId", "Dataset", "Stage", "Code", "OriginRecordKey" },
                unique: true,
                filter: "[OriginRecordKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_UnidadeNegocioId_Dataset_Status",
                table: "IntegrationOccurrences",
                columns: new[] { "UnidadeNegocioId", "Dataset", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntegrationOccurrences_Dedup",
                table: "IntegrationOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationOccurrences_UnidadeNegocioId_Dataset_Status",
                table: "IntegrationOccurrences");

            migrationBuilder.DropColumn(
                name: "UnidadeNegocioId",
                table: "IntegrationOccurrences");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_Dataset_Status",
                table: "IntegrationOccurrences",
                columns: new[] { "Dataset", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_Dedup",
                table: "IntegrationOccurrences",
                columns: new[] { "ExecutionId", "Dataset", "Stage", "Code", "OriginRecordKey" },
                unique: true,
                filter: "[OriginRecordKey] IS NOT NULL");
        }
    }
}
