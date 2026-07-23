using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IAiDocumentationGenerator"/> que gera Markdown no formato utilizado
/// pelos documentos de contexto para agentes de IA em <c>.ai/context/*.md</c>.
/// </summary>
public sealed class AiDocumentationGenerator : IAiDocumentationGenerator
{
    /// <inheritdoc />
    public string Generate(AiDocumentationInput input)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# context/{input.Topic}.md");
        builder.AppendLine();
        builder.AppendLine($"> Escopo: {input.Scope}");
        builder.AppendLine();

        builder.AppendLine("## Propósito");
        builder.AppendLine();
        builder.AppendLine(input.Purpose);
        builder.AppendLine();

        builder.AppendLine("## Conceitos-Chave");
        builder.AppendLine();
        AppendBullets(builder, input.KeyConcepts);
        builder.AppendLine();

        builder.AppendLine("## Diretrizes");
        builder.AppendLine();
        AppendBullets(builder, input.Guidelines);
        builder.AppendLine();

        builder.AppendLine("## Referências");
        builder.AppendLine();
        AppendBullets(builder, input.References);

        return builder.ToString();
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
