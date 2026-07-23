using BlueprintOS.Core.Documentation.Contracts.Assets;
using BlueprintOS.Core.Documentation.Models.Assets;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Assets;

/// <summary>
/// Implementação de <see cref="IAssetPublisher"/> que escreve o ativo em disco, sem envelope
/// de cabeçalho, sob a raiz de ativos configurada (<see cref="DocumentationOptions.AssetsRootPath"/>),
/// criando diretórios conforme necessário.
/// </summary>
public sealed class AssetFilePublisher : IAssetPublisher
{
    private readonly string _assetsRootPath;

    public AssetFilePublisher(IOptions<DocumentationOptions> options)
    {
        _assetsRootPath = options.Value.AssetsRootPath;
    }

    /// <inheritdoc />
    public async Task PublishAsync(DocumentationAsset asset, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_assetsRootPath, asset.RelativePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, asset.Content, cancellationToken);
    }
}
