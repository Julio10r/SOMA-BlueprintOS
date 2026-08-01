using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202607310001_B21FornecedorSynchronization")]
public partial class B21FornecedorSynchronization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "BusinessUnit", table: "Fornecedores", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ErpSistema", table: "Fornecedores", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ErpFornecedorId", table: "Fornecedores", type: "nvarchar(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "OrigemInformacao", table: "Fornecedores", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "MaisCompras");
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UltimaSincronizacaoEm", table: "Fornecedores", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StatusSincronizacao", table: "Fornecedores", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pendente");
        migrationBuilder.AddColumn<string>(name: "MensagemErroSincronizacao", table: "Fornecedores", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Fornecedores_BusinessUnit_ErpSistema_ErpFornecedorId", table: "Fornecedores", columns: new[] { "BusinessUnit", "ErpSistema", "ErpFornecedorId" }, unique: true, filter: "[BusinessUnit] IS NOT NULL AND [ErpSistema] IS NOT NULL AND [ErpFornecedorId] IS NOT NULL");
        migrationBuilder.CreateTable(name: "FornecedoresSincronizacoes", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            BusinessUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            ErpSistema = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            ErpFornecedorId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            Direcao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            ExecutadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            MensagemErro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
        }, constraints: table => table.PrimaryKey("PK_FornecedoresSincronizacoes", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_FornecedoresSincronizacoes_BusinessUnit_ErpSistema_ErpFornecedorId_ExecutadaEm", table: "FornecedoresSincronizacoes", columns: new[] { "BusinessUnit", "ErpSistema", "ErpFornecedorId", "ExecutadaEm" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FornecedoresSincronizacoes");
        migrationBuilder.DropIndex(name: "IX_Fornecedores_BusinessUnit_ErpSistema_ErpFornecedorId", table: "Fornecedores");
        migrationBuilder.DropColumn(name: "BusinessUnit", table: "Fornecedores"); migrationBuilder.DropColumn(name: "ErpSistema", table: "Fornecedores"); migrationBuilder.DropColumn(name: "ErpFornecedorId", table: "Fornecedores");
        migrationBuilder.DropColumn(name: "OrigemInformacao", table: "Fornecedores"); migrationBuilder.DropColumn(name: "UltimaSincronizacaoEm", table: "Fornecedores"); migrationBuilder.DropColumn(name: "StatusSincronizacao", table: "Fornecedores"); migrationBuilder.DropColumn(name: "MensagemErroSincronizacao", table: "Fornecedores");
    }
}
