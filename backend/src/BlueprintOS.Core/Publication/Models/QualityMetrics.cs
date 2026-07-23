namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Indicadores reais de qualidade de build e testes do repositório, coletados no momento da
/// publicação (nunca fabricados), para uso no Relatório Executivo.
/// </summary>
/// <param name="BuildSucceeded">Indica se a última execução de <c>dotnet build</c> foi bem-sucedida.</param>
/// <param name="WarningCount">Quantidade de warnings reportados pelo build.</param>
/// <param name="ErrorCount">Quantidade de erros reportados pelo build.</param>
/// <param name="TestCount">Quantidade de métodos de teste (<c>[Fact]</c>/<c>[Theory]</c>) encontrados no repositório.</param>
/// <param name="Summary">Resumo textual do resultado do build, como reportado pelo próprio `dotnet build`.</param>
public sealed record QualityMetrics(
    bool BuildSucceeded,
    int WarningCount,
    int ErrorCount,
    int TestCount,
    string Summary);
