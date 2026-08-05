using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202607300002_B2FornecedorDiscovery")]
public partial class B2FornecedorDiscovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FornecedoresDescobertos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CodigoItem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Categoria = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                CodigoFornecedor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                Criterio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                TemporaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DescobertoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_FornecedoresDescobertos", x => x.Id));
        migrationBuilder.CreateIndex("IX_FornecedoresDescobertos_TemporaryUserId_DescobertoEm", "FornecedoresDescobertos", new[] { "TemporaryUserId", "DescobertoEm" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("FornecedoresDescobertos");
}
