using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Item 21 do plano de testes da Work Order O1.4.3 (seção 18) — "Allowlist vazia" — nunca
/// interpretada como "sem restrição" (seção 10 da Work Order).</summary>
public sealed class BootstrapAllowedCandidatesOptionsTests
{
    [Fact]
    public void Autoriza_Should_Return_False_For_Any_Email_When_List_Is_Empty()
    {
        var options = new BootstrapAllowedCandidatesOptions { Emails = Array.Empty<string>() };

        Assert.False(options.Autoriza("qualquer@somagrupo.com.br"));
        Assert.False(options.Autoriza(""));
    }

    [Fact]
    public void Autoriza_Should_Return_False_When_List_Is_Default_Uninitialized()
    {
        var options = new BootstrapAllowedCandidatesOptions();

        Assert.False(options.Autoriza("qualquer@somagrupo.com.br"));
    }

    [Fact]
    public void Autoriza_Should_Match_Normalized_Email_Case_Insensitively_And_Trimmed()
    {
        var options = new BootstrapAllowedCandidatesOptions { Emails = new[] { "  Admin.Inicial@Example.Invalid  " } };

        Assert.True(options.Autoriza("admin.inicial@example.invalid"));
    }

    [Fact]
    public void Autoriza_Should_Reject_Email_Not_In_List()
    {
        var options = new BootstrapAllowedCandidatesOptions { Emails = new[] { "admin.inicial@example.invalid" } };

        Assert.False(options.Autoriza("outro@example.invalid"));
    }

    [Fact]
    public void ObterEmailsNormalizados_Should_Ignore_Blank_Entries()
    {
        var options = new BootstrapAllowedCandidatesOptions { Emails = new[] { "", "   ", "admin.inicial@example.invalid" } };

        var normalizados = options.ObterEmailsNormalizados();

        Assert.Single(normalizados);
    }
}
