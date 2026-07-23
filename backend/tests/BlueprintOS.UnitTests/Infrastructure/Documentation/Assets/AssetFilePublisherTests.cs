using BlueprintOS.Core.Documentation.Models.Assets;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Assets;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Assets;

public class AssetFilePublisherTests : IDisposable
{
    private readonly string _assetsRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Fact]
    public async Task PublishAsync_Should_Write_Content_Without_Header_Envelope()
    {
        var publisher = new AssetFilePublisher(Options.Create(new DocumentationOptions { AssetsRootPath = _assetsRoot }));
        var asset = new DocumentationAsset("architecture.mmd", "graph TD\n    A --> B\n");

        await publisher.PublishAsync(asset);

        var filePath = Path.Combine(_assetsRoot, "architecture.mmd");
        Assert.True(File.Exists(filePath));
        Assert.Equal(asset.Content, await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task PublishAsync_Should_Create_Nested_Directories_As_Needed()
    {
        var publisher = new AssetFilePublisher(Options.Create(new DocumentationOptions { AssetsRootPath = _assetsRoot }));
        var asset = new DocumentationAsset("nested/solution-tree.md", "# Árvore da Solução\n");

        await publisher.PublishAsync(asset);

        Assert.True(File.Exists(Path.Combine(_assetsRoot, "nested", "solution-tree.md")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_assetsRoot))
        {
            Directory.Delete(_assetsRoot, recursive: true);
        }
    }
}
