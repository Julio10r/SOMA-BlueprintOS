using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IAdrService"/> que persiste cada ADR como um arquivo Markdown
/// individual em um diretório configurado, seguindo o formato de entradas de <c>.ai/DECISIONS.md</c>.
/// </summary>
public sealed class MarkdownAdrService : IAdrService
{
    private static readonly Regex IdPattern = new(@"^ADR-(\d{4})\.md$", RegexOptions.Compiled);

    private readonly string _directoryPath;

    public MarkdownAdrService(IOptions<DocumentationOptions> options)
    {
        _directoryPath = options.Value.AdrDirectoryPath;
    }

    /// <inheritdoc />
    public async Task<AdrRecord> CreateAsync(
        string title,
        string context,
        string decision,
        string consequences,
        AdrStatus status = AdrStatus.Proposed,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directoryPath);

        var nextNumber = GetNextSequenceNumber();
        var id = $"ADR-{nextNumber:D4}";
        var record = new AdrRecord(id, title, status, context, decision, consequences, DateTimeOffset.UtcNow);

        await File.WriteAllTextAsync(GetFilePath(id), Render(record), cancellationToken);

        return record;
    }

    /// <inheritdoc />
    public async Task<AdrRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return Parse(id, content);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdrRecord>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directoryPath))
        {
            return Array.Empty<AdrRecord>();
        }

        var records = new List<AdrRecord>();
        foreach (var file in Directory.GetFiles(_directoryPath, "ADR-*.md"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            records.Add(Parse(id, content));
        }

        return records.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    private int GetNextSequenceNumber()
    {
        if (!Directory.Exists(_directoryPath))
        {
            return 1;
        }

        var max = 0;
        foreach (var file in Directory.GetFiles(_directoryPath, "ADR-*.md"))
        {
            var match = IdPattern.Match(Path.GetFileName(file));
            if (match.Success && int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) is var number && number > max)
            {
                max = number;
            }
        }

        return max + 1;
    }

    private string GetFilePath(string id) => Path.Combine(_directoryPath, $"{id}.md");

    private static string Render(AdrRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {record.Id}: {record.Title}");
        builder.AppendLine();
        builder.AppendLine($"**Status:** {record.Status}");
        builder.AppendLine();
        builder.AppendLine($"**Contexto:** {record.Context}");
        builder.AppendLine();
        builder.AppendLine($"**Decisão:** {record.Decision}");
        builder.AppendLine();
        builder.AppendLine($"**Consequências:** {record.Consequences}");
        builder.AppendLine();
        builder.AppendLine($"**CreatedAt:** {record.CreatedAt:O}");
        return builder.ToString();
    }

    private static AdrRecord Parse(string id, string content)
    {
        var title = ExtractLine(content, $@"^#\s*{Regex.Escape(id)}:\s*(.+)$") ?? id;
        var status = ExtractLine(content, @"^\*\*Status:\*\*\s*(.+)$") ?? nameof(AdrStatus.Proposed);
        var context = ExtractLine(content, @"^\*\*Contexto:\*\*\s*(.+)$") ?? string.Empty;
        var decision = ExtractLine(content, @"^\*\*Decisão:\*\*\s*(.+)$") ?? string.Empty;
        var consequences = ExtractLine(content, @"^\*\*Consequências:\*\*\s*(.+)$") ?? string.Empty;
        var createdAtRaw = ExtractLine(content, @"^\*\*CreatedAt:\*\*\s*(.+)$");

        var createdAt = createdAtRaw is not null && DateTimeOffset.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;

        var parsedStatus = Enum.TryParse<AdrStatus>(status, ignoreCase: true, out var statusValue) ? statusValue : AdrStatus.Proposed;

        return new AdrRecord(id, title, parsedStatus, context, decision, consequences, createdAt);
    }

    private static string? ExtractLine(string content, string pattern)
    {
        var match = Regex.Match(content, pattern, RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
