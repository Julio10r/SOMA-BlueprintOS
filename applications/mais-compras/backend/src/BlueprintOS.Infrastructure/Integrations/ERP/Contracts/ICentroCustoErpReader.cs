namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Centros de Custo do ERP `SOMA_DESENV` (O1.7, D3/ADR-0021).
/// Mesmo padrão de <c>IFornecedorErpReader</c> (B2.1/B2.1.2): o ERP é fonte canônica e imutável, o +Compras
/// nunca cria/edita/exclui o dado mestre — apenas lê para exibir e para correlacionar com metadados
/// locais.</summary>
public interface ICentroCustoErpReader
{
    Task<IReadOnlyList<CentroCustoErpDto>> BuscarCentrosCustoAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Usado pela validação de vínculo Usuário×Centro de Custo (resolução da dívida O1.6-L2):
    /// confirma que o código ERP informado existe de fato no ERP antes de qualquer persistência local
    /// referenciá-lo — nenhum código arbitrário do cliente é aceito.</summary>
    Task<CentroCustoErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default);
}

public sealed record CentroCustoErpDto(
    string CodigoErp,
    string DescricaoErp,
    DateTimeOffset? UltimaAlteracaoEm);
