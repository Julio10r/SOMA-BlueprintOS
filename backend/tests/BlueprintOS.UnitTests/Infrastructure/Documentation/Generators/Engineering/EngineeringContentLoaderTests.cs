using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class EngineeringContentLoaderTests : IDisposable
{
    private readonly string _aiRoot;
    private readonly string _contentDirectory;

    public EngineeringContentLoaderTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _contentDirectory = Path.Combine(_aiRoot, "content", "engineering");
    }

    [Fact]
    public async Task LoadAsync_Should_Load_All_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Técnica\n\nConteúdo de visão técnica.");
        File.WriteAllText(Path.Combine(_contentDirectory, "02-architecture.md"), "# Arquitetura\n\nConteúdo de arquitetura.");
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Content.Contains("Conteúdo de visão técnica."));
        Assert.Contains(files, f => f.Content.Contains("Conteúdo de arquitetura."));
    }

    [Fact]
    public async Task LoadAsync_Should_Order_Files_Alphabetically_By_Name()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "12-next-steps.md"), "# Próximos Passos");
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Técnica");
        File.WriteAllText(Path.Combine(_contentDirectory, "06-knowledge-engine.md"), "# Conhecimento");
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(
            new[] { "01-overview.md", "06-knowledge-engine.md", "12-next-steps.md" },
            files.Select(f => f.FileName));
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Missing()
    {
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Empty()
    {
        Directory.CreateDirectory(_contentDirectory);
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Ignore_Non_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Técnica");
        File.WriteAllText(Path.Combine(_contentDirectory, "notes.txt"), "Não deve ser carregado.");
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Single(files);
        Assert.Equal("01-overview.md", files[0].FileName);
    }

    [Fact]
    public async Task LoadAsync_Should_Tolerate_Empty_Markdown_File_Without_Throwing()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), string.Empty);
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Single(files);
        Assert.Equal(string.Empty, files[0].Content);
    }

    [Fact]
    public async Task LoadAsync_Should_Preserve_Order_When_Content_Is_Concatenated()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "02-architecture.md"), "Segundo bloco.");
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "Primeiro bloco.");
        File.WriteAllText(Path.Combine(_contentDirectory, "03-system-components.md"), "Terceiro bloco.");
        var loader = new EngineeringContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();
        var concatenated = string.Join("\n", files.Select(f => f.Content));

        Assert.Equal("Primeiro bloco.\nSegundo bloco.\nTerceiro bloco.", concatenated);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
