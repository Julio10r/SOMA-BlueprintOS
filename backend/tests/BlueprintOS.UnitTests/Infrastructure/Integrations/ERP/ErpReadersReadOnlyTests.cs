using System.Reflection;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.ERP;

/// <summary>O1.13.5 — prova estrutural de que o leitor do ERP nunca executa comando de escrita (Work Order,
/// seções 9/26). Read-only aqui não é apenas convenção de código: nenhuma interface de leitura do ERP
/// (Fornecedor/Filial/CentroCusto/descoberta de schema Linx) expõe um único método fora do vocabulário de
/// leitura — quem só possui a interface injetada não consegue, nem por engano, chamar uma operação de
/// escrita, porque ela simplesmente não existe no contrato.</summary>
public sealed class ErpReadersReadOnlyTests
{
    private static readonly string[] VerbosDeLeituraPermitidos = ["Buscar", "Listar", "Obter"];

    private static readonly Type[] InterfacesDeLeituraDoErp =
    [
        typeof(IFornecedorErpReader),
        typeof(IFilialErpReader),
        typeof(ICentroCustoErpReader),
        typeof(ILinxSchemaDiscoveryReader),
    ];

    private static readonly string[] VerbosDeEscritaProibidos =
    [
        "Insert", "Update", "Delete", "Merge", "Alter", "Drop", "Create", "Truncate", "Grant", "Revoke", "Exec",
        "Inserir", "Atualizar", "Excluir", "Deletar", "Gravar", "Alterar", "Remover", "Escrever", "Salvar", "Persistir",
    ];

    [Theory]
    [MemberData(nameof(Interfaces))]
    public void ErpReader_Interface_Should_Only_Expose_Read_Verbs(Type interfaceType)
    {
        var metodos = interfaceType.GetMethods();
        Assert.NotEmpty(metodos);

        foreach (var metodo in metodos)
        {
            Assert.True(
                VerbosDeLeituraPermitidos.Any(verbo => metodo.Name.StartsWith(verbo, StringComparison.Ordinal)),
                $"O método '{interfaceType.Name}.{metodo.Name}' não começa com um verbo de leitura permitido ({string.Join(", ", VerbosDeLeituraPermitidos)}).");
        }
    }

    [Theory]
    [MemberData(nameof(Interfaces))]
    public void ErpReader_Interface_Should_Never_Expose_A_Write_Capable_Method_Name(Type interfaceType)
    {
        foreach (var metodo in interfaceType.GetMethods())
        {
            Assert.True(
                VerbosDeEscritaProibidos.All(verbo => !metodo.Name.Contains(verbo, StringComparison.OrdinalIgnoreCase)),
                $"O método '{interfaceType.Name}.{metodo.Name}' contém um verbo de escrita proibido.");
        }
    }

    public static IEnumerable<object[]> Interfaces() => InterfacesDeLeituraDoErp.Select(t => new object[] { t });

    /// <summary>Reforça a guarda por instância real: nenhum método público de <see cref="LinxSchemaDiscoveryReader"/>
    /// (a classe concreta usada pelo Linx Database Specialist) além dos já declarados na interface — a
    /// classe não amplia a superfície com um método de escrita "extra" não coberto pelo teste acima.</summary>
    [Fact]
    public void LinxSchemaDiscoveryReader_Should_Not_Expose_Any_Public_Method_Beyond_The_Read_Only_Contract()
    {
        var metodosDaInterface = typeof(ILinxSchemaDiscoveryReader).GetMethods().Select(m => m.Name).ToHashSet();
        var metodosPublicosDaClasse = typeof(LinxSchemaDiscoveryReader)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // exclui getters/setters de propriedades
            .Select(m => m.Name)
            .ToHashSet();

        Assert.Subset(metodosDaInterface, metodosPublicosDaClasse);
        Assert.Subset(metodosPublicosDaClasse, metodosDaInterface);
    }

    /// <summary>Guarda-fim-a-fim adicional: o leitor recusa-se a abrir conexão contra qualquer banco que não
    /// seja `SOMA_DESENV` — mesmo padrão já validado em <c>SomaFilialReader</c>/<c>SomaCentroCustoReader</c>
    /// (B2.1/O1.7). Não requer conectividade real: a checagem de nome de banco acontece antes de qualquer
    /// tentativa de abrir a conexão.</summary>
    [Fact]
    public async Task LinxSchemaDiscoveryReader_Should_Refuse_To_Open_Against_A_Database_Other_Than_SOMA_DESENV()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ErpConnection"] = "Server=localhost;Database=OUTRO_BANCO;User Id=x;Password=x;TrustServerCertificate=True",
            })
            .Build();

        var reader = new LinxSchemaDiscoveryReader(configuracao, NullLogger<LinxSchemaDiscoveryReader>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ListarTabelasAsync(null));
        Assert.Contains("SOMA_DESENV", ex.Message);
    }

    [Fact]
    public async Task LinxSchemaDiscoveryReader_Should_Fail_Closed_When_Connection_String_Is_Not_Configured()
    {
        var configuracao = new ConfigurationBuilder().Build();
        var reader = new LinxSchemaDiscoveryReader(configuracao, NullLogger<LinxSchemaDiscoveryReader>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ListarTabelasAsync(null));
    }
}
