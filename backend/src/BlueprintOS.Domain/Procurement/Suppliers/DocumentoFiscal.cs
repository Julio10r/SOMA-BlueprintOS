namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>
/// Documento fiscal canônico do domínio +Compras (CNPJ ou CPF). Normaliza para dígitos puros e
/// valida o dígito verificador (módulo 11) na fronteira de entrada. Compatibilidade com códigos
/// legados não numéricos do Linx (CGC_CPF) pertence exclusivamente ao futuro Adapter Linx, nunca
/// a este Value Object (ADR-0023).
/// </summary>
public sealed record DocumentoFiscal
{
    public string Value { get; }

    private DocumentoFiscal(string value) => Value = value;

    public static DocumentoFiscal Create(string value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        if (normalized.Length is not (11 or 14))
        {
            throw new ArgumentException("Documento fiscal deve conter 11 dígitos (CPF) ou 14 dígitos (CNPJ).", nameof(value));
        }

        if (TodosDigitosIguais(normalized))
        {
            throw new ArgumentException("Documento fiscal com sequência de dígitos repetidos é inválido.", nameof(value));
        }

        if (!DigitoVerificadorValido(normalized))
        {
            throw new ArgumentException("Documento fiscal com dígito verificador inválido.", nameof(value));
        }

        return new DocumentoFiscal(normalized);
    }

    public string Formatado() => Value.Length switch
    {
        14 => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}",
        11 => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}",
        _ => Value,
    };

    public override string ToString() => Value;

    private static bool TodosDigitosIguais(string digits) => digits.Distinct().Count() == 1;

    private static bool DigitoVerificadorValido(string digits) => digits.Length switch
    {
        14 => digits == CalcularDigitosCnpj(digits[..12]),
        11 => digits == CalcularDigitosCpf(digits[..9]),
        _ => false,
    };

    private static string CalcularDigitosCnpj(string base12)
    {
        var digito1 = CalcularDigito(base12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var base13 = base12 + digito1;
        var digito2 = CalcularDigito(base13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return base13 + digito2;
    }

    private static string CalcularDigitosCpf(string base9)
    {
        var digito1 = CalcularDigito(base9, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var base10 = base9 + digito1;
        var digito2 = CalcularDigito(base10, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return base10 + digito2;
    }

    private static char CalcularDigito(string baseDigits, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < baseDigits.Length; i++)
        {
            soma += (baseDigits[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }
}
