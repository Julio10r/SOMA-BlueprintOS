using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IDeployGenerator"/>, refletindo os artefatos reais de
/// containerização presentes no repositório (<c>Dockerfile</c> e <c>docker-compose</c>).
/// </summary>
public sealed class DeployGenerator : IDeployGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Deploy");
        builder.AppendLine();
        builder.AppendLine("O deploy do BlueprintOS é baseado em containers Docker:");
        builder.AppendLine();
        builder.AppendLine("- **`backend/src/BlueprintOS.Api/Dockerfile`** — build multi-stage: publica");
        builder.AppendLine("  `BlueprintOS.Api` com o SDK .NET 9 e executa a imagem publicada sobre");
        builder.AppendLine("  `mcr.microsoft.com/dotnet/aspnet:9.0`, expondo a porta `8080`");
        builder.AppendLine("  (`ASPNETCORE_URLS=http://+:8080`).");
        builder.AppendLine("- **`infrastructure/docker/docker-compose.yml`** e");
        builder.AppendLine("  **`docker-compose.override.yml`** — orquestração local dos serviços.");
        builder.AppendLine();
        builder.AppendLine("Não há, até o momento, pipeline de CI/CD (ex.: GitHub Actions) configurado no");
        builder.AppendLine("repositório.");

        return Task.FromResult(builder.ToString());
    }
}
