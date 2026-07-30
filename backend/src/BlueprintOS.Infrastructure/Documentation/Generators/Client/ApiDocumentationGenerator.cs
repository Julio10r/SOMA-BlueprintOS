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
        builder.AppendLine("A API pública do BlueprintOS ainda está em estágio inicial. Além do endpoint");
        builder.AppendLine("de saúde, há um fluxo consultivo de recomendação de negociação:");
        builder.AppendLine();
        builder.AppendLine("| Método | Rota | Descrição |");
        builder.AppendLine("|--------|------|-----------|");
        builder.AppendLine("| GET | `/health` | Retorna o status de saúde da aplicação. |");
        builder.AppendLine("| POST | `/api/v1/negociacoes/recomendacoes` | Retorna recomendação consultiva; não altera estado e exige decisão humana. |");
        builder.AppendLine();
        builder.AppendLine("A identidade temporária só é aceita em Development; fora desse ambiente a operação");
        builder.AppendLine("falha de forma segura. Não há persistência, ERP ou execução automática de compras.");

        return Task.FromResult(builder.ToString());
    }
}
