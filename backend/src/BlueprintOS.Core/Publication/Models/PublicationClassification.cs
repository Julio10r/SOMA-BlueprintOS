namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Classificação de confidencialidade de um documento publicado. Serve hoje apenas como metadado
/// exibido na capa; é o ponto de extensão para a futura marca d'água (<c>Draft</c>/<c>Internal</c>/
/// <c>Confidential</c>) mencionada no roadmap do Publication Engine.
/// </summary>
public enum PublicationClassification
{
    /// <summary>Rascunho, ainda não revisado.</summary>
    Draft,

    /// <summary>Uso interno da organização.</summary>
    Internal,

    /// <summary>Confidencial, distribuição restrita.</summary>
    Confidential,

    /// <summary>Público, sem restrição de distribuição.</summary>
    Public,
}
