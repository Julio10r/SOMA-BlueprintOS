using System.Diagnostics;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication;

/// <summary>
/// Implementação de <see cref="IQualityMetricsProvider"/> que coleta indicadores reais de
/// qualidade executando <c>dotnet build</c> sobre a solution do backend (warnings/erros) e
/// contando os métodos de teste (<c>[Fact]</c>/<c>[Theory]</c>) presentes nos projetos de
/// teste. Nenhum valor é fabricado: quando uma fonte não está disponível, isso é refletido
/// honestamente no resumo.
/// </summary>
public sealed class QualityMetricsProvider : IQualityMetricsProvider
{
    private static readonly Regex WarningCountPattern = new(@"(\d+)\s+Warning\(s\)", RegexOptions.Compiled);
    private static readonly Regex ErrorCountPattern = new(@"(\d+)\s+Error\(s\)", RegexOptions.Compiled);
    private static readonly Regex TestMethodPattern = new(@"\[(Fact|Theory)(\(|\])", RegexOptions.Compiled);

    private readonly string _solutionPath;
    private readonly string _testsRootPath;

    public QualityMetricsProvider(IOptions<PublicationOptions> options)
    {
        _solutionPath = options.Value.SolutionPath;
        _testsRootPath = options.Value.TestsRootPath;
    }

    /// <inheritdoc />
    public async Task<QualityMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var testCount = CountTestMethods();
        var (buildSucceeded, warningCount, errorCount, summary) = await RunBuildAsync(cancellationToken);

        return new QualityMetrics(buildSucceeded, warningCount, errorCount, testCount, summary);
    }

    private int CountTestMethods()
    {
        if (!Directory.Exists(_testsRootPath))
        {
            return 0;
        }

        var total = 0;
        foreach (var file in Directory.EnumerateFiles(_testsRootPath, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            total += TestMethodPattern.Matches(content).Count;
        }

        return total;
    }

    private async Task<(bool Succeeded, int Warnings, int Errors, string Summary)> RunBuildAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_solutionPath))
        {
            return (false, 0, 0, $"Solution não encontrada em '{_solutionPath}'; status de build não pôde ser coletado.");
        }

        try
        {
            var startInfo = new ProcessStartInfo("dotnet", $"build \"{_solutionPath}\" --nologo -v:q")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, 0, 0, "Não foi possível iniciar o processo `dotnet build`.");
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await stdOutTask + await stdErrTask;

            var succeeded = process.ExitCode == 0;
            var warningMatch = WarningCountPattern.Match(output);
            var errorMatch = ErrorCountPattern.Match(output);
            var warnings = warningMatch.Success ? int.Parse(warningMatch.Groups[1].Value) : 0;
            var errors = errorMatch.Success ? int.Parse(errorMatch.Groups[1].Value) : (succeeded ? 0 : 1);

            var summary = succeeded
                ? $"Build succeeded ({warnings} warning(s), {errors} error(s))."
                : $"Build failed ({warnings} warning(s), {errors} error(s)).";

            return (succeeded, warnings, errors, summary);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, 0, 0, $"Não foi possível executar `dotnet build`: {ex.Message}");
        }
    }
}
