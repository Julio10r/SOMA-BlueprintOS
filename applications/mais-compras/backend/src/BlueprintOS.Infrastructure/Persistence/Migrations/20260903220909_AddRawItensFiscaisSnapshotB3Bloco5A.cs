using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawItensFiscaisSnapshotB3Bloco5A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RAW_LinxItensFiscaisSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoErp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UnidadeErp = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ContaContabilErp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InativoErp = table.Column<bool>(type: "bit", nullable: false),
                    UltimaAlteracao = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxItensFiscaisSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RAW_LinxItensFiscaisSnapshot_CodigoErp",
                table: "RAW_LinxItensFiscaisSnapshot",
                column: "CodigoErp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAW_LinxItensFiscaisSnapshot");
        }
    }
}
