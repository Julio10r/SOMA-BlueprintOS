namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Tipo de documento, usado para escolher a paleta de cores padrão e, futuramente, templates de
/// identidade visual corporativa distintos por tipo.
/// </summary>
public enum PublicationDocumentClass
{
    /// <summary>Relatório executivo, para diretoria.</summary>
    Executive,

    /// <summary>Guia para clientes.</summary>
    Client,

    /// <summary>Guia técnico, para engenharia.</summary>
    Engineering,
}

/// <summary>
/// Identidade visual de um <see cref="PublicationDocument"/>: paleta de cores (hexadecimal, sem
/// o caractere <c>#</c>) e textos configuráveis de cabeçalho/rodapé, consumidos igualmente pelos
/// renderizadores HTML e PDF (Markdown não tem conceito de cor). Ponto de extensão para
/// templates de identidade visual corporativa completos (tipografia, logotipo obrigatório etc.),
/// que poderão substituir os factory methods abaixo sem alterar o restante do modelo.
/// </summary>
/// <param name="DocumentClass">Tipo de documento que esta paleta representa.</param>
/// <param name="PrimaryColorHex">Cor primária (capa, títulos de seção).</param>
/// <param name="AccentColorHex">Cor de destaque (links, ícones, bordas de ênfase).</param>
/// <param name="MutedColorHex">Cor neutra para texto secundário.</param>
/// <param name="BorderColorHex">Cor de bordas e divisores.</param>
/// <param name="HeaderText">Texto customizado de cabeçalho; quando nulo, os renderizadores usam o padrão (marca + título do documento).</param>
/// <param name="FooterText">Texto customizado de rodapé; quando nulo, os renderizadores usam o padrão (marca + versão + data de geração).</param>
public sealed record PublicationTheme(
    PublicationDocumentClass DocumentClass,
    string PrimaryColorHex,
    string AccentColorHex,
    string MutedColorHex,
    string BorderColorHex,
    string? HeaderText = null,
    string? FooterText = null)
{
    /// <summary>Paleta padrão para relatórios executivos (azul institucional).</summary>
    public static PublicationTheme ForExecutive() => new(
        PublicationDocumentClass.Executive, "16324F", "2E5C8A", "5B6A7A", "DFE4EA");

    /// <summary>Paleta padrão para guias de cliente (verde-azulado, mais acolhedor).</summary>
    public static PublicationTheme ForClient() => new(
        PublicationDocumentClass.Client, "0F4C4C", "1E8A7A", "5B6A6A", "DDE9E6");

    /// <summary>Paleta padrão para guias de engenharia (grafite/roxo, mais técnico).</summary>
    public static PublicationTheme ForEngineering() => new(
        PublicationDocumentClass.Engineering, "2B2140", "5B4B8A", "6A6478", "E3DFEA");
}
