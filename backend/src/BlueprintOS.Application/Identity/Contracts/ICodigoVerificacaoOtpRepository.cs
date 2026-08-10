using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface ICodigoVerificacaoOtpRepository
{
    Task<CodigoVerificacaoOtp?> ObterPendentePorUsuarioAsync(Guid usuarioId, CancellationToken ct);
    Task<CodigoVerificacaoOtp?> ObterMaisRecentePorUsuarioAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>Fluxo de Bootstrap (Work Order O1.4.3, seção 11) — candidato sem <see cref="BlueprintOS.Domain.Identity.Usuario"/>
    /// existente, identificado apenas pelo e-mail candidato normalizado.</summary>
    Task<CodigoVerificacaoOtp?> ObterPendentePorEmailCandidatoAsync(string emailCandidato, CancellationToken ct);

    Task AdicionarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct);
    Task AtualizarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct);
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
