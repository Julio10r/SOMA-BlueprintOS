using BlueprintOS.Infrastructure.Publication;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Publication;

public class QualityMetricsProviderTests
{
    [Fact]
    public async Task GetMetricsAsync_Should_Report_Failure_Honestly_When_Solution_Is_Missing()
    {
        var options = Options.Create(new PublicationOptions
        {
            SolutionPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.sln"),
            TestsRootPath = Path.Combine(Path.GetTempPath(), $"missing-tests-{Guid.NewGuid():N}"),
        });
        var provider = new QualityMetricsProvider(options);

        var metrics = await provider.GetMetricsAsync();

        Assert.False(metrics.BuildSucceeded);
        Assert.Equal(0, metrics.TestCount);
        Assert.Contains("não encontrada", metrics.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMetricsAsync_Should_Count_Fact_And_Theory_Methods_In_Tests_Root()
    {
        var testsRoot = Path.Combine(Path.GetTempPath(), $"tests-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testsRoot);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(testsRoot, "SampleTests.cs"), """
                public class SampleTests
                {
                    [Fact]
                    public void First() { }

                    [Theory]
                    [InlineData(1)]
                    public void Second(int value) { }
                }
                """);

            var options = Options.Create(new PublicationOptions
            {
                SolutionPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.sln"),
                TestsRootPath = testsRoot,
            });
            var provider = new QualityMetricsProvider(options);

            var metrics = await provider.GetMetricsAsync();

            Assert.Equal(2, metrics.TestCount);
        }
        finally
        {
            Directory.Delete(testsRoot, recursive: true);
        }
    }
}
