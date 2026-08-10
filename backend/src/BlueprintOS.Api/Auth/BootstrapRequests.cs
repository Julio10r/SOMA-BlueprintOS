namespace BlueprintOS.Api.Auth;

public sealed record BootstrapIniciarRequest(string? Email, string? Secret);

public sealed record BootstrapOtpVerificarRequest(string? Email, string? Codigo);

public sealed record BootstrapConcluirUnidadeNegocioRequest(Guid? Id, string? Nome, string? Slug);

public sealed record BootstrapConcluirAdministradorRequest(string? Nome);

public sealed record BootstrapConcluirRequest(
    BootstrapConcluirUnidadeNegocioRequest? UnidadeNegocio,
    BootstrapConcluirAdministradorRequest? Administrador);
