namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Identificador fiscal brasileiro normalizado para persistência e comparação.</summary>
public sealed record Cnpj
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalized.Length != 14)
        {
            throw new ArgumentException("CNPJ must contain 14 digits.", nameof(value));
        }

        return new Cnpj(normalized);
    }

    public override string ToString() => Value;
}
