using QRCoder;

namespace BlueprintOS.Infrastructure.Publication.Content;

/// <summary>
/// Gera a imagem PNG de um QR Code a partir de um conteúdo real (ex.: URL do repositório).
/// Usa <c>PngByteQRCode</c> (QRCoder), que não depende de <c>System.Drawing</c>, garantindo
/// funcionamento em qualquer sistema operacional sem bibliotecas nativas adicionais.
/// </summary>
public static class QrCodeImageGenerator
{
    public static byte[] GeneratePng(string content, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(data);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}
