namespace BlueprintOS.Infrastructure.Publication;

/// <summary>
/// Configuração utilizada pelo Publication Engine.
/// </summary>
public sealed class PublicationOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "Publication";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, onde os documentos publicados (HTML/PDF/Markdown)
    /// são gravados.
    /// </summary>
    public string DistRootPath { get; set; } = "dist";

    /// <summary>
    /// Caminho, relativo ou absoluto, para a solution do backend, usado para coletar o status
    /// real de build (warnings/erros) exibido no Relatório Executivo.
    /// </summary>
    public string SolutionPath { get; set; } = "backend/BlueprintOS.sln";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, dos projetos de teste, usado para contar a
    /// quantidade real de testes exibida no Relatório Executivo.
    /// </summary>
    public string TestsRootPath { get; set; } = "backend/tests";

    /// <summary>
    /// Versão do projeto exibida na capa e no rodapé dos documentos publicados.
    /// </summary>
    public string ProjectVersion { get; set; } = "1.0.0";
}
