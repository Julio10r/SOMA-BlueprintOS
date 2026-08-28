using System.Diagnostics;
using System.Globalization;
using BlueprintOS.Core.Documentation.Contracts;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IGitLogReader"/> que consulta o histórico Git local via
/// <c>git log</c>, executado como processo. Realiza apenas leitura de metadados locais
/// (sem rede, sem commit/push).
/// </summary>
public sealed class GitCliDocumentationService : IGitLogReader
{
    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastCommitDateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("git", $"log -1 --format=%cI -- \"{filePath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var trimmed = output.Trim();
        if (process.ExitCode != 0 || string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;
    }
}
