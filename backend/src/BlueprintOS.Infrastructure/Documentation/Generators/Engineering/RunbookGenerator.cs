using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IRunbookGenerator"/>. Não existe um catálogo formal e completo
/// de runbooks de produção, mas já há orientações operacionais e lições de troubleshooting
/// registradas na documentação do projeto; este gerador distingue as duas coisas honestamente.
/// </summary>
public sealed class RunbookGenerator : IRunbookGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Runbook operacional");
        builder.AppendLine();
        builder.AppendLine("Não há, até o momento, um catálogo formal e completo de runbooks de produção");
        builder.AppendLine("(o BlueprintOS ainda não está em operação em produção — ver `.ai/ROADMAP.md`).");
        builder.AppendLine();
        builder.AppendLine("Já existem, porém, orientações operacionais e lições de troubleshooting reais");
        builder.AppendLine("registradas ao longo do projeto — por exemplo, em `.ai/memory/completed_sprints.md`");
        builder.AppendLine("(incidentes encontrados e corrigidos em validações reais contra o ERP/SQL Server");
        builder.AppendLine("corporativo), em `.ai/memory/known_issues.md` e no");
        builder.AppendLine("[Engineering Handbook](../Engineering%20Handbook.md). Um catálogo de runbooks formal");
        builder.AppendLine("será consolidado quando houver operação real em produção.");

        return Task.FromResult(builder.ToString());
    }
}
