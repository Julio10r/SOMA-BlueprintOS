using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Casos de uso da Gestão de Unidades de Alocação (O1.8), seguindo o mesmo padrão de
/// <c>IUsuarioUseCases</c> (O1.6): <c>unidadeNegocioId</c> sempre resolvido pela API a partir da
/// identidade autenticada, nunca do corpo da requisição.</summary>
public interface IListarUnidadesAlocacaoUseCase
{
    Task<IReadOnlyList<UnidadeAlocacaoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IObterUnidadeAlocacaoUseCase
{
    Task<UnidadeAlocacaoDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarUnidadeAlocacaoUseCase
{
    Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(UnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarUnidadeAlocacaoUseCase
{
    Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(Guid id, UnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAlterarStatusUnidadeAlocacaoUseCase
{
    Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
