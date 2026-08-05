namespace BlueprintOS.Infrastructure.Publication;

/// <summary>
/// Configuração utilizada pelo Publication Engine. A partir da ADR-0019, o Engine tem exatamente
/// uma fonte (<see cref="DocsRootPath"/>) e um destino (<see cref="DistRootPath"/>) — nenhuma
/// configuração aponta para <c>.ai/content/</c>.
/// </summary>
public sealed class PublicationOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "Publication";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, de onde os documentos técnicos (<c>.md</c>) são
    /// descobertos e publicados. Única fonte do Publication Engine.
    /// </summary>
    public string DocsRootPath { get; set; } = "docs";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, onde os documentos publicados (HTML/PDF/Markdown)
    /// são gravados. Único destino do Publication Engine — nunca <see cref="DocsRootPath"/> nem
    /// um diretório dentro dele.
    /// </summary>
    public string DistRootPath { get; set; } = "dist";

    /// <summary>
    /// Nomes de diretórios de topo (relativos a <see cref="DocsRootPath"/>) que nunca são
    /// descobertos para publicação — histórico de auditorias e material de demonstração não
    /// compõem a documentação técnica publicável.
    /// </summary>
    public HashSet<string> ExcludedTopLevelDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "audits",
        "demo",
    };

    /// <summary>
    /// Caminho, relativo ou absoluto, para a solution do backend, usado para coletar o status
    /// real de build (warnings/erros) exibido no relatório de publicação.
    /// </summary>
    public string SolutionPath { get; set; } = "backend/BlueprintOS.sln";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, dos projetos de teste, usado para contar a
    /// quantidade real de testes.
    /// </summary>
    public string TestsRootPath { get; set; } = "backend/tests";

    /// <summary>
    /// Versão do projeto exibida no rodapé dos documentos publicados.
    /// </summary>
    public string ProjectVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Valida que <see cref="DocsRootPath"/> existe e que <see cref="DistRootPath"/> não é igual
    /// nem está contido dentro de <see cref="DocsRootPath"/> (e vice-versa), prevenindo escrita
    /// acidental sobre a fonte, sobre <c>.ai/</c> ou fora da árvore esperada por path traversal.
    /// Lança <see cref="InvalidOperationException"/> quando a configuração é insegura.
    /// </summary>
    public void ValidateSafePaths()
    {
        var docsFullPath = Path.GetFullPath(DocsRootPath);
        var distFullPath = Path.GetFullPath(DistRootPath);

        if (!Directory.Exists(docsFullPath))
        {
            throw new InvalidOperationException(
                $"DocsRootPath não existe: '{docsFullPath}'. O Publication Engine exige uma fonte de documentação real.");
        }

        if (IsSameOrDescendant(distFullPath, docsFullPath))
        {
            throw new InvalidOperationException(
                $"DistRootPath ('{distFullPath}') não pode ser igual a DocsRootPath nem estar contido dentro dele ('{docsFullPath}').");
        }

        if (IsSameOrDescendant(docsFullPath, distFullPath))
        {
            throw new InvalidOperationException(
                $"DocsRootPath ('{docsFullPath}') não pode ser igual a DistRootPath nem estar contido dentro dele ('{distFullPath}').");
        }

        var aiFullPath = Path.GetFullPath(Path.Combine(docsFullPath, "..", ".ai"));
        if (Directory.Exists(aiFullPath) && IsSameOrDescendant(distFullPath, aiFullPath))
        {
            throw new InvalidOperationException(
                $"DistRootPath ('{distFullPath}') não pode apontar para dentro de '.ai/' ('{aiFullPath}').");
        }
    }

    private static bool IsSameOrDescendant(string candidate, string ancestor)
    {
        var normalizedCandidate = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedAncestor = ancestor.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedCandidate, normalizedAncestor, StringComparison.Ordinal))
        {
            return true;
        }

        var ancestorWithSeparator = normalizedAncestor + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(ancestorWithSeparator, StringComparison.Ordinal);
    }
}
