#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// B3 — Bloco 5A, decisão do Product Owner (03/09/2026, revisão pré-Gate B): não existe uma regra universal
/// de que toda integração Linx deve ser sempre FULL ou sempre INCREMENTAL — cada dataset declara sua própria
/// <see cref="ReadDatasetDefinition.EstrategiaNormal"/>. Reaproveitado tanto para essa estratégia normal
/// quanto para o modo de execução de uma carga específica (<see cref="ReadDatasetDefinition.ResolveCommandText"/>),
/// que são conceitos deliberadamente separados: um dataset com estratégia normal Incremental ainda pode
/// executar em modo Full sob demanda (recarga administrativa) sem deixar de ser, normalmente, incremental.
/// </summary>
public enum DatasetLoadKind
{
    Full = 1,
    Incremental = 2,
}

/// <summary>
/// Descreve a coluna (ou colunas, quando o dataset depende de mais de uma tabela de origem) usada como
/// watermark de um dataset Incremental, e a janela de sobreposição de segurança aplicada antes de comparar
/// contra o watermark armazenado — nunca cravada sem antes descobrir o tipo/precisão/comportamento real da
/// coluna (ver <see cref="ReadDatasetDefinition"/>). O pipeline é idempotente por design: reler uma pequena
/// sobreposição é sempre preferível a arriscar perder um registro na fronteira temporal.
/// </summary>
public sealed record WatermarkDefinition(
    IReadOnlyList<string> QualifiedColumns,
    string SqlType,
    TimeSpan OverlapWindow,
    string Description);

/// <summary>
/// One code-reviewed, pre-registered dataset a <see cref="IReadExecutionAdapter"/> is allowed to read. The
/// caller (an <see cref="Models.ActionProposal"/>) only ever carries <see cref="Name"/> in its
/// <c>Resource</c> field — never a query fragment. Everything that determines what is actually read (tables,
/// joins, columns, technical filters) lives in <see cref="FullCommandTextFactory"/>/<see cref="IncrementalCommandTextFactory"/>,
/// which only this assembly's own code can supply; there is no path by which a caller-supplied string becomes
/// part of the executed SQL.
///
/// FULL never means an artificial "always true" filter (e.g. a watermark greater than an arbitrarily old
/// date) — it is a genuinely different, complete query. Every dataset supports a FULL reload on demand
/// (<see cref="FullCommandTextFactory"/> is always present) regardless of <see cref="EstrategiaNormal"/>,
/// because reconciliation/recovery must always be possible (B3, item "Recarga Full administrativa").
/// </summary>
public sealed record ReadDatasetDefinition(
    string Name,
    string Description,
    string SourceConnectionProfileKey,
    string DestinationConnectionProfileKey,
    string DestinationTable,
    IReadOnlyList<string> Columns,
    DatasetLoadKind EstrategiaNormal,
    Func<string> FullCommandTextFactory,
    Func<string>? IncrementalCommandTextFactory = null,
    WatermarkDefinition? Watermark = null,
    int CommandTimeoutSeconds = 120)
{
    /// <summary>Nenhum dataset Incremental pode começar operando incrementalmente — a primeira carga é
    /// sempre Full, reconciliada e homologada (ver o estado de bootstrap/baseline do dataset, mantido pela
    /// camada de aplicação). Full-sob-demanda permanece sempre disponível independentemente disso.</summary>
    public bool BootstrapFullObrigatorio => EstrategiaNormal == DatasetLoadKind.Incremental;

    public string ResolveCommandText(DatasetLoadKind modo) => modo switch
    {
        DatasetLoadKind.Full => FullCommandTextFactory(),
        DatasetLoadKind.Incremental => IncrementalCommandTextFactory?.Invoke()
            ?? throw new InvalidOperationException($"Dataset '{Name}' nao declara uma query incremental."),
        _ => throw new ArgumentOutOfRangeException(nameof(modo), modo, null),
    };
}

/// <summary>
/// The fixed set of datasets a <see cref="IReadExecutionAdapter"/> may resolve <see cref="Models.ActionProposal.Resource"/>
/// against. Deliberately closed — there is no registration API reachable from a request; new datasets are
/// added only by changing the catalog's own implementation and going through code review.
/// </summary>
public interface IReadDatasetCatalog
{
    bool TryGet(string datasetName, out ReadDatasetDefinition? definition);
}

/// <summary>
/// Encodes/decodes <see cref="Models.ActionProposal.AdditionalContext"/> as the carrier of which
/// <see cref="DatasetLoadKind"/> a LiveRead proposal requests — deliberately NOT a new top-level field on
/// <see cref="Models.ActionProposal"/> (avoids a write-shaped-model change for a read-only concern) and
/// deliberately part of the hashed payload: a Full reload and the recurring Incremental load are different
/// operations with different risk profiles and must never share one <c>ApprovalGrant</c> — encoding the mode
/// here means they naturally hash differently (see <c>ActionProposal.ComputeHash</c>), so a standing approval
/// for the daily incremental proposal can NEVER be silently reused to authorize an exceptional Full reload.
/// </summary>
public static class DatasetLoadModeContext
{
    private const string Prefix = "loadMode=";

    /// <summary>Throws for anything other than the two defined <see cref="DatasetLoadKind"/> members — this
    /// side of the boundary is entirely internal (only ever called by this codebase with a literal enum
    /// value), but is validated anyway so neither side of the encode/decode pair can ever produce a value
    /// outside the closed set.</summary>
    public static string Encode(DatasetLoadKind modo) => modo switch
    {
        DatasetLoadKind.Full => Prefix + nameof(DatasetLoadKind.Full),
        DatasetLoadKind.Incremental => Prefix + nameof(DatasetLoadKind.Incremental),
        _ => throw new ArgumentOutOfRangeException(nameof(modo), modo, null),
    };

    /// <summary>
    /// B3/Bloco 5A, decisão do PO (fail-closed): deliberadamente NÃO retorna um valor com fallback — um
    /// <c>AdditionalContext</c> ausente, malformado ou não reconhecido é um erro de contrato, nunca uma
    /// escolha implícita de Full. Full é seguro quanto a perda de alterações, mas continua sendo uma
    /// operação real e potencialmente pesada: um typo ou uma configuração incorreta nunca deve disparar uma
    /// carga completa silenciosamente. Também deliberadamente NÃO usa
    /// <see cref="Enum.TryParse{TEnum}(string?,out TEnum)"/>: aquele overload também aceita o valor numérico
    /// subjacente como string (ex.: "1", "2", ou um "999" fora do intervalo que passaria a type-check como um
    /// <see cref="DatasetLoadKind"/> aparentemente válido, mas indefinido) — uma superfície de entrada aberta,
    /// não fechada. Esta é uma correspondência exaustiva, sensível a maiúsculas/minúsculas, contra exatamente
    /// as duas strings literais que <see cref="Encode"/> pode produzir; qualquer outra entrada — texto livre,
    /// dígitos, fragmentos parecidos com SQL, casing errado, espaço em branco extra — retorna <c>false</c> e
    /// NUNCA é usada. Não há caminho de <see cref="Models.ActionProposal.AdditionalContext"/> até nada além
    /// desses dois membros fixos e tipados, ou uma rejeição explícita: nunca pode virar conteúdo
    /// executável/interpretado, e nunca escolhe Full por omissão.
    /// </summary>
    public static bool TryDecode(string? additionalContext, out DatasetLoadKind modo)
    {
        if (additionalContext is null || !additionalContext.StartsWith(Prefix, StringComparison.Ordinal))
        {
            modo = default;
            return false;
        }

        switch (additionalContext[Prefix.Length..])
        {
            case nameof(DatasetLoadKind.Full):
                modo = DatasetLoadKind.Full;
                return true;
            case nameof(DatasetLoadKind.Incremental):
                modo = DatasetLoadKind.Incremental;
                return true;
            default:
                modo = default;
                return false;
        }
    }

}
