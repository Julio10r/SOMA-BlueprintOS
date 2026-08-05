using BlueprintOS.Infrastructure.Publication.Docs;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Docs;

public class DocsDiscoveryServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public DocsDiscoveryServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "BlueprintOSDocsDiscoveryTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose() => Directory.Delete(_tempDirectory, recursive: true);

    private static DocsDiscoveryService CreateService(params string[] excluded) =>
        new(new HashSet<string>(excluded, StringComparer.OrdinalIgnoreCase));

    private void WriteFile(string relativePath, string content = "# Título\n\nConteúdo.")
    {
        var fullPath = Path.Combine(_tempDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    [Fact]
    public void Discover_Should_Find_Markdown_Files_Recursively_Across_Nested_Domains()
    {
        WriteFile("architecture/Architecture.md");
        WriteFile("backend/procurement/Procurement.md");
        WriteFile("backend/integration/Integration.md");

        var documents = CreateService().Discover(_tempDirectory);

        Assert.Equal(3, documents.Count);
        Assert.Contains(documents, d => d.RelativePath == "architecture/Architecture.md" && d.Category == "architecture");
        Assert.Contains(documents, d => d.RelativePath == "backend/procurement/Procurement.md" && d.Category == "backend/procurement");
    }

    [Fact]
    public void Discover_Should_Ignore_Non_Markdown_Files()
    {
        WriteFile("architecture/Architecture.md");
        WriteFile("assets/diagram.mmd", "graph TD; A-->B;");
        WriteFile("assets/notes.txt", "not markdown");

        var documents = CreateService().Discover(_tempDirectory);

        Assert.Single(documents);
        Assert.Equal("architecture/Architecture.md", documents[0].RelativePath);
    }

    [Fact]
    public void Discover_Should_Return_Documents_In_Deterministic_Ordinal_Order()
    {
        WriteFile("zeta/Zeta.md");
        WriteFile("alpha/Alpha.md");
        WriteFile("Beta.md");

        var first = CreateService().Discover(_tempDirectory);
        var second = CreateService().Discover(_tempDirectory);

        var expectedOrder = new[] { "Beta.md", "alpha/Alpha.md", "zeta/Zeta.md" };
        Assert.Equal(expectedOrder, first.Select(d => d.RelativePath));
        Assert.Equal(first.Select(d => d.RelativePath), second.Select(d => d.RelativePath));
    }

    [Fact]
    public void Discover_Should_Preserve_Relative_Directory_As_Category_And_Filename_As_Slug()
    {
        WriteFile("backend/procurement/FornecedorCnpjEnrichment.md");

        var document = Assert.Single(CreateService().Discover(_tempDirectory));

        Assert.Equal("backend/procurement", document.Category);
        Assert.Equal("FornecedorCnpjEnrichment", document.Slug);
    }

    [Fact]
    public void Discover_Should_Use_Empty_Category_For_Root_Level_Documents()
    {
        WriteFile("Product Blueprint.md");

        var document = Assert.Single(CreateService().Discover(_tempDirectory));

        Assert.Equal(string.Empty, document.Category);
    }

    [Fact]
    public void Discover_Should_Skip_Configured_Excluded_Top_Level_Directories()
    {
        WriteFile("architecture/Architecture.md");
        WriteFile("audits/OldAudit.md");
        WriteFile("demo/Demo.md");

        var documents = CreateService("audits", "demo").Discover(_tempDirectory);

        Assert.Single(documents);
        Assert.Equal("architecture/Architecture.md", documents[0].RelativePath);
    }

    [Fact]
    public void Discover_Should_Not_Exclude_Root_File_Whose_Name_Matches_An_Excluded_Directory()
    {
        WriteFile("audits.md");

        var documents = CreateService("audits").Discover(_tempDirectory);

        Assert.Single(documents);
    }

    [Fact]
    public void Discover_Should_Throw_When_Source_Directory_Does_Not_Exist()
    {
        var missingPath = Path.Combine(_tempDirectory, "does-not-exist");

        Assert.Throws<DirectoryNotFoundException>(() => CreateService().Discover(missingPath));
    }

    [Fact]
    public void Discover_Should_Not_Depend_On_Audience_Named_Directories()
    {
        WriteFile("architecture/Architecture.md");
        WriteFile("whatever-domain/Custom.md");

        var documents = CreateService().Discover(_tempDirectory);

        Assert.Equal(2, documents.Count);
        Assert.DoesNotContain(documents, d =>
            d.Category is "executive" or "client" or "engineering");
    }
}
