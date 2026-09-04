namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed record FornecedorVinculoDominioResultado(Guid? TipoId, Guid? SubtipoId, Guid? CondicaoId, IReadOnlyList<string> NaoResolvidos)
{
    public bool Mudou(Guid? tipoAtual, Guid? subtipoAtual, Guid? condicaoAtual) =>
        TipoId != tipoAtual || SubtipoId != subtipoAtual || CondicaoId != condicaoAtual;
}

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): resolução PURA, sem I/O, dos valores livres já
/// sincronizados em <c>Fornecedor.TipoFornecedor/SubtipoFornecedor/CondicaoPagamento</c> para os
/// respectivos <c>FornecedorDominioErp.Id</c>. Decisão do PO: "não inventar vínculo quando o valor Linx não
/// puder ser resolvido" — um valor livre presente mas sem correspondência exata no catálogo NUNCA é
/// vinculado (fica registrado em <see cref="FornecedorVinculoDominioResultado.NaoResolvidos"/> para virar
/// ocorrência) e NUNCA regride um vínculo já resolvido em execução anterior — se o catálogo não tem match
/// agora (execução parcial, catálogo desatualizado), o Id atual é preservado, nunca zerado.
/// </summary>
public static class FornecedorVinculoDominioResolver
{
    public static FornecedorVinculoDominioResultado Resolver(
        string? tipoFornecedor, string? subtipoFornecedor, string? condicaoPagamento,
        Guid? tipoIdAtual, Guid? subtipoIdAtual, Guid? condicaoIdAtual,
        IReadOnlyDictionary<(string TipoDominio, string CodigoErp), Guid> catalogo)
    {
        var naoResolvidos = new List<string>();

        var tipoId = Resolver("TipoFornecedor", tipoFornecedor, tipoIdAtual, catalogo, naoResolvidos);
        var subtipoCodigo = string.IsNullOrWhiteSpace(subtipoFornecedor) || string.IsNullOrWhiteSpace(tipoFornecedor)
            ? subtipoFornecedor
            : $"{tipoFornecedor.Trim()}:{subtipoFornecedor.Trim()}";
        var subtipoId = Resolver("SubtipoFornecedor", subtipoCodigo, subtipoIdAtual, catalogo, naoResolvidos, rotuloOcorrencia: subtipoFornecedor);
        var condicaoId = Resolver("CondicaoPagamento", condicaoPagamento, condicaoIdAtual, catalogo, naoResolvidos);

        return new FornecedorVinculoDominioResultado(tipoId, subtipoId, condicaoId, naoResolvidos);
    }

    private static Guid? Resolver(
        string discriminador, string? valorLivre, Guid? valorAtual,
        IReadOnlyDictionary<(string TipoDominio, string CodigoErp), Guid> catalogo,
        List<string> naoResolvidos, string? rotuloOcorrencia = null)
    {
        if (string.IsNullOrWhiteSpace(valorLivre)) return valorAtual;

        if (catalogo.TryGetValue((discriminador, valorLivre.Trim()), out var id)) return id;

        naoResolvidos.Add($"{discriminador}:{rotuloOcorrencia ?? valorLivre}");
        return valorAtual;
    }
}
