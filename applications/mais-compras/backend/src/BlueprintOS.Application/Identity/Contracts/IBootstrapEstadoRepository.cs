using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Contrato de leitura de <see cref="BootstrapEstado"/> (Work Order O1.4.3, seção 12). A escrita
/// (transição <c>Concluido = false → true</c>, via UPDATE condicional) é escopo de O1.4.3.2 — esta etapa
/// (O1.4.3.1) expõe apenas a leitura necessária para <c>GET /bootstrap/estado</c> e para a checagem de
/// "ainda disponível" antes de <c>POST /bootstrap/iniciar</c>/<c>POST /bootstrap/otp/verificar</c>.</summary>
public interface IBootstrapEstadoRepository
{
    /// <summary>Lê a linha fixa (<see cref="BootstrapEstado.IdFixo"/>) explicitamente. Retorna <c>null</c>
    /// se a linha estiver ausente (falha operacional — a seed migration deveria sempre criá-la); o chamador
    /// deve tratar a ausência como fail-closed (indisponível), nunca como "disponível por omissão" em
    /// runtime (Work Order O1.4.3, seção 12).</summary>
    Task<BootstrapEstado?> ObterAsync(CancellationToken ct);

    /// <summary>Marca a linha como rastreada para atualização (O1.4.3.2) — a escrita real só ocorre em
    /// <see cref="SalvarAlteracoesAsync"/>, junto com as demais entidades da transação de conclusão (Work
    /// Order O1.4.3, seção 13). Nunca chamado isoladamente fora desse fluxo.</summary>
    Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct);

    /// <summary>Persiste, em uma única chamada a <c>SaveChangesAsync</c>, todas as alterações rastreadas no
    /// mesmo <c>DbContext</c> compartilhado pelos demais repositórios da conclusão do Bootstrap (Usuario,
    /// UnidadeNegocio, Perfil, UsuarioPerfil, BootstrapEstado) — a transação implícita mais simples descrita
    /// na Work Order O1.4.3, seção 13. Lança <see cref="ConcurrencyConflictException"/> se
    /// <see cref="BootstrapEstado.RowVersion"/> tiver sido alterado por outra conclusão concorrente
    /// (compare-and-swap perdido).</summary>
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
