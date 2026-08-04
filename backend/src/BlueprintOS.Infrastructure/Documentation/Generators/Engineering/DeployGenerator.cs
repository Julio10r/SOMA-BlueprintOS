using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IDeployGenerator"/>, refletindo o ambiente de desenvolvimento
/// real do repositório: backend .NET e frontend Vite executados diretamente, sem Docker
/// (ver ADR-0019 em <c>.ai/DECISIONS.md</c>), sempre contra um SQL Server externo.
/// </summary>
public sealed class DeployGenerator : IDeployGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Deploy");
        builder.AppendLine();
        builder.AppendLine("O ambiente oficial de desenvolvimento do BlueprintOS não usa Docker (ver");
        builder.AppendLine("ADR-0019 em `.ai/DECISIONS.md`):");
        builder.AppendLine();
        builder.AppendLine("- **Backend** — API .NET executada diretamente via `dotnet run`");
        builder.AppendLine("  (`backend/src/BlueprintOS.Api`), perfil `http` (`launchSettings.json`),");
        builder.AppendLine("  porta `5262`.");
        builder.AppendLine("- **Frontend** — React/Vite executado via `npm run dev`");
        builder.AppendLine("  (`frontend/web`), porta `5173`.");
        builder.AppendLine("- **Banco de dados** — sempre SQL Server externo (bancos corporativos");
        builder.AppendLine("  `MAISCOMPRAS`/`SOMA_DESENV`, acessado via VPN), nunca um container local.");
        builder.AppendLine();
        builder.AppendLine("Não há, até o momento, pipeline de CI/CD (ex.: GitHub Actions) nem ambiente");
        builder.AppendLine("de homologação configurado no repositório.");

        return Task.FromResult(builder.ToString());
    }
}
