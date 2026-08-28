using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IBootstrapSessaoRepository
{
    Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct);

    /// <summary>Usado por <c>ConcluirBootstrapUseCase</c> (O1.4.3.2) para reobter a sessão já autenticada pela
    /// política <c>BootstrapAuthenticated</c> a partir da claim <c>bootstrap_session_id</c> — nunca se
    /// confia no e-mail do payload da requisição (Work Order O1.4.3, seção 13, passo 3).</summary>
    Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Fluxo de invalidação em cascata (Work Order O1.4.3, seção 8): uma nova tentativa de
    /// <c>POST /bootstrap/iniciar</c> sempre invalida qualquer <see cref="BootstrapSessao"/> anterior do
    /// mesmo e-mail candidato ainda não usada/revogada.</summary>
    Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct);

    Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct);
    Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct);
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
