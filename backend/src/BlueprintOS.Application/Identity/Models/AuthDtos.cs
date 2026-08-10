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

public sealed record IdentidadeAtualDto(Guid UsuarioId, string Email, string Nome, Guid UnidadeNegocioId);
