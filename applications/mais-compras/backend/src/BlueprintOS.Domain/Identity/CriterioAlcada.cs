namespace BlueprintOS.Domain.Identity;

/// <summary>Critério que dispara uma <see cref="AlcadaAprovacao"/> (O1.12). Catálogo definitivo de
/// critérios ainda é dúvida de produto (`ComprasDataModel.md`) — este é o conjunto mínimo suficiente para
/// a fundação de cadastro, sem motor de avaliação.</summary>
public enum CriterioAlcada
{
    Valor = 0,
    Categoria = 1,
    CentroCusto = 2,
}
