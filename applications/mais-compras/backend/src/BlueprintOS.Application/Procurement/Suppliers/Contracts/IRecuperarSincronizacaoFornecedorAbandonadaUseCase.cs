namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Recuperação administrativa GOVERNADA de uma <c>SincronizacaoFornecedor</c> comprovadamente
/// abandonada (Status "EmAndamento" sem nenhum processo real em curso — B3 Bloco 5A.9, GAP KALUNGA: uma
/// falha fora do tratamento por registro podia deixar o registro preso para sempre, bloqueando a guarda de
/// concorrência de qualquer nova execução real). Nunca um UPDATE manual direto no banco — exige permissão
/// administrativa (rota protegida por `Sistema.Gerenciar`, mesma classe das demais rotas de sincronização)
/// e justificativa obrigatória, e registra quem executou e quando.</summary>
public interface IRecuperarSincronizacaoFornecedorAbandonadaUseCase
{
    Task<RecuperarSincronizacaoFornecedorAbandonadaResumo> ExecuteAsync(
        RecuperarSincronizacaoFornecedorAbandonadaDto dto, CancellationToken cancellationToken = default);
}

public sealed record RecuperarSincronizacaoFornecedorAbandonadaDto(Guid ExecucaoId, string Justificativa);

public sealed record RecuperarSincronizacaoFornecedorAbandonadaResumo(
    Guid ExecucaoId, string StatusAnterior, string StatusFinal, DateTimeOffset RecuperadaEm, Guid UsuarioRecuperacaoId);
