using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Executive;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IKpiGenerator"/>. Até o momento, o BlueprintOS não possui
/// nenhuma fonte real de indicadores de negócio (o produto ainda está na Fase 0 -
/// Fundação); este gerador reflete essa ausência honestamente em vez de inventar métricas.
/// </summary>
public sealed class KpiGenerator : IKpiGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Indicadores-chave de desempenho (KPIs)");
        builder.AppendLine();
        builder.AppendLine("Nenhum KPI de negócio registrado até o momento.");
        builder.AppendLine();
        builder.AppendLine("O BlueprintOS encontra-se na Fase 0 - Fundação (ver Roadmap), etapa em que ainda não há");
        builder.AppendLine("operação em produção nem dados de uso reais a partir dos quais derivar indicadores.");
        builder.AppendLine("Este documento será atualizado assim que módulos de Analytics/Dashboard (Fase 4 do");
        builder.AppendLine("Roadmap) passarem a coletar métricas reais.");

        return Task.FromResult(builder.ToString());
    }
}
