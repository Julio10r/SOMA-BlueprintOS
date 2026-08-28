using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Procurement;

/// <summary>DEB-13 reaberta — validação funcional do entregável #41 (Gate Final da Onda 1, continuação
/// 12/08/2026). A migration <c>B212FornecedorLinxCanonicalModel</c> (02/08/2026) renomeou fisicamente as
/// colunas legadas <c>Nome</c>/<c>Cnpj</c> da tabela <c>Fornecedores</c> para <c>RazaoSocial</c>/
/// <c>Cnpj_Cpf</c>, mas <c>FornecedorConfiguration</c> nunca foi atualizada e continuou instruindo o EF a
/// gerar SQL contra os nomes físicos antigos — <c>SELECT [Nome], [Cnpj] ...</c> contra colunas que não
/// existem mais. Nenhum teste com InMemory/SQLite capturava isso, porque o modelo e o "banco" de teste são
/// sempre gerados a partir do mesmo código: os dois sempre concordam entre si, mesmo quando ambos
/// divergem do schema físico real. Só reproduziu ao conectar no banco de desenvolvimento real.
///
/// Este teste usa o provider relacional (<c>UseSqlServer</c>, sem nunca abrir conexão real — apenas a
/// construção do modelo é exercitada) para inspecionar os nomes de coluna FÍSICOS que o EF de fato geraria
/// no SQL, travando a correção: <c>RazaoSocial</c>/<c>Cnpj_Cpf</c> devem mapear para colunas com o
/// MESMO nome (convenção), nunca para <c>Nome</c>/<c>Cnpj</c> (os nomes físicos antigos, já renomeados).</summary>
public sealed class FornecedorColumnMappingTests
{
    [Theory]
    [InlineData(nameof(Fornecedor.RazaoSocial), "RazaoSocial")]
    [InlineData(nameof(Fornecedor.Cnpj_Cpf), "Cnpj_Cpf")]
    public void Should_Map_Property_To_Real_Physical_Column_Name(string propriedade, string colunaFisicaEsperada)
    {
        using var db = CreateContext();

        var entidade = db.Model.FindEntityType(typeof(Fornecedor))!;
        var coluna = entidade.FindProperty(propriedade)!.GetColumnName();

        Assert.Equal(colunaFisicaEsperada, coluna);
    }

    [Fact]
    public void Should_Not_Map_Any_Property_To_The_Old_Renamed_Columns()
    {
        using var db = CreateContext();

        var entidade = db.Model.FindEntityType(typeof(Fornecedor))!;
        var colunas = entidade.GetProperties().Select(p => p.GetColumnName()).ToArray();

        Assert.DoesNotContain("Nome", colunas);
        Assert.DoesNotContain("Cnpj", colunas);
    }

    private static BlueprintOSDbContext CreateContext()
    {
        // Connection string nunca é usada para abrir conexão real — apenas o provider relacional
        // (SqlServer) precisa estar presente para que as convenções de nome de coluna físico rodem.
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseSqlServer("Server=(local);Database=NuncaConectado;Trusted_Connection=True;")
            .Options;
        return new BlueprintOSDbContext(options);
    }
}
