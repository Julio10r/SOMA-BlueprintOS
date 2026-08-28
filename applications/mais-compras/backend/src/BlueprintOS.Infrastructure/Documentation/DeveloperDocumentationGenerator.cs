using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IDeveloperDocumentationGenerator"/> que gera Markdown no estilo
/// de um README/guia técnico para desenvolvedores.
/// </summary>
public sealed class DeveloperDocumentationGenerator : IDeveloperDocumentationGenerator
{
    /// <inheritdoc />
    public string Generate(DeveloperDocumentationInput input)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {input.Title}");
        builder.AppendLine();
        builder.AppendLine(input.Overview);
        builder.AppendLine();

        builder.AppendLine("## Setup");
        builder.AppendLine();
        AppendNumbered(builder, input.SetupSteps);
        builder.AppendLine();

        builder.AppendLine("## Exemplos de Uso");
        builder.AppendLine();
        AppendBullets(builder, input.UsageExamples);
        builder.AppendLine();

        builder.AppendLine("## Troubleshooting");
        builder.AppendLine();
        AppendBullets(builder, input.Troubleshooting);

        return builder.ToString();
    }

    private static void AppendNumbered(StringBuilder builder, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("_Nenhum passo registrado._");
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            builder.AppendLine($"{i + 1}. {items[i]}");
        }
    }

    private static void AppendBullets(StringBuilder builder, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("_Nenhum item registrado._");
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }
    }
}
