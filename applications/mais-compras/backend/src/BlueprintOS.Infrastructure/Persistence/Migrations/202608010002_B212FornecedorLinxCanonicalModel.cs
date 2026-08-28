using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202608010002_B212FornecedorLinxCanonicalModel")]
public partial class B212FornecedorLinxCanonicalModel : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.DropIndex(name: "IX_Fornecedores_Cnpj", table: "Fornecedores");
        m.RenameColumn(name: "Cnpj", table: "Fornecedores", newName: "Cnpj_Cpf");
        m.RenameColumn(name: "Nome", table: "Fornecedores", newName: "RazaoSocial");
        m.AlterColumn<string>(name: "Cnpj_Cpf", table: "Fornecedores", type: "varchar(14)", maxLength: 14, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(14)", oldMaxLength: 14);
        m.AddColumn<bool>(name: "Beneficiador", table: "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<bool>(name: "Licenciado", table: "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<Guid>(name: "CondicaoPagamentoDominioId", table: "Fornecedores", type: "uniqueidentifier", nullable: true);
        m.AddColumn<Guid>(name: "TipoFornecedorDominioId", table: "Fornecedores", type: "uniqueidentifier", nullable: true);
        m.AddColumn<Guid>(name: "SubtipoFornecedorDominioId", table: "Fornecedores", type: "uniqueidentifier", nullable: true);
        m.CreateTable(name: "FornecedoresDominiosErp", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            CodigoERP = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            BusinessUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            ErpSistema = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            UltimaSincronizacaoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_FornecedoresDominiosErp", x => x.Id));
        m.CreateIndex(name: "IX_Fornecedores_Cnpj_Cpf", table: "Fornecedores", column: "Cnpj_Cpf", unique: true);
        m.CreateIndex(name: "IX_Fornecedores_RazaoSocial", table: "Fornecedores", column: "RazaoSocial");
        m.CreateIndex(name: "IX_Fornecedores_CondicaoPagamentoDominioId", table: "Fornecedores", column: "CondicaoPagamentoDominioId");
        m.CreateIndex(name: "IX_Fornecedores_TipoFornecedorDominioId", table: "Fornecedores", column: "TipoFornecedorDominioId");
        m.CreateIndex(name: "IX_Fornecedores_SubtipoFornecedorDominioId", table: "Fornecedores", column: "SubtipoFornecedorDominioId");
        m.CreateIndex(name: "IX_FornecedoresDominiosErp_Tipo_BusinessUnit_ErpSistema_CodigoERP", table: "FornecedoresDominiosErp", columns: new[] { "Tipo", "BusinessUnit", "ErpSistema", "CodigoERP" }, unique: true);
        m.AddForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_CondicaoPagamentoDominioId", table: "Fornecedores", column: "CondicaoPagamentoDominioId", principalTable: "FornecedoresDominiosErp", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_TipoFornecedorDominioId", table: "Fornecedores", column: "TipoFornecedorDominioId", principalTable: "FornecedoresDominiosErp", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_SubtipoFornecedorDominioId", table: "Fornecedores", column: "SubtipoFornecedorDominioId", principalTable: "FornecedoresDominiosErp", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_CondicaoPagamentoDominioId", table: "Fornecedores");
        m.DropForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_TipoFornecedorDominioId", table: "Fornecedores");
        m.DropForeignKey(name: "FK_Fornecedores_FornecedoresDominiosErp_SubtipoFornecedorDominioId", table: "Fornecedores");
        m.DropTable(name: "FornecedoresDominiosErp");
        m.DropIndex(name: "IX_Fornecedores_Cnpj_Cpf", table: "Fornecedores");
        m.DropIndex(name: "IX_Fornecedores_RazaoSocial", table: "Fornecedores");
        m.DropIndex(name: "IX_Fornecedores_CondicaoPagamentoDominioId", table: "Fornecedores");
        m.DropIndex(name: "IX_Fornecedores_TipoFornecedorDominioId", table: "Fornecedores");
        m.DropIndex(name: "IX_Fornecedores_SubtipoFornecedorDominioId", table: "Fornecedores");
        m.DropColumn(name: "Beneficiador", table: "Fornecedores");
        m.DropColumn(name: "Licenciado", table: "Fornecedores");
        m.DropColumn(name: "CondicaoPagamentoDominioId", table: "Fornecedores");
        m.DropColumn(name: "TipoFornecedorDominioId", table: "Fornecedores");
        m.DropColumn(name: "SubtipoFornecedorDominioId", table: "Fornecedores");
        m.AlterColumn<string>(name: "Cnpj_Cpf", table: "Fornecedores", type: "nvarchar(14)", maxLength: 14, nullable: false, oldClrType: typeof(string), oldType: "varchar(14)", oldMaxLength: 14);
        m.RenameColumn(name: "Cnpj_Cpf", table: "Fornecedores", newName: "Cnpj");
        m.RenameColumn(name: "RazaoSocial", table: "Fornecedores", newName: "Nome");
        m.CreateIndex(name: "IX_Fornecedores_Cnpj", table: "Fornecedores", column: "Cnpj", unique: true);
    }
}
