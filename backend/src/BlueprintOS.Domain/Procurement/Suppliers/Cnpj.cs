namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>
/// Identificador fiscal legado para compatibilidade com construtores antigos de <see cref="Fornecedor"/>.
/// Delega normalização e validação a <see cref="DocumentoFiscal"/> (fonte única de verdade, ADR-0023) e
/// restringe o resultado a CNPJ (14 dígitos).
/// </summary>
public sealed record Cnpj
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string value)
    {
        var documento = DocumentoFiscal.Create(value);
        if (documento.Value.Length != 14)
        {
            throw new ArgumentException("CNPJ must contain 14 digits.", nameof(value));
        }

        return new Cnpj(documento.Value);
    }

    public override string ToString() => Value;
}
