using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IOtpRequestThrottleRepository
{
    Task<OtpRequestThrottle?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct);
    Task AdicionarAsync(OtpRequestThrottle throttle, CancellationToken ct);
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
