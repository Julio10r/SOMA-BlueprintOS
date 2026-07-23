using BlueprintOS.Infrastructure.Knowledge;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Knowledge;

public class MarkdownKnowledgeProviderTests : IDisposable
{
    private readonly string _directory;

    public MarkdownKnowledgeProviderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task LoadDocumentsAsync_Should_Load_Markdown_Files_With_Title_From_Heading()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "doc1.md"), "# Título do Documento\n\nConteúdo de teste.");
        await File.WriteAllTextAsync(Path.Combine(_directory, "doc2.md"), "Conteúdo sem título.");
        await File.WriteAllTextAsync(Path.Combine(_directory, "ignore.txt"), "não deve ser carregado");

        var provider = new MarkdownKnowledgeProvider(Options.Create(new KnowledgeOptions { DirectoryPath = _directory }));

        var documents = await provider.LoadDocumentsAsync();

        Assert.Equal(2, documents.Count);
        Assert.Contains(documents, d => d.Title == "Título do Documento");
        Assert.Contains(documents, d => d.Title == "doc2");
    }

    [Fact]
    public async Task LoadDocumentsAsync_Should_Return_Empty_When_Directory_Does_Not_Exist()
    {
        var provider = new MarkdownKnowledgeProvider(
            Options.Create(new KnowledgeOptions { DirectoryPath = Path.Combine(_directory, "inexistente") }));

        var documents = await provider.LoadDocumentsAsync();

        Assert.Empty(documents);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
