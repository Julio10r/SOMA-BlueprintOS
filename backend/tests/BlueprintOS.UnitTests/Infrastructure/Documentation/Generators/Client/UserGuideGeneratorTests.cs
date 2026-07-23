using BlueprintOS.Infrastructure.Documentation.Generators.Client;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class UserGuideGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Honestly_State_Frontend_Not_Available()
    {
        var result = await new UserGuideGenerator().GenerateAsync();

        Assert.Contains("não possui uma interface de usuário", result);
    }
}
