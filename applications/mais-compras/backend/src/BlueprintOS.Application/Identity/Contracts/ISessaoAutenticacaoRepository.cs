using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface ISessaoAutenticacaoRepository
{
    Task<SessaoAutenticacao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct);
    Task AdicionarAsync(SessaoAutenticacao sessao, CancellationToken ct);
    Task AtualizarAsync(SessaoAutenticacao sessao, CancellationToken ct);
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
