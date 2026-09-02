using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Domain.Procurement.Suppliers;

public sealed class DocumentoFiscalTests
{
    [Fact]
    public void Create_Should_Accept_Valid_Cnpj_Without_Mask()
    {
        var documento = DocumentoFiscal.Create("15436940000103");
        Assert.Equal("15436940000103", documento.Value);
    }

    [Fact]
    public void Create_Should_Accept_Valid_Cnpj_With_Mask()
    {
        var documento = DocumentoFiscal.Create("15.436.940/0001-03");
        Assert.Equal("15436940000103", documento.Value);
    }

    [Fact]
    public void Masked_And_Unmasked_Cnpj_Should_Produce_The_Same_Normalized_Value()
    {
        var comMascara = DocumentoFiscal.Create("15.436.940/0001-03");
        var semMascara = DocumentoFiscal.Create("15436940000103");
        Assert.Equal(semMascara.Value, comMascara.Value);
        Assert.Equal(semMascara, comMascara);
    }

    [Fact]
    public void Create_Should_Accept_Valid_Cpf_With_Or_Without_Mask()
    {
        var comMascara = DocumentoFiscal.Create("123.456.789-09");
        var semMascara = DocumentoFiscal.Create("12345678909");
        Assert.Equal("12345678909", comMascara.Value);
        Assert.Equal(comMascara, semMascara);
    }

    [Fact]
    public void Create_Should_Reject_Cnpj_With_Invalid_Check_Digit()
    {
        var exception = Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create("15436940000104"));
        Assert.Contains("dígito verificador", exception.Message);
    }

    [Fact]
    public void Create_Should_Reject_Cpf_With_Invalid_Check_Digit()
    {
        Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create("12345678901"));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("1234567890123")]
    [InlineData("123456789012345")]
    [InlineData("")]
    public void Create_Should_Reject_Invalid_Length(string documento)
    {
        Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create(documento));
    }

    [Fact]
    public void Create_Should_Strip_Punctuation_But_Keep_Letters()
    {
        // Gate de homologação (2026-09-01): CNPJ alfanumérico (Instrução Normativa RFB nº
        // 2.229/2024, vigente a partir de julho/2026) — letras nas 12 primeiras posições fazem
        // parte do documento, não são mais descartadas como ruído. Só pontuação/máscara é
        // removida. Exemplo oficial da Receita Federal (base SERPRO).
        var documento = DocumentoFiscal.Create("12.ABC.345/01DE-35");
        Assert.Equal("12ABC34501DE35", documento.Value);
    }

    [Fact]
    public void Create_Should_Reject_Cpf_With_Letters()
    {
        // CPF nunca muda com a IN RFB 2.229/2024 — continua sempre só numérico.
        Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create("1234567A901"));
    }

    [Fact]
    public void Create_Should_Reject_Cnpj_With_Letter_In_Check_Digits()
    {
        // Os 2 dígitos verificadores do CNPJ continuam sempre numéricos, mesmo no formato
        // alfanumérico — só as 12 primeiras posições podem ter letras.
        Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create("12ABC34501DE3A"));
    }

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    public void Create_Should_Reject_Repeated_Digit_Sequences(string documento)
    {
        Assert.Throws<ArgumentException>(() => DocumentoFiscal.Create(documento));
    }

    [Fact]
    public void Formatado_Should_Apply_Cnpj_Mask_Only_For_Presentation()
    {
        var documento = DocumentoFiscal.Create("15436940000103");
        Assert.Equal("15.436.940/0001-03", documento.Formatado());
        Assert.Equal("15436940000103", documento.Value);
    }

    [Fact]
    public void Formatado_Should_Apply_Cpf_Mask_Only_For_Presentation()
    {
        var documento = DocumentoFiscal.Create("12345678909");
        Assert.Equal("123.456.789-09", documento.Formatado());
        Assert.Equal("12345678909", documento.Value);
    }
}

public sealed class CnpjTests
{
    [Fact]
    public void Create_Should_Delegate_Normalization_To_DocumentoFiscal()
    {
        var cnpj = Cnpj.Create("15.436.940/0001-03");
        Assert.Equal("15436940000103", cnpj.Value);
    }

    [Fact]
    public void Create_Should_Reject_Invalid_Check_Digit()
    {
        Assert.Throws<ArgumentException>(() => Cnpj.Create("15436940000104"));
    }

    [Fact]
    public void Create_Should_Reject_Cpf_Length()
    {
        Assert.Throws<ArgumentException>(() => Cnpj.Create("12345678909"));
    }
}
