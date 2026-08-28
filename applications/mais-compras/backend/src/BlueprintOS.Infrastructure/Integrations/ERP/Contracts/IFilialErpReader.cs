namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Filiais do ERP `SOMA_DESENV` (O1.7, D3/ADR-0021). Mesmo
/// padrão de <c>IFornecedorErpReader</c> (B2.1/B2.1.2): o ERP é fonte canônica e imutável, o +Compras nunca
/// cria/edita/exclui o dado mestre — apenas lê para exibir e para correlacionar com metadados locais.</summary>
public interface IFilialErpReader
{
    Task<IReadOnlyList<FilialErpDto>> BuscarFiliaisAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Usado pela validação de vínculo de Centro de Custo/Filial: confirma que um código ERP
    /// específico existe de fato no ERP antes de qualquer persistência local referenciá-lo.</summary>
    Task<FilialErpDto?> BuscarPorCodigoAsync(string codigoCliFor, CancellationToken cancellationToken = default);
}

/// <summary>Filial como lida do ERP. <c>CodigoCliFor</c>/<c>NomeCliFor</c> são a referência de negócio da
/// integração (chave de correlação com o cadastro de Cliente/Fornecedor do SOMA_DESENV usado como origem de
/// Filial) — nunca alterados/normalizados pelo +Compras.</summary>
public sealed record FilialErpDto(
    string CodigoCliFor,
    string NomeCliFor,
    string? UnidadeNegocioErpId,
    DateTimeOffset? UltimaAlteracaoEm);
