namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Tipo de documento, usado para eventual customização de cabeçalho/rodapé por audiência. A
/// paleta e a tipografia são sempre as mesmas para os três tipos — a identidade visual do
/// BlueprintOS é unificada, vinda do Design System oficial (AZZAS 2154 - GDT Design System).
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
/// Identidade visual de um <see cref="PublicationDocument"/>: paleta, tipografia e a folha de
/// estilo oficial (para o HTML), todas resolvidas pelo <c>IDocumentThemeProvider</c> a partir do
/// Design System oficial — nenhum valor é hardcoded neste tipo nem por quem o constrói.
/// </summary>
/// <param name="DocumentClass">Tipo de documento que este tema representa.</param>
/// <param name="Palette">Paleta de cores oficial.</param>
/// <param name="Typography">Tipografia oficial.</param>
/// <param name="Stylesheet">CSS oficial do Design System (mais camada estrutural baseada em tokens), embutido verbatim pelo <c>HtmlRenderer</c>.</param>
/// <param name="HeaderText">Texto customizado de cabeçalho; quando nulo, os renderizadores usam o padrão (marca + título do documento).</param>
/// <param name="FooterText">Texto customizado de rodapé; quando nulo, os renderizadores usam o padrão (marca + versão + data de geração).</param>
public sealed record PublicationTheme(
    PublicationDocumentClass DocumentClass,
    DocumentPalette Palette,
    DocumentTypography Typography,
    string Stylesheet,
    string? HeaderText = null,
    string? FooterText = null);
