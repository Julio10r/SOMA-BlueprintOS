using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Client;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IFaqGenerator"/>. Não existe, até o momento, um repositório
/// real de perguntas frequentes; este gerador produz um documento mínimo e honesto.
/// </summary>
public sealed class FaqGenerator : IFaqGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Perguntas frequentes (FAQ)");
        builder.AppendLine();
        builder.AppendLine("Nenhuma pergunta frequente registrada ainda.");
        builder.AppendLine();
        builder.AppendLine("Este documento será populado conforme dúvidas recorrentes de usuários e clientes");
        builder.AppendLine("forem identificadas durante a operação do produto.");

        return Task.FromResult(builder.ToString());
    }
}
