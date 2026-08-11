namespace BlueprintOS.Application.Identity.Models;

/// <summary>Identidade mínima necessária para associar uma chamada a seu solicitante.
///
/// O1.5 acrescentou <see cref="UnidadeNegocioId"/>: os casos de uso administrativos precisam escopar
/// leituras e escritas à Unidade de Negócio da identidade autenticada, jamais a um valor vindo do corpo
/// da requisição. É anulável porque o esquema de autenticação exclusivo de Development
/// (<c>X-Development-User-Id</c>) não carrega essa claim — nesse caso, os casos de uso administrativos
/// falham fechado em vez de assumir uma Unidade de Negócio.
///
/// <see cref="Permissoes"/> NÃO é a fonte de autorização — o enforcement acontece nas policies do
/// ASP.NET Core sobre <c>HttpContext.User</c>. Está aqui apenas para defesa em profundidade dentro dos
/// casos de uso, sempre com o mesmo conteúdo já resolvido no backend.</summary>
public sealed record RequestIdentity(
    Guid UserId,
    string Role,
    Guid? UnidadeNegocioId = null,
    IReadOnlyList<string>? Permissoes = null);
