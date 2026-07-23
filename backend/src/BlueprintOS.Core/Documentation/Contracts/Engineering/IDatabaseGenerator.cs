namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação de banco de dados. Como o backend ainda
/// não possui <c>DbContext</c> ou entidades EF Core persistentes, o gerador deve refletir
/// esse estado honestamente.
/// </summary>
public interface IDatabaseGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de banco de dados.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
