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
    DateTimeOffset? AtualizadoEm,
    string? UnidadeAlocacaoPadraoNome,
    int QuantidadeUnidadesAlocacaoVinculadas,
    /// <summary>Id interno do <see cref="Domain.Identity.CentroCustoMetadado"/> (O1.7), exposto a partir da
    /// O1.12 para permitir que outras estruturas (Alçadas, Regras Orçamentárias) referenciem o Centro de
    /// Custo por FK real. <c>null</c> quando <see cref="TemMetadadoLocal"/> é <c>false</c> — nesse caso o
    /// Centro de Custo ainda não pode ser referenciado por essas estruturas até que algum metadado local
    /// seja criado (ex.: pela primeira edição em Gestão de Centros de Custo).</summary>
    Guid? CentroCustoMetadadoId = null);

public sealed record CentroCustoMetadadoInput(string? DescricaoMaisCompras, bool AtivoNoMaisCompras);

/// <summary>Um vínculo de Unidade de Alocação com um Centro de Custo, na projeção de leitura do
/// relacionamento N:N (O1.9).</summary>
public sealed record UnidadeAlocacaoVinculoDto(Guid Id, string Nome, bool Ativo, bool Padrao);

/// <summary>Substitui integralmente o conjunto de Unidades de Alocação vinculadas a um Centro de Custo.
/// <c>PadraoId</c>, quando informado, deve estar entre <c>UnidadeAlocacaoIds</c> — nunca um Id fora do
/// vínculo que está sendo definido na mesma requisição.</summary>
public sealed record SubstituirVinculosUnidadeAlocacaoInput(IReadOnlyList<Guid> UnidadeAlocacaoIds, Guid? PadraoId);

public enum ErpMetadadoFalha
{
    Nenhuma = 0,
    CodigoErpNaoEncontrado,
    AncoradoPorOutraUnidadeDeNegocio,

    /// <summary>O1.9 — um Id de Unidade de Alocação informado no vínculo não existe, ou existe em outra
    /// Unidade de Negócio (nunca revelado como distinção, sempre tratado como inválido).</summary>
    UnidadeAlocacaoInvalida,

    /// <summary>O1.9 — <c>PadraoId</c> foi informado, mas não está entre os Ids vinculados na mesma
    /// requisição.</summary>
    PadraoForaDoVinculo,
}

public sealed record ErpMetadadoResultado<T>(bool Sucesso, ErpMetadadoFalha Falha, string? Mensagem, T? Valor)
{
    public static ErpMetadadoResultado<T> Ok(T valor) => new(true, ErpMetadadoFalha.Nenhuma, null, valor);
    public static ErpMetadadoResultado<T> Erro(ErpMetadadoFalha falha, string mensagem) => new(false, falha, mensagem, default);
}
