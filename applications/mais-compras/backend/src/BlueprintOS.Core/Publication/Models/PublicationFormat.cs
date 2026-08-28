namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Formatos de saída suportados pelo Publication Engine para cada documento publicado.
/// </summary>
public enum PublicationFormat
{
    /// <summary>Documento Markdown, para versionamento no Git.</summary>
    Markdown,

    /// <summary>Documento HTML, com aparência moderna e responsiva.</summary>
    Html,

    /// <summary>Documento PDF, pronto para impressão e apresentação.</summary>
    Pdf,
}
