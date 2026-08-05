namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Documento fiscal compatível com Linx CGC_CPF; formato final é validado nas bordas.</summary>
public sealed record DocumentoFiscal
{
    public string Value { get; }

    private DocumentoFiscal(string value) => Value = value;

    public static DocumentoFiscal Create(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is 0 or > 14) throw new ArgumentException("Documento fiscal must contain 1 to 14 characters.", nameof(value));
        return new DocumentoFiscal(normalized);
    }

    public override string ToString() => Value;
}
