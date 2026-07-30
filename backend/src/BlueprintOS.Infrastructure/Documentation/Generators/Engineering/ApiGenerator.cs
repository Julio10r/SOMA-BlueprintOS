using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IApiGenerator"/>, refletindo os endpoints reais mapeados
/// em <c>BlueprintOS.Api/Program.cs</c>.
/// </summary>
public sealed class ApiGenerator : IApiGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## API — documentação técnica");
        builder.AppendLine();
        builder.AppendLine("`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de");
        builder.AppendLine("infraestrutura via `AddInfrastructure` e expõe saúde e o primeiro slice consultivo de negociação:");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine("GET /health");
        builder.AppendLine("  -> 200 OK { Status, Application, Environment, Version }");
        builder.AppendLine();
        builder.AppendLine("POST /api/v1/negotiations/history");
        builder.AppendLine("  -> 201 Created com o histórico consolidado do fornecedor (transitório)");
        builder.AppendLine("GET /api/v1/negotiations/suppliers/{supplierId}");
        builder.AppendLine("  -> 200 OK com histórico ou 404 Not Found");
        builder.AppendLine("POST /api/v1/negotiations/recommendations");
        builder.AppendLine("  -> 200 OK com recomendação explicável e humanDecisionRequired: true");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.");
        builder.AppendLine("Os endpoints de negociação são consultivos, sem persistência durável, identidade ou ação automática.");

        return Task.FromResult(builder.ToString());
    }
}
