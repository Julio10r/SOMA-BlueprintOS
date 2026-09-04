using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarFornecedorPorUnidadeNegocioOnda2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): esta migration NÃO usa
            // Guid.Empty como default (o scaffold automático do EF Core geraria exatamente isso para uma
            // coluna Guid NOT NULL sem default explícito) — backfill de UnidadeNegocioId deve sempre
            // resolver a Unidade de Negócio real (Grupo Soma), nunca um GUID inventado. Colunas nascem
            // NULLABLE, são preenchidas por SQL que resolve o dado real, e só então se tornam NOT NULL.
            migrationBuilder.DropIndex(
                name: "IX_FornecedorLinxVinculos_ErpSistema_CodigoErp",
                table: "FornecedorLinxVinculos");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_Cnpj_Cpf",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_ErpSistema_ErpFornecedorId",
                table: "Fornecedores");

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "FornecedorLinxVinculos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "Fornecedores",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill seguro: resolve a Unidade de Negócio real pelo slug 'grupo-soma' — nunca por
            // "a única linha da tabela" (o ambiente pode legitimamente ter mais de uma Unidade de Negócio,
            // ex.: fixtures de homologação/Gate) nem por um Guid hardcoded. Falha fechado (THROW) se o slug
            // 'grupo-soma' não existir ou não for único no ambiente onde a migration for aplicada, em vez de
            // adivinhar qual registro é Grupo Soma (regra explícita do Product Owner: em caso de dúvida,
            // parar antes de qualquer escrita destrutiva).
            migrationBuilder.Sql(@"
DECLARE @Total INT = (SELECT COUNT(*) FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');
IF @Total <> 1
BEGIN
    DECLARE @Msg NVARCHAR(400) = N'Onda 2 — backfill de Fornecedores/FornecedorLinxVinculos.UnidadeNegocioId exige exatamente 1 Unidade de Negocio com Slug = ''grupo-soma'' para resolver Grupo Soma automaticamente; encontrado ' + CAST(@Total AS NVARCHAR(10)) + N'. Resolva manualmente (identifique a UnidadeNegocio real de Grupo Soma) antes de reexecutar esta migration.';
    THROW 51000, @Msg, 1;
END

DECLARE @GrupoSomaId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [UnidadesNegocio] WHERE [Slug] = N'grupo-soma');

UPDATE [Fornecedores] SET [UnidadeNegocioId] = @GrupoSomaId WHERE [UnidadeNegocioId] IS NULL;
UPDATE [FornecedorLinxVinculos] SET [UnidadeNegocioId] = @GrupoSomaId WHERE [UnidadeNegocioId] IS NULL;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "FornecedorLinxVinculos",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "Fornecedores",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorLinxVinculos_UnidadeNegocioId_ErpSistema_CodigoErp",
                table: "FornecedorLinxVinculos",
                columns: new[] { "UnidadeNegocioId", "ErpSistema", "CodigoErp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_UnidadeNegocioId_Cnpj_Cpf",
                table: "Fornecedores",
                columns: new[] { "UnidadeNegocioId", "Cnpj_Cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_UnidadeNegocioId_ErpSistema_ErpFornecedorId",
                table: "Fornecedores",
                columns: new[] { "UnidadeNegocioId", "ErpSistema", "ErpFornecedorId" },
                unique: true,
                filter: "[ErpFornecedorId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FornecedorLinxVinculos_UnidadeNegocioId_ErpSistema_CodigoErp",
                table: "FornecedorLinxVinculos");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_UnidadeNegocioId_Cnpj_Cpf",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_UnidadeNegocioId_ErpSistema_ErpFornecedorId",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "UnidadeNegocioId",
                table: "FornecedorLinxVinculos");

            migrationBuilder.DropColumn(
                name: "UnidadeNegocioId",
                table: "Fornecedores");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorLinxVinculos_ErpSistema_CodigoErp",
                table: "FornecedorLinxVinculos",
                columns: new[] { "ErpSistema", "CodigoErp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Cnpj_Cpf",
                table: "Fornecedores",
                column: "Cnpj_Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_ErpSistema_ErpFornecedorId",
                table: "Fornecedores",
                columns: new[] { "ErpSistema", "ErpFornecedorId" },
                unique: true,
                filter: "[ErpFornecedorId] IS NOT NULL");
        }
    }
}
