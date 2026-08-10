using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Rastreia a criação (O1.4.3.2) — a escrita real ocorre junto com as demais entidades da
    /// transação de conclusão do Bootstrap (Work Order O1.4.3, seção 13), via
    /// <see cref="IBootstrapEstadoRepository.SalvarAlteracoesAsync"/> no mesmo <c>DbContext</c> compartilhado.</summary>
    Task AdicionarAsync(Usuario usuario, CancellationToken ct);
}
