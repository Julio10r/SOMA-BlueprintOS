using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IConsultarBootstrapEstadoUseCase
{
    Task<ConsultarBootstrapEstadoResultado> ExecuteAsync(CancellationToken ct);
}

public interface IIniciarBootstrapUseCase
{
    Task<IniciarBootstrapResultado> ExecuteAsync(string email, string secret, CancellationToken ct);
}

public interface IValidarOtpBootstrapUseCase
{
    Task<ValidarOtpBootstrapResultado> ExecuteAsync(string email, string codigo, CancellationToken ct);
}

/// <summary>Conclusão transacional do Bootstrap (O1.4.3.2; Work Order O1.4.3, seção 13). <paramref
/// name="bootstrapSessaoId"/> identifica a <c>BootstrapSessao</c> já autenticada pela política
/// <c>BootstrapAuthenticated</c> — o e-mail do Administrador Sênior vem exclusivamente dela, nunca do
/// payload (seção 13, passo 3).</summary>
public interface IConcluirBootstrapUseCase
{
    Task<ConcluirBootstrapResultado> ExecuteAsync(
        Guid bootstrapSessaoId,
        UnidadeNegocioBootstrapPayload unidadeNegocio,
        AdministradorSeniorBootstrapPayload administrador,
        CancellationToken ct);
}
