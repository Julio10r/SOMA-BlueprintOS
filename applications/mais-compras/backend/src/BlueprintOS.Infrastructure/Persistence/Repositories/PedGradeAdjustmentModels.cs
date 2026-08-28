#pragma warning disable CS1591

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// What the caller must state to adjust the grade quantities (positions 1-6, sizes 34..44) of one
/// COMPRAS_PRODUTO row identified by (PEDIDO, PRODUTO, COR_PRODUTO). Grade position 7+ (size 32 and beyond)
/// is out of scope for this mechanism and never appears here or in the adapter that services it.
/// </summary>
public sealed record PedGradeAdjustmentRequest(
    string Pedido,
    string Produto,
    string CorProduto,
    int Tam1,
    int Tam2,
    int Tam3,
    int Tam4,
    int Tam5,
    int Tam6)
{
    public int Total => Tam1 + Tam2 + Tam3 + Tam4 + Tam5 + Tam6;
}
