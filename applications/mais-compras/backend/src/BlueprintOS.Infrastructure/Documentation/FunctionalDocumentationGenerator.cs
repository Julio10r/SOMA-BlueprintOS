using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IFunctionalDocumentationGenerator"/> que gera Markdown descrevendo
/// os casos de uso funcionais de um módulo.
/// </summary>
public sealed class FunctionalDocumentationGenerator : IFunctionalDocumentationGenerator
{
    /// <inheritdoc />
    public string Generate(FunctionalDocumentationInput input)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Documentação Funcional — Módulo {input.ModuleName}");
        builder.AppendLine();

        foreach (var useCase in input.UseCases)
        {
            AppendUseCase(builder, useCase);
        }

        return builder.ToString();
    }

    private static void AppendUseCase(StringBuilder builder, UseCaseDescription useCase)
    {
        builder.AppendLine($"## {useCase.Name}");
        builder.AppendLine();
        builder.AppendLine(useCase.Description);
        builder.AppendLine();

        builder.AppendLine("**Atores:** " + string.Join(", ", useCase.Actors));
        builder.AppendLine();

        builder.AppendLine("**Passos:**");
        builder.AppendLine();
        for (var i = 0; i < useCase.Steps.Count; i++)
        {
            builder.AppendLine($"{i + 1}. {useCase.Steps[i]}");
        }

        builder.AppendLine();
        builder.AppendLine($"**Resultado esperado:** {useCase.ExpectedResult}");
        builder.AppendLine();
    }
}
