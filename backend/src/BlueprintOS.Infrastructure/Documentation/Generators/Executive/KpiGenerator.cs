using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Executive;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IKpiGenerator"/>. O BlueprintOS ainda não possui KPIs de negócio
/// formalizados para produção, mas já existem métricas técnicas e evidências operacionais reais
/// de validação; este gerador distingue as duas coisas em vez de negar toda evidência existente.
/// </summary>
public sealed class KpiGenerator : IKpiGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Indicadores-chave de desempenho (KPIs)");
        builder.AppendLine();
        builder.AppendLine("Nenhum KPI de negócio formalizado para produção até o momento — o BlueprintOS");
        builder.AppendLine("ainda não está em operação em produção (ver `.ai/ROADMAP.md`).");
        builder.AppendLine();
        builder.AppendLine("Já existem, porém, métricas técnicas e evidências operacionais reais de validação —");
        builder.AppendLine("build, suíte de testes automatizados e sincronizações reais contra o ERP corporativo —");
        builder.AppendLine("detalhadas em `.ai/PROJECT_STATE.md` e `.ai/BACKLOG.md`. KPIs de negócio formais serão");
        builder.AppendLine("introduzidos quando módulos de Analytics/Dashboard entrarem em operação (ver Roadmap).");

        return Task.FromResult(builder.ToString());
    }
}
