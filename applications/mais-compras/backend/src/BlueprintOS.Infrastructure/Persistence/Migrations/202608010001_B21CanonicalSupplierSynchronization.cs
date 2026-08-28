using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlueprintOSDbContext))]
[Migration("202608010001_B21CanonicalSupplierSynchronization")]
public partial class B21CanonicalSupplierSynchronization : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        var supplier = new (string Name, string Type, int? Length)[]
        {
            ("NomeFantasia", "nvarchar", 200), ("TipoPessoa", "nvarchar", 20), ("InscricaoEstadual", "nvarchar", 40), ("InscricaoMunicipal", "nvarchar", 40),
            ("Cep", "nvarchar", 12), ("Logradouro", "nvarchar", 200), ("Numero", "nvarchar", 30), ("Complemento", "nvarchar", 100), ("Bairro", "nvarchar", 100),
            ("CodigoMunicipio", "nvarchar", 30), ("Ddd", "nvarchar", 5), ("EmailFiscal", "nvarchar", 254), ("Banco", "nvarchar", 20), ("Agencia", "nvarchar", 20),
            ("Conta", "nvarchar", 30), ("DigitosConta", "nvarchar", 5), ("CondicaoPagamento", "nvarchar", 80), ("TipoFornecedor", "nvarchar", 80),
            ("SubtipoFornecedor", "nvarchar", 80), ("ContaContabil", "nvarchar", 80), ("RegimeFiscal", "nvarchar", 80), ("CategoriasFornecimento", "nvarchar", 500),
            ("HashDadosSincronizaveis", "nvarchar", 128), ("OrigemUltimaAlteracao", "nvarchar", 30)
        };
        foreach (var column in supplier) m.AddColumn<string>(column.Name, "Fornecedores", type: $"{column.Type}({column.Length})", maxLength: column.Length, nullable: true);
        m.AddColumn<bool>("ForneceMateriais", "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<bool>("ForneceConsumo", "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<bool>("ForneceServicos", "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<bool>("ForneceProdutos", "Fornecedores", type: "bit", nullable: false, defaultValue: false);
        m.AddColumn<bool?>("SimplesNacional", "Fornecedores", type: "bit", nullable: true);
        m.AddColumn<int>("Versao", "Fornecedores", type: "int", nullable: false, defaultValue: 1);
        var audit = new (string Name, string Type, int? Length)[]
        {
            ("Origem", "nvarchar", 30), ("Destino", "nvarchar", 30), ("TimestampComprasOriginal", "nvarchar", 80), ("TimestampErpOriginal", "nvarchar", 80),
            ("TimestampComprasNormalizado", "nvarchar", 80), ("TimestampErpNormalizado", "nvarchar", 80), ("Decisao", "nvarchar", 40), ("CamposAlterados", "nvarchar", 1000),
            ("DadosAntes", "nvarchar", 8000), ("DadosDepois", "nvarchar", 8000), ("HashAntes", "nvarchar", 128), ("HashDepois", "nvarchar", 128)
        };
        foreach (var column in audit) m.AddColumn<string>(column.Name, "FornecedoresSincronizacoes", type: column.Name is "DadosAntes" or "DadosDepois" ? "nvarchar(max)" : $"{column.Type}({column.Length})", maxLength: column.Name is "DadosAntes" or "DadosDepois" ? null : column.Length, nullable: column.Name == "Decisao" ? false : true, defaultValue: column.Name == "Decisao" ? "Alterado" : null);
        m.AddColumn<int>("Tentativa", "FornecedoresSincronizacoes", type: "int", nullable: false, defaultValue: 1);
        m.AddColumn<int>("DuracaoMs", "FornecedoresSincronizacoes", type: "int", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropColumn("ForneceMateriais", "Fornecedores"); m.DropColumn("ForneceConsumo", "Fornecedores"); m.DropColumn("ForneceServicos", "Fornecedores"); m.DropColumn("ForneceProdutos", "Fornecedores"); m.DropColumn("SimplesNacional", "Fornecedores"); m.DropColumn("Versao", "Fornecedores");
        foreach (var c in new[] { "NomeFantasia", "TipoPessoa", "InscricaoEstadual", "InscricaoMunicipal", "Cep", "Logradouro", "Numero", "Complemento", "Bairro", "CodigoMunicipio", "Ddd", "EmailFiscal", "Banco", "Agencia", "Conta", "DigitosConta", "CondicaoPagamento", "TipoFornecedor", "SubtipoFornecedor", "ContaContabil", "RegimeFiscal", "CategoriasFornecimento", "HashDadosSincronizaveis", "OrigemUltimaAlteracao" }) m.DropColumn(c, "Fornecedores");
        foreach (var c in new[] { "Origem", "Destino", "TimestampComprasOriginal", "TimestampErpOriginal", "TimestampComprasNormalizado", "TimestampErpNormalizado", "Decisao", "CamposAlterados", "DadosAntes", "DadosDepois", "HashAntes", "HashDepois", "Tentativa", "DuracaoMs" }) m.DropColumn(c, "FornecedoresSincronizacoes");
    }
}
