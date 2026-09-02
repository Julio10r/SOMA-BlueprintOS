namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>
/// Documento fiscal canônico do domínio +Compras (CNPJ ou CPF). Normaliza e valida o dígito
/// verificador (módulo 11) na fronteira de entrada. Compatibilidade com códigos legados não
/// numéricos do Linx (CGC_CPF) pertence exclusivamente ao futuro Adapter Linx, nunca a este Value
/// Object (ADR-0023).
///
/// CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026): as
/// 12 primeiras posições podem ser letras A-Z ou dígitos; os 2 dígitos verificadores finais
/// continuam sempre numéricos. CPF nunca muda — permanece só numérico (11 dígitos). A normalização
/// preserva letras (antes usava <c>char.IsDigit</c>, que descartava qualquer letra do CNPJ). O
/// cálculo do dígito verificador (<see cref="CalcularDigito"/>) já usa <c>char - '0'</c>, que É a
/// mesma operação de "valor ASCII menos 48" pedida pela IN 2.229/2024 (dígitos 0-9 valem 0-9,
/// letras A-Z valem 17-42) — não precisou mudar, só a normalização que descartava as letras antes
/// de chegar até aqui.
/// </summary>
public sealed record DocumentoFiscal
{
    public string Value { get; }

    private DocumentoFiscal(string value) => Value = value;

    public static DocumentoFiscal Create(string value)
    {
        var normalized = new string((value ?? string.Empty).ToUpperInvariant()
            .Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')).ToArray());

        if (normalized.Length is not (11 or 14))
        {
            throw new ArgumentException("Documento fiscal deve conter 11 dígitos (CPF) ou 14 caracteres alfanuméricos (CNPJ).", nameof(value));
        }

        // CPF nunca muda com a IN RFB 2.229/2024 — continua sempre só numérico.
        if (normalized.Length == 11 && !normalized.All(char.IsDigit))
        {
            throw new ArgumentException("CPF deve conter apenas dígitos.", nameof(value));
        }

        // Os 2 dígitos verificadores do CNPJ continuam sempre numéricos, mesmo no formato
        // alfanumérico — só as 12 primeiras posições podem ter letras.
        if (normalized.Length == 14 && !normalized[12..].All(char.IsDigit))
        {
            throw new ArgumentException("Os dígitos verificadores do CNPJ devem ser numéricos.", nameof(value));
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
