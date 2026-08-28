using BlueprintOS.Api.Negotiations;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.UnitTests.Api.Negotiations;

public sealed class NegotiationRecommendationRequestValidatorTests
{
    [Fact]
    public void Validate_Should_Accept_Only_Existing_Negotiation_Context_Fields()
    {
        var request = new NegotiationRecommendationRequest(
            Guid.NewGuid(), Guid.NewGuid(), 100m, 5, 90, 1000m, false, true, 2, 1100m, "Normal");

        var errors = NegotiationRecommendationRequestValidator.Validate(request, out var urgency);

        Assert.Empty(errors);
        Assert.Equal(NegotiationUrgencyLevel.Normal, urgency);
    }

    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Request_Without_Calling_The_UseCase()
    {
        var request = new NegotiationRecommendationRequest(Guid.Empty, Guid.Empty, 0m, -1, 101, 0m, false, false, 0, -1m, "Unknown");

        var errors = NegotiationRecommendationRequestValidator.Validate(request, out _);

        Assert.Contains(nameof(request.SupplierId), errors.Keys);
        Assert.Contains(nameof(request.ProductId), errors.Keys);
        Assert.Contains(nameof(request.Urgency), errors.Keys);
    }
}
