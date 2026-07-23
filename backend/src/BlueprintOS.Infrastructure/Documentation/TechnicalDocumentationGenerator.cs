using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="ITechnicalDocumentationGenerator"/> que gera Markdown estruturado
/// a partir de metadados de módulo.
/// </summary>
public sealed class TechnicalDocumentationGenerator : ITechnicalDocumentationGenerator
{
    /// <inheritdoc />
    public string Generate(ModuleMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Documentação Técnica — Módulo {metadata.ModuleName}");
        builder.AppendLine();
        builder.AppendLine(metadata.Description);
        builder.AppendLine();

        builder.AppendLine("## Contratos");
        builder.AppendLine();
        AppendList(builder, metadata.Contracts);
        builder.AppendLine();

        builder.AppendLine("## Classes");
        builder.AppendLine();
        AppendList(builder, metadata.Classes);

        return builder.ToString();
    }

    private static void AppendList(StringBuilder builder, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("_Nenhum item registrado._");
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- `{item}`");
        }
    }
}
