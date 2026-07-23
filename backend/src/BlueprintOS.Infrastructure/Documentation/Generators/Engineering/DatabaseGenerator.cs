using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IDatabaseGenerator"/>. O backend ainda não possui nenhum
/// <c>DbContext</c> ou entidade EF Core persistente; este gerador reflete esse estado
/// honestamente em vez de inventar um schema.
/// </summary>
public sealed class DatabaseGenerator : IDatabaseGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Banco de dados");
        builder.AppendLine();
        builder.AppendLine("Nenhum schema de banco de dados definido até o momento.");
        builder.AppendLine();
        builder.AppendLine("O backend ainda não possui nenhum `DbContext` (EF Core) nem entidades persistentes.");
        builder.AppendLine("A persistência atual dos módulos existentes (ex.: `Documentation`, `Knowledge`) é feita");
        builder.AppendLine("em memória (`InMemoryDocumentationRepository`) ou em arquivos Markdown (ADRs, changelog),");
        builder.AppendLine("adequado ao escopo das sprints entregues até aqui. Este documento será atualizado assim");
        builder.AppendLine("que um `DbContext` real for introduzido no projeto.");

        return Task.FromResult(builder.ToString());
    }
}
