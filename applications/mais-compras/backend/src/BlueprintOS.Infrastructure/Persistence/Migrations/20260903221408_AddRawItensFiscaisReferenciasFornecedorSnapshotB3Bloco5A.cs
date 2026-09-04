using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawItensFiscaisReferenciasFornecedorSnapshotB3Bloco5A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RAW_LinxItensFiscaisReferenciasFornecedorSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoItem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoItemFornecedor = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ErpFornecedorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FornecedoresResolvidos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAW_LinxItensFiscaisReferenciasFornecedorSnapshot", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAW_LinxItensFiscaisReferenciasFornecedorSnapshot");
        }
    }
}
