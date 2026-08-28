using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.UnitTests.Infrastructure.Publication;

/// <summary>
/// Monta um <see cref="PublicationDocument"/> mínimo para os testes de renderizadores, com
/// metadados/tema padrão, para evitar repetir a estrutura completa em cada arquivo de teste.
/// </summary>
internal static class PublicationDocumentTestBuilder
{
    private static readonly DocumentPalette TestPalette = new(
        "#111111", "#222222", "#333333", "#444444", "#555555", "#666666", "#777777", "#888888", "#999999", "#aaaaaa");

    private static readonly DocumentTypography TestTypography = new("Display Font", "Body Font", "Mono Font");

    public static PublicationDocument Create(
        string slug,
        string title,
        string subtitle,
        string category,
        IReadOnlyList<PublicationSection> sections,
        PublicationAssets? assets = null,
        IReadOnlyList<PublicationSection>? appendix = null) => new(
        Slug: slug,
        Category: category,
        Metadata: PublicationMetadata.Create(
            title: title,
            subtitle: subtitle,
            audience: "Testes",
            version: "1.0.0",
            generatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        Sections: sections,
        Assets: assets ?? PublicationAssets.Empty,
        Appendix: appendix ?? Array.Empty<PublicationSection>(),
        Theme: new PublicationTheme(PublicationDocumentClass.Executive, TestPalette, TestTypography, "/* css de teste */"));
}
