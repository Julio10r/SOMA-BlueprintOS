using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Motor único de montagem e publicação de documentos (Template Engine): dado um
/// <see cref="DocumentTemplate"/> (o que é fixo do documento), o conteúdo autoral carregado de
/// <c>.ai/content/{categoria}/</c>, as poucas <see cref="DocumentSection"/> dinâmicas que o
/// publisher ainda gera em código (roadmap automático), os diagramas já renderizados
/// (<see cref="DocumentDiagram"/>, nunca código Mermaid bruto) e o
/// <see cref="IDocumentationAssetsManager"/> (tema, selos, QR Code, apêndice), monta o
/// <see cref="PublicationDocument"/> completo — capa, resumo executivo, índice, cores, ativos e
/// apêndice inclusos — e o grava em disco em todos os formatos.
/// </summary>
/// <remarks>
/// Introduzido para eliminar a duplicação que existia entre <c>ExecutivePublisher</c>,
/// <c>ClientPublisher</c> e <c>EngineeringPublisher</c>: os três montavam, de forma quase
/// idêntica, seções a partir do conteúdo autoral, metadados, selos/QR Code e apêndice. Com este
/// assembler, um novo documento publicado passa a exigir apenas um <see cref="DocumentTemplate"/>
/// e a lista de seções/diagramas dinâmicos específicos — sem repetir a lógica de montagem, e sem
/// que nenhum publisher acesse assets/tema/CSS diretamente.
/// </remarks>
internal static class DocumentAssembler
{
    /// <summary>
    /// Monta o documento a partir do template, do conteúdo autoral (na ordem em que foi
    /// carregado), das seções dinâmicas e diagramas informados, e grava os artefatos em disco.
    /// Tema, selos/QR Code e apêndice são obtidos exclusivamente via
    /// <paramref name="assetsManager"/>. Insere automaticamente um Resumo Executivo logo após a
    /// capa (derivado do próprio conteúdo autoral) e move o roadmap completo para o apêndice,
    /// mantendo no corpo apenas uma timeline condensada.
    /// </summary>
    public static async Task<IReadOnlyList<PublishedArtifact>> AssembleAsync(
        DocumentTemplate template,
        IReadOnlyList<(string FileName, string Content)> contentFiles,
        IReadOnlyList<DocumentSection> dynamicSections,
        IReadOnlyList<DocumentDiagram> diagrams,
        IDocumentationAssetsManager assetsManager,
        QualityMetrics metrics,
        DateTimeOffset generatedAt,
        string projectVersion,
        string distRootPath,
        IReadOnlyList<IContentRenderer> renderers,
        CancellationToken cancellationToken = default)
    {
        var contentSections = new List<PublicationSection>(contentFiles.Count);
        foreach (var file in contentFiles)
        {
            var (heading, body) = ReportPublishingHelper.SplitHeading(file.Content);
            contentSections.Add(ReportPublishingHelper.BuildSection(heading, body));
        }

        var sections = new List<PublicationSection>(contentSections.Count + dynamicSections.Count + diagrams.Count + 1)
        {
            BuildExecutiveSummary(template, contentSections),
        };
        sections.AddRange(contentSections);

        var roadmapAppendix = new List<PublicationSection>();
        foreach (var dynamicSection in dynamicSections)
        {
            var markdown = await dynamicSection.BuildMarkdownAsync(cancellationToken);
            if (string.Equals(dynamicSection.Title, "Roadmap Automático", StringComparison.Ordinal))
            {
                // Fase 6: o corpo principal recebe apenas uma timeline condensada; o roadmap
                // técnico completo (o mesmo Markdown gerado) vai para o apêndice.
                sections.Add(ReportPublishingHelper.BuildSection("Linha do Tempo", BuildCondensedTimeline(markdown)));
                roadmapAppendix.Add(ReportPublishingHelper.BuildSection("Roadmap Completo", markdown));
                continue;
            }

            sections.Add(ReportPublishingHelper.BuildSection(dynamicSection.Title, markdown));
        }

        foreach (var diagram in diagrams)
        {
            sections.Add(diagram.Section);
        }

        var metadata = PublicationMetadata.Create(
            title: template.Title,
            subtitle: template.Subtitle,
            audience: template.Audience,
            version: projectVersion,
            generatedAt: generatedAt,
            tags: template.Tags);

        var assets = assetsManager.BuildStandardAssets(metrics);
        if (diagrams.Count > 0)
        {
            assets = assets with { Mermaid = diagrams.Select(d => d.Asset).ToArray() };
        }

        assets = assets with { Badges = BuildIndicatorBadges(assets.Badges, metrics, projectVersion, sections.Count) };

        var appendix = new List<PublicationSection>(roadmapAppendix);
        appendix.AddRange(assetsManager.BuildStandardAppendix(metadata));

        var document = new PublicationDocument(
            Slug: template.Slug,
            Category: template.Category,
            Metadata: metadata,
            Sections: sections,
            Assets: assets,
            Appendix: appendix,
            Theme: assetsManager.GetTheme(template.DocumentClass));

        return await ReportPublishingHelper.WriteAllFormatsAsync(
            document, template.Category, distRootPath, renderers, cancellationToken);
    }

    /// <summary>
    /// Fase 4: monta uma página de Resumo Executivo logo após a capa — Visão Geral, Objetivo,
    /// Público-alvo, Principais Benefícios, Estado Atual e Próximos Passos —, reaproveitando o
    /// próprio conteúdo autoral já carregado (primeiro parágrafo de cada seção correspondente),
    /// sem inventar nenhuma informação de negócio.
    /// </summary>
    private static PublicationSection BuildExecutiveSummary(DocumentTemplate template, IReadOnlyList<PublicationSection> contentSections)
    {
        // Rótulos em negrito inline (não ContentBlock.Heading) para não duplicar, dentro do
        // documento, os títulos das seções reais de conteúdo autoral que seguem esta página.
        var blocks = new List<ContentBlock>
        {
            ContentBlock.Paragraph($"**Visão Geral:** {template.Subtitle}"),
            ContentBlock.Paragraph($"**Público-alvo:** {template.Audience}"),
        };

        AppendFirstParagraphIfFound(blocks, contentSections, "Principais benefícios", "benefíc");
        AppendFirstParagraphIfFound(blocks, contentSections, "Estado atual", "estado atual", "estado", "atual");
        AppendFirstParagraphIfFound(blocks, contentSections, "Próximos passos", "próximos passos", "próximo");

        return new PublicationSection("Resumo Executivo", blocks);
    }

    private static void AppendFirstParagraphIfFound(
        List<ContentBlock> blocks, IReadOnlyList<PublicationSection> contentSections, string label, params string[] keywords)
    {
        var match = contentSections.FirstOrDefault(section =>
            keywords.Any(keyword => section.Heading.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

        var paragraph = match?.Blocks.FirstOrDefault(b => b.Kind == ContentBlockKind.Paragraph)?.Text;
        if (string.IsNullOrWhiteSpace(paragraph))
        {
            return;
        }

        blocks.Add(ContentBlock.Paragraph($"**{label}:** {paragraph}"));
    }

    /// <summary>
    /// Fase 6: extrai apenas os títulos de fase (linhas <c>### Fase ...</c>) do Markdown completo
    /// do roadmap, para uma timeline condensada no corpo principal.
    /// </summary>
    private static string BuildCondensedTimeline(string fullRoadmapMarkdown)
    {
        var phases = fullRoadmapMarkdown
            .Replace("\r\n", "\n")
            .Split('\n')
            .Where(line => line.TrimStart().StartsWith("### ", StringComparison.Ordinal))
            .Select(line => line.TrimStart('#', ' ').Trim())
            .ToList();

        if (phases.Count == 0)
        {
            return "Roadmap ainda não estruturado em fases. Ver detalhe completo no apêndice.";
        }

        return "Linha do tempo (detalhe completo no apêndice):\n\n"
            + string.Join('\n', phases.Select(phase => $"- {phase}"));
    }

    /// <summary>Fase 5: indicadores como selos — Build, Testes, Versão, Documentos e Status.</summary>
    private static IReadOnlyList<BadgeAsset> BuildIndicatorBadges(
        IReadOnlyList<BadgeAsset> standardBadges, QualityMetrics metrics, string projectVersion, int sectionCount)
    {
        var badges = new List<BadgeAsset>(standardBadges)
        {
            new("badge-version", "Versão", projectVersion, BadgeStatus.Neutral),
            new("badge-sections", "Documentos", sectionCount.ToString(), BadgeStatus.Neutral),
            new(
                "badge-status",
                "Status",
                metrics.BuildSucceeded ? "Operacional" : "Instável",
                metrics.BuildSucceeded ? BadgeStatus.Success : BadgeStatus.Warning),
        };

        return badges;
    }
}
