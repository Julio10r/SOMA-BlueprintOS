namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>B3 — Bloco 5A.9: DTO de leitura de um vínculo Linx no frontend (cadastro/detalhe de
/// Fornecedor). "Mais recente" (<see cref="DataParaTransferencia"/>) e "Principal" são conceitos
/// independentes — a UI nunca deve tratá-los como sinônimos.</summary>
public sealed record FornecedorLinxVinculoDto(
    Guid Id, string ErpSistema, string CodigoErp, string NomeClifor, bool Ativo, bool Principal, DateTimeOffset? DataParaTransferencia);

public interface IListarFornecedorLinxVinculosUseCase
{
    Task<IReadOnlyList<FornecedorLinxVinculoDto>?> ExecuteAsync(Guid fornecedorId, CancellationToken cancellationToken = default);
}

/// <summary>Troca explícita de Principal pelo comprador (Bloco 5A.9, §5/§15) — nunca automática por
/// recência. Rejeita vínculo inativo (§3: "não pode ser Principal") e vínculo de outro Fornecedor.</summary>
public interface IDefinirFornecedorLinxVinculoPrincipalUseCase
{
    Task<bool> ExecuteAsync(Guid fornecedorId, Guid vinculoId, CancellationToken cancellationToken = default);
}
