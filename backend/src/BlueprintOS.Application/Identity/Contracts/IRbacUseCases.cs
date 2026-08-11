using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Casos de uso da Gestão de Perfis (O1.5 — RBAC Real).
///
/// Todos recebem <c>unidadeNegocioId</c> explicitamente, resolvido pela camada de API a partir da
/// identidade autenticada (<see cref="ICurrentIdentity"/>) — nunca do corpo da requisição.
///
/// Os casos de uso de escrita recebem também <c>permissoesDoAtor</c>: as permissões efetivas de quem está
/// executando a operação, igualmente resolvidas no backend. Isto implementa a regra de não-escalonamento —
/// ninguém concede uma permissão que não possui.</summary>
public interface IListarPerfisUseCase
{
    Task<IReadOnlyList<PerfilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IObterPerfilUseCase
{
    Task<PerfilDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarPerfilUseCase
{
    Task<RbacResultado<PerfilDto>> ExecuteAsync(PerfilInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct);
}

public interface IAtualizarPerfilUseCase
{
    Task<RbacResultado<PerfilDto>> ExecuteAsync(Guid id, PerfilInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct);
}

public interface IAlterarStatusPerfilUseCase
{
    Task<RbacResultado<PerfilDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IListarCatalogoPermissoesUseCase
{
    Task<IReadOnlyList<PermissaoCatalogoDto>> ExecuteAsync(CancellationToken ct);
}
