using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class ClientContentLoaderTests : IDisposable
{
    private readonly string _aiRoot;
    private readonly string _contentDirectory;

    public ClientContentLoaderTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _contentDirectory = Path.Combine(_aiRoot, "content", "client");
    }

    [Fact]
    public async Task LoadAsync_Should_Load_All_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Geral\n\nConteúdo de visão geral.");
        File.WriteAllText(Path.Combine(_contentDirectory, "02-business-value.md"), "# Valor para o Negócio\n\nConteúdo de valor.");
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Content.Contains("Conteúdo de visão geral."));
        Assert.Contains(files, f => f.Content.Contains("Conteúdo de valor."));
    }

    [Fact]
    public async Task LoadAsync_Should_Order_Files_Alphabetically_By_Name()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "09-next-steps.md"), "# Próximos Passos");
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Geral");
        File.WriteAllText(Path.Combine(_contentDirectory, "06-security.md"), "# Segurança");
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Equal(
            new[] { "01-overview.md", "06-security.md", "09-next-steps.md" },
            files.Select(f => f.FileName));
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Missing()
    {
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Empty_List_When_Folder_Is_Empty()
    {
        Directory.CreateDirectory(_contentDirectory);
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadAsync_Should_Ignore_Non_Markdown_Files()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), "# Visão Geral");
        File.WriteAllText(Path.Combine(_contentDirectory, "notes.txt"), "Não deve ser carregado.");
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var files = await loader.LoadAsync();

        Assert.Single(files);
        Assert.Equal("01-overview.md", files[0].FileName);
    }

    [Fact]
    public async Task LoadAsync_Should_Tolerate_Empty_Markdown_File_Without_Throwing()
    {
        Directory.CreateDirectory(_contentDirectory);
        File.WriteAllText(Path.Combine(_contentDirectory, "01-overview.md"), string.Empty);
        var loader = new ClientContentLoader(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

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
