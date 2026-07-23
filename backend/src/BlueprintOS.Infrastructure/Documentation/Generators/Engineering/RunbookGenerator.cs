using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IRunbookGenerator"/>. Não existem, até o momento,
/// procedimentos operacionais reais registrados; este gerador produz um documento
/// mínimo e honesto.
/// </summary>
public sealed class RunbookGenerator : IRunbookGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Runbook operacional");
        builder.AppendLine();
        builder.AppendLine("Nenhum procedimento operacional (runbook) registrado ainda.");
        builder.AppendLine();
        builder.AppendLine("O BlueprintOS ainda não está em operação em produção (ver Roadmap — Fase 0);");
        builder.AppendLine("procedimentos de incidentes, rollback e observabilidade serão documentados aqui");
        builder.AppendLine("assim que existirem operações reais a partir da Fase 4 (Observabilidade e Escala).");

        return Task.FromResult(builder.ToString());
    }
}
