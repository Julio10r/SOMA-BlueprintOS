using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Casos de uso da Gestão de Usuários (O1.6), seguindo o mesmo padrão de
/// <c>IRbacUseCases</c> (O1.5): <c>unidadeNegocioId</c> sempre resolvido pela API a partir da identidade
/// autenticada, nunca do corpo da requisição.</summary>
public interface IListarUsuariosUseCase
{
    Task<IReadOnlyList<UsuarioDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IObterUsuarioUseCase
{
    Task<UsuarioDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarUsuarioUseCase
{
    Task<RbacResultado<UsuarioDto>> ExecuteAsync(UsuarioInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct);
}

public interface IAtualizarUsuarioUseCase
{
    Task<RbacResultado<UsuarioDto>> ExecuteAsync(Guid id, UsuarioInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct);
}

public interface IAlterarStatusUsuarioUseCase
{
    Task<RbacResultado<UsuarioDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
