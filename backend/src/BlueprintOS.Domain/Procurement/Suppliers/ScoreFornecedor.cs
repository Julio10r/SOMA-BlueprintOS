namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Regra centralizada do score inicial de descoberta de fornecedores.</summary>
public static class ScoreFornecedor
{
    public const decimal ItemExato = 100;
    public const decimal Familia = 80;
    public const decimal Categoria = 60;
    public const decimal Historico = 40;

    public static decimal Calcular(bool itemExato, bool familia, bool categoria, bool historico) =>
        itemExato ? ItemExato : familia ? Familia : categoria ? Categoria : historico ? Historico : 0;

    public static string DeterminarCriterio(bool itemExato, bool familia, bool categoria, bool historico) =>
        itemExato ? "ItemExato" : familia ? "Familia" : categoria ? "Categoria" : historico ? "Historico" : "SemCorrespondencia";
}
