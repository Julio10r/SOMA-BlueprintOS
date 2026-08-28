using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadeNegocioIdToSincronizacaoFornecedorDEB03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeNegocioId",
                table: "SincronizacoesFornecedores",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // DEB-03 (Gate Final da Onda 1) — backfill de dados historicos: ao gerar esta migration existe
            // apenas uma UnidadeNegocio real no ambiente (Bootstrap, "Grupo Soma"), portanto toda execucao
            // de sincronizacao pre-existente e atribuida a ela. Se mais de uma UnidadeNegocio existir no
            // ambiente de destino, esta atualizacao nao decide qual delas e a correta — nesse caso a
            // migration deve ser revisada manualmente antes de aplicar (nao ha heuristica segura aqui).
            migrationBuilder.Sql(
                """
                UPDATE SincronizacoesFornecedores
                SET UnidadeNegocioId = (SELECT TOP 1 Id FROM UnidadesNegocio)
                WHERE UnidadeNegocioId = '00000000-0000-0000-0000-000000000000'
                AND (SELECT COUNT(*) FROM UnidadesNegocio) = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SincronizacoesFornecedores_UnidadeNegocioId",
                table: "SincronizacoesFornecedores",
                column: "UnidadeNegocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SincronizacoesFornecedores_UnidadeNegocioId",
                table: "SincronizacoesFornecedores");

            migrationBuilder.DropColumn(
                name: "UnidadeNegocioId",
                table: "SincronizacoesFornecedores");
        }
    }
}
