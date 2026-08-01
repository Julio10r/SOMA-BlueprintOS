using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202608010004_B223FornecedorEnriquecimentoAnalise")]
public partial class B223FornecedorEnriquecimentoAnalise : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FornecedoresEnriquecimentoAnalises",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Cnpj_Cpf = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: false),
                ConsultaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Campo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ValorAnterior = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ValorNovo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Decisao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Usuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                BusinessUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ErpSistema = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Fonte = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FornecedoresEnriquecimentoAnalises", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_FornecedoresEnriquecimentoAnalises_CorrelationId",
            table: "FornecedoresEnriquecimentoAnalises",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_FornecedoresEnriquecimentoAnalises_FornecedorId_Campo_DataHora",
            table: "FornecedoresEnriquecimentoAnalises",
            columns: new[] { "FornecedorId", "Campo", "DataHora" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FornecedoresEnriquecimentoAnalises");
    }
}
