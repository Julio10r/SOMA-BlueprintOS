using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Models;

/// <summary>Resposta deliberadamente idêntica para e-mail existente/inexistente/inativo — anti-enumeração
/// (security-design-auth-o1.4.md, §2.1/§3.1).</summary>
public sealed record SolicitarOtpResultado(bool DomainAutorizado);

public sealed record ValidarOtpResultado(
    bool Sucesso,
    string? MotivoGenerico,
    string? SessionRawToken,
    Guid? UsuarioId,
    string? Email,
    string? Nome,
    Guid? UnidadeNegocioId);

/// <summary><c>Permissoes</c> (O1.5) são as permissões efetivas resolvidas no backend a cada requisição,
/// a partir dos Perfis ativos vinculados ao usuário — nunca lidas de um token, cookie ou payload do
/// cliente. São revalidadas junto com a sessão em cada chamada, então inativar um Perfil ou desvincular
/// um usuário tem efeito imediato, sem esperar expiração de sessão.</summary>
public sealed record IdentidadeAtualDto(
    Guid UsuarioId,
    string Email,
    string Nome,
    Guid UnidadeNegocioId,
    IReadOnlyList<string> Permissoes,
    EscopoAdministrativo EscopoAdministrativo);
