using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Client;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IApiDocumentationGenerator"/> voltada a clientes/integradores,
/// refletindo a superfície real, hoje mínima, de endpoints expostos por <c>BlueprintOS.Api</c>.
/// </summary>
public sealed class ApiDocumentationGenerator : IApiDocumentationGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## API para clientes e integradores");
        builder.AppendLine();
        builder.AppendLine("A API pública do BlueprintOS ainda está em estágio inicial. O único endpoint");
        builder.AppendLine("disponível hoje é o de verificação de saúde do serviço:");
        builder.AppendLine();
        builder.AppendLine("| Método | Rota      | Descrição                              |");
        builder.AppendLine("|--------|-----------|-----------------------------------------|");
        builder.AppendLine("| GET    | `/health` | Retorna o status de saúde da aplicação. |");
        builder.AppendLine();
        builder.AppendLine("Novos endpoints de negócio serão documentados aqui automaticamente conforme forem");
        builder.AppendLine("expostos em `BlueprintOS.Api`.");

        return Task.FromResult(builder.ToString());
    }
}
