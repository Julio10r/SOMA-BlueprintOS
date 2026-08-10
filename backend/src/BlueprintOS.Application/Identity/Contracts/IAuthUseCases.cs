using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface ISolicitarOtpUseCase
{
    Task<SolicitarOtpResultado> ExecuteAsync(string email, CancellationToken ct);
}

public interface IValidarOtpUseCase
{
    Task<ValidarOtpResultado> ExecuteAsync(string email, string codigo, CancellationToken ct);
}

public interface ILogoutUseCase
{
    Task ExecuteAsync(string sessionRawToken, CancellationToken ct);
}

public interface IObterIdentidadeAtualUseCase
{
    Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct);
}
