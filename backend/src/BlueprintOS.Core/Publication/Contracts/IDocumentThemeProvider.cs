using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Único ponto de acesso à identidade visual oficial da plataforma (AZZAS 2154 - GDT Design
/// System), lida de <c>docs/design-system/</c>. Fornece ao Publication Engine cores, tipografia
/// e a folha de estilo oficial para embutir no HTML — nenhum Publisher ou renderizador lê
/// arquivos do Design System diretamente, nem hardcoda cor ou fonte.
/// </summary>
public interface IDocumentThemeProvider
{
    /// <summary>
    /// Paleta de cores oficial, extraída de <c>colors_and_type.css</c>. Se o Design System não
    /// estiver presente no repositório, retorna uma paleta de segurança (fallback) idêntica aos
    /// valores oficiais documentados, para que a publicação nunca falhe por sua ausência.
    /// </summary>
    DocumentPalette GetPalette();

    /// <summary>
    /// Tipografia oficial (fontes de destaque, corpo e monoespaçada), extraída de
    /// <c>colors_and_type.css</c>/<c>fonts.css</c>, com o mesmo fallback de segurança.
    /// </summary>
    DocumentTypography GetTypography();

    /// <summary>
    /// Folha de estilo completa a embutir no HTML gerado: o conteúdo oficial de
    /// <c>fonts.css</c> e <c>colors_and_type.css</c> (tokens), somada a uma camada estrutural de
    /// layout de documento que referencia apenas essas variáveis — nunca redefine cor ou fonte.
    /// Com o mesmo fallback de segurança quando o Design System está ausente.
    /// </summary>
    string GetStylesheet();
}
