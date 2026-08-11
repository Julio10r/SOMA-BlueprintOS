using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência dos metadados locais +Compras de Filial (O1.7). Toda leitura/escrita é escopada
/// por Unidade de Negócio — mesmo cuidado de <see cref="IUsuarioRepository"/>/<c>IPerfilRepository</c>.</summary>
public interface IFilialMetadadoRepository
{
    Task<IReadOnlyDictionary<string, FilialMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<FilialMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(FilialMetadado metadado, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
