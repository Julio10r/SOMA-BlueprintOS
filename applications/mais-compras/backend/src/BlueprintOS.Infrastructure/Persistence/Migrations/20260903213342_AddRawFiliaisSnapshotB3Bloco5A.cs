using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawFiliaisSnapshotB3Bloco5A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RAW_LinxFiliaisSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoErp = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    DescricaoErp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InativoErp = table.Column<bool>(type: "bit", nullable: true),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxFiliaisSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxFiliaisSnapshot_CodigoErp",
                table: "RAW_LinxFiliaisSnapshot",
                column: "CodigoErp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAW_LinxFiliaisSnapshot");
        }
    }
}
