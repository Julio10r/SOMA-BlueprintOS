using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCliforAndUltimaAlteracaoToRawFornecedoresSnapshotB3Bloco5A9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Clifor",
                table: "RAW_LinxFornecedoresSnapshot",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaAlteracao",
                table: "RAW_LinxFornecedoresSnapshot",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Clifor",
                table: "RAW_LinxFornecedoresSnapshot");

            migrationBuilder.DropColumn(
                name: "UltimaAlteracao",
                table: "RAW_LinxFornecedoresSnapshot");
        }
    }
}
