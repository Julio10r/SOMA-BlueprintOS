namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Tipo de um bloco de conteúdo estruturado dentro de uma <see cref="PublicationSection"/>.
/// </summary>
public enum ContentBlockKind
{
    /// <summary>Subtítulo dentro do corpo da seção.</summary>
    Heading,

    /// <summary>Parágrafo de texto corrido.</summary>
    Paragraph,

    /// <summary>Lista não ordenada de itens.</summary>
    BulletList,

    /// <summary>Tabela com cabeçalho e linhas.</summary>
    Table,

    /// <summary>Bloco de código pré-formatado.</summary>
    CodeBlock,

    /// <summary>Imagem embutida, referenciando um asset de <see cref="PublicationAssets"/> por id.</summary>
    Image,
}

/// <summary>
/// Bloco de conteúdo estruturado — a unidade mínima do modelo comum (ViewModel) de um
/// documento de publicação. Todo o conteúdo de um <see cref="PublicationDocument"/> é
/// representado por uma sequência de <see cref="ContentBlock"/>, produzida uma única vez a
/// partir das fontes reais do projeto. Cada <see cref="Core.Publication.Contracts.IContentRenderer"/>
/// (Markdown, HTML, PDF e futuros formatos como Word, PowerPoint ou site estático) consome
/// exatamente os mesmos blocos, sem reprocessar ou reinterpretar o conteúdo original — isso
/// garante uma única fonte de verdade e elimina duplicação de lógica de interpretação entre
/// formatos.
/// </summary>
/// <param name="Kind">Tipo do bloco.</param>
/// <param name="Text">Texto do bloco, para <see cref="ContentBlockKind.Heading"/>, <see cref="ContentBlockKind.Paragraph"/> e <see cref="ContentBlockKind.CodeBlock"/>. Pode conter ênfase inline (<c>**negrito**</c>, <c>`código`</c>), interpretada por cada renderizador via <c>InlineSpan</c>.</param>
/// <param name="Level">Nível do subtítulo, quando <see cref="Kind"/> é <see cref="ContentBlockKind.Heading"/>.</param>
/// <param name="Items">Itens da lista, quando <see cref="Kind"/> é <see cref="ContentBlockKind.BulletList"/>.</param>
/// <param name="TableHeader">Células do cabeçalho, quando <see cref="Kind"/> é <see cref="ContentBlockKind.Table"/>.</param>
/// <param name="TableRows">Linhas da tabela (cada uma com as mesmas colunas do cabeçalho), quando <see cref="Kind"/> é <see cref="ContentBlockKind.Table"/>.</param>
/// <param name="AssetId">Id do asset (em <see cref="PublicationAssets"/>) referenciado, quando <see cref="Kind"/> é <see cref="ContentBlockKind.Image"/>.</param>
/// <param name="Caption">Legenda opcional, aplicável a <see cref="ContentBlockKind.Image"/> e <see cref="ContentBlockKind.Table"/>; ponto de extensão para a futura numeração automática de figuras/tabelas.</param>
public sealed record ContentBlock(
    ContentBlockKind Kind,
    string? Text = null,
    int Level = 0,
    IReadOnlyList<string>? Items = null,
    IReadOnlyList<string>? TableHeader = null,
    IReadOnlyList<IReadOnlyList<string>>? TableRows = null,
    string? AssetId = null,
    string? Caption = null)
{
    /// <summary>Cria um bloco de subtítulo.</summary>
    public static ContentBlock Heading(string text, int level) => new(ContentBlockKind.Heading, Text: text, Level: level);

    /// <summary>Cria um bloco de parágrafo.</summary>
    public static ContentBlock Paragraph(string text) => new(ContentBlockKind.Paragraph, Text: text);

    /// <summary>Cria um bloco de lista não ordenada.</summary>
    public static ContentBlock BulletList(IReadOnlyList<string> items) => new(ContentBlockKind.BulletList, Items: items);

    /// <summary>Cria um bloco de tabela.</summary>
    public static ContentBlock Table(IReadOnlyList<string> header, IReadOnlyList<IReadOnlyList<string>> rows, string? caption = null) =>
        new(ContentBlockKind.Table, TableHeader: header, TableRows: rows, Caption: caption);

    /// <summary>Cria um bloco de código.</summary>
    public static ContentBlock CodeBlock(string code) => new(ContentBlockKind.CodeBlock, Text: code);

    /// <summary>Cria um bloco de imagem, referenciando um asset por id.</summary>
    public static ContentBlock Image(string assetId, string? caption = null) =>
        new(ContentBlockKind.Image, AssetId: assetId, Caption: caption);
}
