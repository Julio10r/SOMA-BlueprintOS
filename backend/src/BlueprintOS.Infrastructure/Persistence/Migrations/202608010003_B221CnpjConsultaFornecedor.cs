using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202608010003_B221CnpjConsultaFornecedor")]
public partial class B221CnpjConsultaFornecedor : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "FornecedoresCnpjConsultas", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Cnpj_Cpf = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: false),
            FonteConsulta = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            DataConsulta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            Usuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            Resultado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            MensagemErro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
            CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            BusinessUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            ErpSistema = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
        }, constraints: table => table.PrimaryKey("PK_FornecedoresCnpjConsultas", x => x.Id));
        m.CreateIndex(name: "IX_FornecedoresCnpjConsultas_BusinessUnit_Cnpj_Cpf_DataConsulta", table: "FornecedoresCnpjConsultas", columns: new[] { "BusinessUnit", "Cnpj_Cpf", "DataConsulta" });
        m.CreateIndex(name: "IX_FornecedoresCnpjConsultas_CorrelationId", table: "FornecedoresCnpjConsultas", column: "CorrelationId");
    }

    protected override void Down(MigrationBuilder m) => m.DropTable(name: "FornecedoresCnpjConsultas");
}
