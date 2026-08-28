using BlueprintOS.Infrastructure.Publication;

namespace BlueprintOS.UnitTests.Infrastructure.Publication;

public class PublicationOptionsTests : IDisposable
{
    private readonly string _tempRoot;

    public PublicationOptionsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "BlueprintOSPublicationOptionsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose() => Directory.Delete(_tempRoot, recursive: true);

    private string CreateDir(string relative)
    {
        var path = Path.Combine(_tempRoot, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void ValidateSafePaths_Should_Throw_When_DocsRootPath_Does_Not_Exist()
    {
        var options = new PublicationOptions
        {
            DocsRootPath = Path.Combine(_tempRoot, "missing-docs"),
            DistRootPath = CreateDir("dist"),
        };

        var ex = Assert.Throws<InvalidOperationException>(options.ValidateSafePaths);
        Assert.Contains("DocsRootPath", ex.Message);
    }

    [Fact]
    public void ValidateSafePaths_Should_Throw_When_DistRootPath_Equals_DocsRootPath()
    {
        var docs = CreateDir("docs");
        var options = new PublicationOptions { DocsRootPath = docs, DistRootPath = docs };

        Assert.Throws<InvalidOperationException>(options.ValidateSafePaths);
    }

    [Fact]
    public void ValidateSafePaths_Should_Throw_When_DistRootPath_Is_Inside_DocsRootPath()
    {
        var docs = CreateDir("docs");
        var options = new PublicationOptions
        {
            DocsRootPath = docs,
            DistRootPath = Path.Combine(docs, "dist"),
        };

        Assert.Throws<InvalidOperationException>(options.ValidateSafePaths);
    }

    [Fact]
    public void ValidateSafePaths_Should_Throw_When_DocsRootPath_Is_Inside_DistRootPath()
    {
        var dist = CreateDir("dist");
        var options = new PublicationOptions
        {
            DocsRootPath = Path.Combine(dist, "docs"),
            DistRootPath = dist,
        };
        Directory.CreateDirectory(options.DocsRootPath);

        Assert.Throws<InvalidOperationException>(options.ValidateSafePaths);
    }

    [Fact]
    public void ValidateSafePaths_Should_Throw_When_DistRootPath_Points_Inside_Ai_Directory()
    {
        var docs = CreateDir("docs");
        var aiDirectory = CreateDir(".ai");
        var options = new PublicationOptions
        {
            DocsRootPath = docs,
            DistRootPath = Path.Combine(aiDirectory, "leak"),
        };

        Assert.Throws<InvalidOperationException>(options.ValidateSafePaths);
    }

    [Fact]
    public void ValidateSafePaths_Should_Not_Throw_For_Sibling_Docs_And_Dist()
    {
        var options = new PublicationOptions
        {
            DocsRootPath = CreateDir("docs"),
            DistRootPath = CreateDir("dist"),
        };

        options.ValidateSafePaths();
    }

    [Fact]
    public void Default_DocsRootPath_And_DistRootPath_Should_Not_Reference_Ai_Content()
    {
        var options = new PublicationOptions();

        Assert.Equal("docs", options.DocsRootPath);
        Assert.Equal("dist", options.DistRootPath);
        Assert.DoesNotContain(".ai", options.DocsRootPath);
        Assert.DoesNotContain(".ai", options.DistRootPath);
    }
}
