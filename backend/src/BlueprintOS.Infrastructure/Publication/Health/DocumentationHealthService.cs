using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Health;
using BlueprintOS.Infrastructure.Publication.Rendering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Health;

/// <summary>
/// Implementação de <see cref="IDocumentationHealthService"/>: lê os artefatos Markdown já
/// gerados pelo Publication Engine (sem reprocessar <c>PublicationDocument</c> nem tocar em
/// nenhum <c>IReportPublisher</c>) e verifica cobertura, estrutura, links e conteúdo.
/// </summary>
public sealed class DocumentationHealthService : IDocumentationHealthService
{
    private static readonly Regex HeadingPattern = new(@"^(#{1,6})\s+(.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex ImagePattern = new(@"!\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex LinkPattern = new(@"(?<!!)\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownNoisePattern = new(
        @"^#{1,6}\s+|!\[[^\]]*\]\([^)]*\)|\[[^\]]*\]\([^)]*\)|<a[^>]*>|</a>|[`*_>#|-]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly DocumentationHealthOptions _options;

    public DocumentationHealthService(IOptions<DocumentationHealthOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<DocumentationHealthReport> AnalyzeAsync(
        IReadOnlyList<PublishedArtifact> artifacts,
        CancellationToken cancellationToken = default)
    {
        var documents = artifacts
            .Where(a => a.Format == PublicationFormat.Markdown)
            .Select(a => new AnalyzedDocument(a, ReadContent(a.FilePath)))
            .ToList();

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AnalyzeCoverageAndContent(document);
            AnalyzeStructure(document);
            AnalyzeLinks(document);
        }

        AnalyzeCrossDocument(documents);

        var results = documents
            .Select(d => new DocumentHealthResult(
                d.Artifact.RelativePath,
                d.Errors.Count > 0 ? DocumentHealthStatus.Error : d.Warnings.Count > 0 ? DocumentHealthStatus.Warning : DocumentHealthStatus.Healthy,
                d.WordCount,
                d.Errors,
                d.Warnings))
            .ToList();

        var recommendations = BuildRecommendations(documents);

        return Task.FromResult(new DocumentationHealthReport(results, recommendations));
    }

    /// <inheritdoc />
    public async Task<string> WriteReportAsync(
        DocumentationHealthReport report,
        CancellationToken cancellationToken = default)
    {
        var outputPath = Path.GetFullPath(_options.OutputPath);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = Render(report);
        await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);

        return outputPath;
    }

    private static string ReadContent(string filePath) =>
        File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

    private void AnalyzeCoverageAndContent(AnalyzedDocument document)
    {
        var plainText = MarkdownNoisePattern.Replace(document.Content, " ");
        var words = plainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        document.WordCount = words.Length;
        document.NormalizedWords = new HashSet<string>(words.Select(w => w.ToLowerInvariant()));

        if (document.WordCount == 0)
        {
            document.Errors.Add("Documento vazio (sem conteúdo além da marcação Markdown).");
        }
        else if (document.WordCount < _options.MinWordCount)
        {
            document.Warnings.Add(
                $"Conteúdo abaixo do limite mínimo: {document.WordCount} palavra(s) (mínimo: {_options.MinWordCount}).");
        }
    }

    private static void AnalyzeStructure(AnalyzedDocument document)
    {
        var lines = document.Content.Replace("\r\n", "\n").Split('\n');
        var headings = new List<(int Level, string Text)>();
        foreach (var line in lines)
        {
            var match = HeadingPattern.Match(line);
            if (match.Success)
            {
                headings.Add((match.Groups[1].Value.Length, match.Groups[2].Value.Trim()));
            }
        }

        document.Headings = headings;

        var title = headings.FirstOrDefault(h => h.Level == 1);
        if (title.Text is null)
        {
            document.Errors.Add("Documento não possui título principal (heading nível 1).");
        }
        else
        {
            document.Title = title.Text;
        }

        if (!headings.Any(h => h.Level >= 2))
        {
            document.Errors.Add("Documento não possui nenhuma seção (heading nível 2 ou superior).");
        }

        int? previousLevel = null;
        foreach (var heading in headings)
        {
            if (previousLevel is not null && heading.Level - previousLevel > 1)
            {
                document.Warnings.Add(
                    $"Ordem de heading inválida: \"{heading.Text}\" (nível {heading.Level}) segue nível {previousLevel} sem heading intermediário.");
            }

            previousLevel = heading.Level;
        }

        foreach (var group in headings.GroupBy(h => h.Text, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            document.Warnings.Add($"Heading duplicado dentro do documento: \"{group.Key}\" ({group.Count()}x).");
        }
    }

    private static void AnalyzeLinks(AnalyzedDocument document)
    {
        var directory = Path.GetDirectoryName(document.Artifact.FilePath) ?? ".";
        var slugs = new HashSet<string>(
            document.Headings.Select(h => MarkdownRenderer.Slugify(h.Text)),
            StringComparer.OrdinalIgnoreCase);

        foreach (Match match in LinkPattern.Matches(document.Content))
        {
            var target = match.Groups[1].Value.Trim();
            if (IsExternal(target))
            {
                continue;
            }

            if (target.StartsWith('#'))
            {
                var anchor = target.TrimStart('#');
                if (!slugs.Contains(anchor))
                {
                    document.Errors.Add($"Link quebrado (âncora inexistente): {target}");
                }

                continue;
            }

            var filePart = target.Split('#')[0];
            if (filePart.Length == 0)
            {
                continue;
            }

            var resolved = Path.GetFullPath(Path.Combine(directory, filePart));
            if (!File.Exists(resolved))
            {
                document.Errors.Add($"Link quebrado (arquivo inexistente): {target}");
            }
        }

        foreach (Match match in ImagePattern.Matches(document.Content))
        {
            var target = match.Groups[1].Value.Trim();
            if (target.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || IsExternal(target))
            {
                continue;
            }

            var resolved = Path.GetFullPath(Path.Combine(directory, target));
            if (!File.Exists(resolved))
            {
                document.Errors.Add($"Imagem inexistente: {target}");
            }
        }
    }

    private void AnalyzeCrossDocument(IReadOnlyList<AnalyzedDocument> documents)
    {
        foreach (var group in documents.Where(d => d.Title is not null).GroupBy(d => d.Title!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            var paths = string.Join(", ", group.Select(d => d.Artifact.RelativePath));
            foreach (var document in group)
            {
                document.Warnings.Add($"Título duplicado entre documentos: \"{group.Key}\" também usado em {paths}.");
            }
        }

        for (var i = 0; i < documents.Count; i++)
        {
            for (var j = i + 1; j < documents.Count; j++)
            {
                var a = documents[i];
                var b = documents[j];
                if (a.WordCount == 0 || b.WordCount == 0)
                {
                    continue;
                }

                if (string.Equals(Normalize(a.Content), Normalize(b.Content), StringComparison.Ordinal))
                {
                    a.Errors.Add($"Documento idêntico a {b.Artifact.RelativePath}.");
                    b.Errors.Add($"Documento idêntico a {a.Artifact.RelativePath}.");
                    continue;
                }

                var similarity = JaccardSimilarity(a.NormalizedWords, b.NormalizedWords);
                if (similarity >= _options.SimilarityThreshold)
                {
                    a.Warnings.Add($"Documento muito semelhante a {b.Artifact.RelativePath} ({similarity:P0} de similaridade).");
                    b.Warnings.Add($"Documento muito semelhante a {a.Artifact.RelativePath} ({similarity:P0} de similaridade).");
                }
            }
        }
    }

    private static double JaccardSimilarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0)
        {
            return 0;
        }

        var intersection = a.Count(b.Contains);
        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static string Normalize(string content) =>
        Regex.Replace(content, @"\s+", " ").Trim();

    private static bool IsExternal(string target) =>
        target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> BuildRecommendations(IReadOnlyList<AnalyzedDocument> documents)
    {
        var recommendations = new List<string>();

        var withErrors = documents.Where(d => d.Errors.Count > 0).Select(d => d.Artifact.RelativePath).ToList();
        if (withErrors.Count > 0)
        {
            recommendations.Add($"Corrigir erros estruturais/de integridade em: {string.Join(", ", withErrors)}.");
        }

        var withWarnings = documents.Where(d => d.Warnings.Count > 0 && d.Errors.Count == 0).Select(d => d.Artifact.RelativePath).ToList();
        if (withWarnings.Count > 0)
        {
            recommendations.Add($"Revisar avisos (conteúdo curto, headings ou duplicações) em: {string.Join(", ", withWarnings)}.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Nenhuma ação necessária — toda a documentação analisada está saudável.");
        }

        return recommendations;
    }

    private static string Render(DocumentationHealthReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Documentation Health");
        builder.AppendLine();
        builder.AppendLine("## Resumo");
        builder.AppendLine();
        builder.AppendLine($"- Documentos: {report.TotalDocuments}");
        builder.AppendLine($"- Saudáveis: {report.HealthyCount}");
        builder.AppendLine($"- Avisos: {report.WarningCount}");
        builder.AppendLine($"- Erros: {report.ErrorCount}");
        builder.AppendLine();

        builder.AppendLine("## Cobertura");
        builder.AppendLine();
        if (report.Documents.Count == 0)
        {
            builder.AppendLine("Nenhum documento Markdown foi encontrado entre os artefatos publicados.");
        }
        else
        {
            builder.AppendLine("| Documento | Status | Palavras |");
            builder.AppendLine("|---|---|---|");
            foreach (var document in report.Documents)
            {
                builder.AppendLine($"| {document.RelativePath} | {StatusLabel(document.Status)} | {document.WordCount} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Estrutura e Links");
        builder.AppendLine();
        var anyIssue = false;
        foreach (var document in report.Documents)
        {
            if (document.Errors.Count == 0 && document.Warnings.Count == 0)
            {
                continue;
            }

            anyIssue = true;
            builder.AppendLine($"### {document.RelativePath}");
            builder.AppendLine();
            foreach (var error in document.Errors)
            {
                builder.AppendLine($"- ❌ {error}");
            }

            foreach (var warning in document.Warnings)
            {
                builder.AppendLine($"- ⚠️ {warning}");
            }

            builder.AppendLine();
        }

        if (!anyIssue)
        {
            builder.AppendLine("Nenhum problema estrutural, de link ou de conteúdo encontrado.");
            builder.AppendLine();
        }

        builder.AppendLine("## Recomendações");
        builder.AppendLine();
        foreach (var recommendation in report.Recommendations)
        {
            builder.AppendLine($"- {recommendation}");
        }

        return builder.ToString();
    }

    private static string StatusLabel(DocumentHealthStatus status) => status switch
    {
        DocumentHealthStatus.Healthy => "✅ Saudável",
        DocumentHealthStatus.Warning => "⚠️ Aviso",
        DocumentHealthStatus.Error => "❌ Erro",
        _ => status.ToString(),
    };

    private sealed class AnalyzedDocument
    {
        public AnalyzedDocument(PublishedArtifact artifact, string content)
        {
            Artifact = artifact;
            Content = content;
        }

        public PublishedArtifact Artifact { get; }

        public string Content { get; }

        public int WordCount { get; set; }

        public string? Title { get; set; }

        public IReadOnlyList<(int Level, string Text)> Headings { get; set; } = Array.Empty<(int, string)>();

        public HashSet<string> NormalizedWords { get; set; } = new();

        public List<string> Errors { get; } = new();

        public List<string> Warnings { get; } = new();
    }
}
