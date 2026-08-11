namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de Filial (O1.7). <c>CodigoCliFor</c>/<c>NomeCliFor</c> vêm do ERP
/// (colunas exigidas pela ADR-0020 item 2); <c>DescricaoMaisCompras</c>/<c>AtivoNoMaisCompras</c> são os
/// metadados locais do +Compras. Quando não existe metadado local ainda para o código ERP retornado, o
/// registro é considerado Ativo por padrão (comportamento definido nesta sprint — ver relatório da O1.7)
/// e <c>TemMetadadoLocal</c> vem <c>false</c>.</summary>
public sealed record FilialDto(
    string CodigoCliFor,
    string NomeCliFor,
    string? UnidadeNegocioErpId,
    string? DescricaoMaisCompras,
    bool AtivoNoMaisCompras,
    bool TemMetadadoLocal,
    DateTimeOffset? AtualizadoEm);

public sealed record FilialMetadadoInput(string? DescricaoMaisCompras, bool AtivoNoMaisCompras);

/// <summary>Projeção de leitura de Centro de Custo (O1.7). <c>CodigoErp</c>/<c>DescricaoErp</c> vêm do ERP;
/// <c>DescricaoMaisCompras</c>/<c>AtivoNoMaisCompras</c> são os metadados locais do +Compras. Mesma regra de
/// "ativo por padrão sem metadado local" de <see cref="FilialDto"/>.</summary>
public sealed record CentroCustoDto(
    string CodigoErp,
    string DescricaoErp,
    string? DescricaoMaisCompras,
    bool AtivoNoMaisCompras,
    bool TemMetadadoLocal,
    DateTimeOffset? AtualizadoEm);

public sealed record CentroCustoMetadadoInput(string? DescricaoMaisCompras, bool AtivoNoMaisCompras);

public enum ErpMetadadoFalha
{
    Nenhuma = 0,
    CodigoErpNaoEncontrado,
    AncoradoPorOutraUnidadeDeNegocio,
}

public sealed record ErpMetadadoResultado<T>(bool Sucesso, ErpMetadadoFalha Falha, string? Mensagem, T? Valor)
{
    public static ErpMetadadoResultado<T> Ok(T valor) => new(true, ErpMetadadoFalha.Nenhuma, null, valor);
    public static ErpMetadadoResultado<T> Erro(ErpMetadadoFalha falha, string mensagem) => new(false, falha, mensagem, default);
}
