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
        builder.AppendLine("infraestrutura via `AddInfrastructure` e expõe saúde e uma recomendação consultiva:");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine("GET /health");
        builder.AppendLine("  -> 200 OK { Status, Application, Environment, Version }");
        builder.AppendLine();
        builder.AppendLine("POST /api/v1/negociacoes/recomendacoes");
        builder.AppendLine("  -> 200 OK { RequestId, Strategy, Justifications, Alerts, SuccessProbability, HumanDecisionRequired }");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.");
        builder.AppendLine("O controller delega ao caso de uso Application, reutiliza contratos de memória e estratégia e não altera estado.");

        return Task.FromResult(builder.ToString());
    }
}
