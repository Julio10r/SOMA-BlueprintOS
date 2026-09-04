using BlueprintOS.Application.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>Prova pura da regra do PO: "não inventar vínculo quando o valor Linx não puder ser resolvido" e
/// "nunca regride um vínculo já resolvido".</summary>
public sealed class FornecedorVinculoDominioResolverTests
{
    [Fact]
    public void Valor_Livre_Resolvido_No_Catalogo_Retorna_O_Id_Correspondente()
    {
        var tipoId = Guid.NewGuid();
        var catalogo = new Dictionary<(string, string), Guid> { [("TipoFornecedor", "IND")] = tipoId };

        var resultado = FornecedorVinculoDominioResolver.Resolver("IND", null, null, null, null, null, catalogo);

        Assert.Equal(tipoId, resultado.TipoId);
        Assert.Empty(resultado.NaoResolvidos);
    }

    [Fact]
    public void Valor_Livre_Sem_Correspondencia_Nunca_Inventa_E_Vira_Nao_Resolvido()
    {
        var resultado = FornecedorVinculoDominioResolver.Resolver("TIPO_INEXISTENTE", null, null, null, null, null, new Dictionary<(string, string), Guid>());

        Assert.Null(resultado.TipoId);
        Assert.Single(resultado.NaoResolvidos);
        Assert.Contains("TipoFornecedor", resultado.NaoResolvidos[0]);
    }

    [Fact]
    public void Valor_Livre_Sem_Correspondencia_Nunca_Regride_Vinculo_Ja_Resolvido()
    {
        var idAtual = Guid.NewGuid();

        var resultado = FornecedorVinculoDominioResolver.Resolver("TIPO_MUDOU_E_SUMIU", null, null, idAtual, null, null, new Dictionary<(string, string), Guid>());

        Assert.Equal(idAtual, resultado.TipoId); // preserva, nunca zera
        Assert.Single(resultado.NaoResolvidos);
    }

    [Fact]
    public void Valor_Livre_Nulo_Preserva_Vinculo_Atual_Sem_Gerar_Ocorrencia()
    {
        var idAtual = Guid.NewGuid();

        var resultado = FornecedorVinculoDominioResolver.Resolver(null, null, null, idAtual, null, null, new Dictionary<(string, string), Guid>());

        Assert.Equal(idAtual, resultado.TipoId);
        Assert.Empty(resultado.NaoResolvidos);
    }

    [Fact]
    public void Subtipo_Usa_Chave_Composta_TipoSubtipo()
    {
        var subtipoId = Guid.NewGuid();
        var catalogo = new Dictionary<(string, string), Guid> { [("SubtipoFornecedor", "IND:ACESSORIOS")] = subtipoId };

        var resultado = FornecedorVinculoDominioResolver.Resolver(null, "ACESSORIOS", null, null, null, null, catalogo);
        // Sem TipoFornecedor livre informado, a chave composta não pode ser montada -> não resolve, nunca inventa.
        Assert.Null(resultado.SubtipoId);
    }

    [Fact]
    public void Mudou_Detecta_Qualquer_Dimensao_Diferente()
    {
        var resultado = new FornecedorVinculoDominioResultado(Guid.NewGuid(), null, null, []);

        Assert.True(resultado.Mudou(null, null, null));
        Assert.False(new FornecedorVinculoDominioResultado(null, null, null, []).Mudou(null, null, null));
    }
}
