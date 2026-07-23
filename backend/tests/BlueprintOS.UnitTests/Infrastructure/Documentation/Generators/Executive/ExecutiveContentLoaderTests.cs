using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class ExecutiveContentLoaderTests : IDisposable
{
    private readonly string _aiRoot;
    private readonly string _contentDirectory;

    public ExecutiveContentLoaderTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _contentDirectory = Path.Combine(_aiRoot, "content", "executive");
    }

    [Fact]
    public async Task LoadAsync_Should_Load_All_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-vision.md"), "# Visão\n\nConteúdo de visão.");
        File.WriteAllText(Path.Combine(_contentDirectory, "02-business-problem.md"), "# O Desafio\n\nConteúdo do problema.");
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Content.Contains("Conteúdo de visão."));
        Assert.Contains(files, f => f.Content.Contains("Conteúdo do problema."));
    }

    [Fact]
    public async Task LoadAsync_Should_Order_Files_Alphabetically_By_Name()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "09-next-steps.md"), "# Próximos Passos");
        File.WriteAllText(Path.Combine(_contentDirectory, "01-vision.md"), "# Visão");
        File.WriteAllText(Path.Combine(_contentDirectory, "05-benefits.md"), "# Benefícios");
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(
            new[] { "01-vision.md", "05-benefits.md", "09-next-steps.md" },
            files.Select(f => f.FileName));
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Missing()
    {
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Empty()
    {
        Directory.CreateDirectory(_contentDirectory);
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Ignore_Non_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-vision.md"), "# Visão");
        File.WriteAllText(Path.Combine(_contentDirectory, "notes.txt"), "Não deve ser carregado.");
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Single(files);
        Assert.Equal("01-vision.md", files[0].FileName);
    }

    [Fact]
    public async Task LoadAsync_Should_Tolerate_Empty_Markdown_File_Without_Throwing()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-vision.md"), string.Empty);
        var loader = new ExecutiveContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Single(files);
        Assert.Equal(string.Empty, files[0].Content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
