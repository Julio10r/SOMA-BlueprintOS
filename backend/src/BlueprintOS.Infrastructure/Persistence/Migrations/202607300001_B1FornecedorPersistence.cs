using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

/// <summary>Initial SQL Server schema for the B1 supplier aggregate.</summary>
[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202607300001_B1FornecedorPersistence")]
public partial class B1FornecedorPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Fornecedores",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                Telefone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ScoreIA = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                TemporaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Fornecedores", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Fornecedores_Cnpj", table: "Fornecedores", column: "Cnpj", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Fornecedores_Nome", table: "Fornecedores", column: "Nome");
        migrationBuilder.CreateIndex(name: "IX_Fornecedores_TemporaryUserId", table: "Fornecedores", column: "TemporaryUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Fornecedores");
}
