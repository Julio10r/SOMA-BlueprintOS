using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Client;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IFunctionalGuideGenerator"/> que descreve, de forma
/// resumida, as capacidades funcionais reais já implementadas nos módulos do backend
/// (Documentation, Knowledge, Agentes de IA e Negociação).
/// </summary>
public sealed class FunctionalGuideGenerator : IFunctionalGuideGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Guia funcional");
        builder.AppendLine();
        builder.AppendLine("Capacidades funcionais reais já implementadas no backend do BlueprintOS:");
        builder.AppendLine();
        builder.AppendLine("- **Documentação viva:** geração automática de documentação executiva, de cliente e de");
        builder.AppendLine("  engenharia, versionamento de documentos, changelog e Architecture Decision Records (ADRs).");
        builder.AppendLine("- **Conhecimento (Knowledge):** ingestão e recuperação de conhecimento organizacional a");
        builder.AppendLine("  partir de conteúdo Markdown.");
        builder.AppendLine("- **Agentes de IA:** runtime de agentes especializados (ex.: agente de conhecimento),");
        builder.AppendLine("  construídos sobre um runtime de IA comum.");
        builder.AppendLine("- **Negociação (Buyer sênior):** memória de negociação e motor de estratégia de");
        builder.AppendLine("  negociação baseado em regras (urgência, histórico do fornecedor, faixa de preço).");
        builder.AppendLine();
        builder.AppendLine("Funcionalidades de produto além destas ainda não foram implementadas; consulte o");
        builder.AppendLine("Roadmap para o planejamento de fases futuras.");

        return Task.FromResult(builder.ToString());
    }
}
