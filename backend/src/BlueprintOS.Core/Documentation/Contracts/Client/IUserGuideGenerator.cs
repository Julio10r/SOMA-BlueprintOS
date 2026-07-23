namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de guia do usuário final. Como o frontend ainda não foi
/// construído (ver <c>.ai/memory/known_issues.md</c>), o conteúdo deve refletir esse estado
/// honestamente, em vez de descrever telas ou fluxos inexistentes.
/// </summary>
public interface IUserGuideGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do guia do usuário.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
