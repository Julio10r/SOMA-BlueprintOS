using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Content;

public class QrCodeImageGeneratorTests
{
    [Fact]
    public void GeneratePng_Should_Produce_A_Valid_Png_For_Real_Content()
    {
        var bytes = QrCodeImageGenerator.GeneratePng("https://github.com/Julio10r/SOMA-BlueprintOS");

        Assert.NotEmpty(bytes);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }
}
