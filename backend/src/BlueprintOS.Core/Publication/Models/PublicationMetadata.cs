namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Metadados de um <see cref="PublicationDocument"/>: tudo o que descreve o documento em si
/// (não o seu conteúdo), exibido na capa, no rodapé e futuramente usado por recursos como
/// controle de revisão, marca d'água e referências cruzadas entre documentos.
/// </summary>
/// <param name="Title">Título do documento.</param>
/// <param name="Subtitle">Subtítulo/tagline exibido na capa.</param>
/// <param name="Audience">Público-alvo do documento (ex.: "Diretoria", "Clientes", "Equipe de Engenharia").</param>
/// <param name="Version">Versão do projeto/documento.</param>
/// <param name="GeneratedAt">Data e hora em que este documento foi gerado.</param>
/// <param name="LastUpdated">Data da última atualização de conteúdo relevante (pode coincidir com <paramref name="GeneratedAt"/> até que exista um histórico de revisões real).</param>
/// <param name="Author">Autor/responsável pela geração do documento.</param>
/// <param name="Company">Nome da empresa/organização.</param>
/// <param name="Classification">Classificação de confidencialidade do documento.</param>
/// <param name="Tags">Marcadores livres para categorização e busca.</param>
/// <param name="RevisionHistory">Histórico de revisões do documento (ponto de extensão; tipicamente contém apenas a revisão atual nesta sprint).</param>
public sealed record PublicationMetadata(
    string Title,
    string Subtitle,
    string Audience,
    string Version,
    DateTimeOffset GeneratedAt,
    DateTimeOffset LastUpdated,
    string Author,
    string Company,
    PublicationClassification Classification,
    IReadOnlyList<string> Tags,
    IReadOnlyList<PublicationRevision> RevisionHistory)
{
    /// <summary>
    /// Cria metadados padrão para um documento gerado agora pelo Publication Engine, sem
    /// histórico de revisões anterior (revisão inicial apenas).
    /// </summary>
    public static PublicationMetadata Create(
        string title,
        string subtitle,
        string audience,
        string version,
        DateTimeOffset generatedAt,
        IReadOnlyList<string>? tags = null,
        PublicationClassification classification = PublicationClassification.Internal) => new(
        Title: title,
        Subtitle: subtitle,
        Audience: audience,
        Version: version,
        GeneratedAt: generatedAt,
        LastUpdated: generatedAt,
        Author: "Publication Engine (BlueprintOS)",
        Company: "SOMA",
        Classification: classification,
        Tags: tags ?? Array.Empty<string>(),
        RevisionHistory: new[] { new PublicationRevision(version, generatedAt, "Publication Engine (BlueprintOS)", "Geração automática.") });
}
