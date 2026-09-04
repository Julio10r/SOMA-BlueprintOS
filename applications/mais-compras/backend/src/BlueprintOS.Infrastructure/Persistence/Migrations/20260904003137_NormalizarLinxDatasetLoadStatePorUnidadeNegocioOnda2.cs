using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarLinxDatasetLoadStatePorUnidadeNegocioOnda2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): mesma disciplina de
            // backfill seguro das migrations de Fornecedor/IntegrationOccurrence — nunca Guid.Empty.
            migrationBuilder.DropPrimaryKey(
                name: "PK_LinxDatasetLoadState",
                table: "LinxDatasetLoadState");

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "LinxDatasetLoadState",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
DECLARE @Total INT = (SELECT COUNT(*) FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');
IF @Total <> 1
BEGIN
    DECLARE @Msg NVARCHAR(400) = N'Onda 2 — backfill de LinxDatasetLoadState.UnidadeNegocioId exige exatamente 1 Unidade de Negocio com Slug = ''grupo-soma'' para resolver Grupo Soma automaticamente; encontrado ' + CAST(@Total AS NVARCHAR(10)) + N'. Resolva manualmente antes de reexecutar esta migration.';
    THROW 51000, @Msg, 1;
END

DECLARE @GrupoSomaId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');

UPDATE [LinxDatasetLoadState] SET [UnidadeNegocioId] = @GrupoSomaId WHERE [UnidadeNegocioId] IS NULL;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "LinxDatasetLoadState",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinxDatasetLoadState",
                table: "LinxDatasetLoadState",
                columns: new[] { "UnidadeNegocioId", "Dataset" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LinxDatasetLoadState",
                table: "LinxDatasetLoadState");

            migrationBuilder.DropColumn(
                name: "UnidadeNegocioId",
                table: "LinxDatasetLoadState");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinxDatasetLoadState",
                table: "LinxDatasetLoadState",
                column: "Dataset");
        }
    }
}
