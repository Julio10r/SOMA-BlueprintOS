using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Client;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IUserGuideGenerator"/>. Como o frontend do BlueprintOS
/// ainda não foi construído (ver <c>.ai/memory/known_issues.md</c>), este gerador reflete
/// esse estado honestamente em vez de descrever telas ou fluxos inexistentes.
/// </summary>
public sealed class UserGuideGenerator : IUserGuideGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Guia do usuário");
        builder.AppendLine();
        builder.AppendLine("O BlueprintOS ainda não possui uma interface de usuário (frontend) publicada.");
        builder.AppendLine("O projeto Web (React/TypeScript) previsto em PROJECT.md/ARCHITECTURE.md ainda não foi");
        builder.AppendLine("criado; toda a entrega até o momento é exclusivamente backend (ver `known_issues.md`).");
        builder.AppendLine();
        builder.AppendLine("Este guia será preenchido com instruções de uso reais assim que a interface de usuário");
        builder.AppendLine("estiver disponível.");

        return Task.FromResult(builder.ToString());
    }
}
