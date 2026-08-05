using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IDatabaseGenerator"/>, refletindo a persistência real do
/// vertical slice de Fornecedores via <c>BlueprintOSDbContext</c> (EF Core + SQL Server).
/// </summary>
public sealed class DatabaseGenerator : IDatabaseGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Banco de dados");
        builder.AppendLine();
        builder.AppendLine("O backend possui um `DbContext` real: `BlueprintOSDbContext`");
        builder.AppendLine("(`backend/src/BlueprintOS.Infrastructure/Persistence/`), usando Entity Framework Core");
        builder.AppendLine("com SQL Server. Ele persiste o domínio de Fornecedores (cadastro, descoberta, sincronização");
        builder.AppendLine("com o ERP e histórico de consulta de CNPJ), com migrations reais aplicadas nesse mesmo");
        builder.AppendLine("projeto (`Persistence/Migrations/`).");
        builder.AppendLine();
        builder.AppendLine("O banco é sempre externo — bancos corporativos `MAISCOMPRAS`/`SOMA_DESENV`, acessados via");
        builder.AppendLine("VPN — nunca um SQL Server local ou em container. Não há pasta `database/` na raiz do");
        builder.AppendLine("repositório nem scripts/seeds de banco separados; a persistência dos demais módulos");
        builder.AppendLine("(ex.: `Documentation`, `Knowledge`) permanece em memória ou em arquivos Markdown.");
        builder.AppendLine("Este documento será atualizado conforme novos módulos passarem a persistir dados.");

        return Task.FromResult(builder.ToString());
    }
}
