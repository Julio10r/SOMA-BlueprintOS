using BlueprintOS.Infrastructure.Documentation.Generators.Client;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class FaqGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_State_No_Faq_Registered_Honestly()
    {
        var result = await new FaqGenerator().GenerateAsync();

        Assert.Contains("Nenhuma pergunta frequente registrada", result);
    }
}
