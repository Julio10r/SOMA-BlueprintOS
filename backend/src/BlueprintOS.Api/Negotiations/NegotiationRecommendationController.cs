using System.Diagnostics;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Negotiations.Contracts;
using BlueprintOS.Application.Procurement.Negotiations.Models;

namespace BlueprintOS.Api.Negotiations;

/// <summary>Controller minimalista do endpoint consultivo de negociação.</summary>
public static class NegotiationRecommendationController
{
    public static IEndpointRouteBuilder MapNegotiationRecommendation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/negociacoes/recomendacoes", Handle)
            .WithTags("Negociações");
        return endpoints;
    }

    public static IResult Handle(
        NegotiationRecommendationRequest? request,
        HttpContext httpContext,
        INegotiationRecommendationUseCase useCase,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BlueprintOS.Api.Negotiations.NegotiationRecommendationController");
        var requestId = GetRequestId(httpContext);
        httpContext.Response.Headers["X-Request-Id"] = requestId;
        var stopwatch = Stopwatch.StartNew();

        if (request is null)
        {
            return Error(StatusCodes.Status400BadRequest, requestId, "invalid_request", "Request body is required.");
        }

        var errors = NegotiationRecommendationRequestValidator.Validate(request, out var urgencyLevel);
        if (errors.Count > 0)
        {
            return Results.Json(new ApiErrorResponse(requestId, "validation_error", "The request is invalid.", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = useCase.Execute(new NegotiationRecommendationCommand(
                requestId,
                request.SupplierId,
                request.ProductId,
                request.CurrentPrice,
                request.LeadTimeDays,
                request.SlaScore,
                request.PurchaseValue,
                request.IsCriticalItem,
                request.IsRecurringPurchase,
                request.NumberOfSuppliers,
                request.BudgetLimit,
                urgencyLevel));
            var response = NegotiationRecommendationResponse.From(result);

            logger.LogInformation(
                "Negotiation recommendation completed. RequestId: {RequestId}; Strategy: {Strategy}; Outcome: {Outcome}; DurationMs: {DurationMs}",
                requestId,
                response.Strategy,
                "Success",
                stopwatch.ElapsedMilliseconds);

            return Results.Ok(response);
        }
        catch (IdentityUnavailableException exception)
        {
            var statusCode = exception.IsEnvironmentFailure ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest;
            logger.LogWarning("Negotiation recommendation rejected. RequestId: {RequestId}; Outcome: {Outcome}; DurationMs: {DurationMs}", requestId, "IdentityUnavailable", stopwatch.ElapsedMilliseconds);
            return Error(statusCode, requestId, "identity_unavailable", "A valid identity is unavailable for this environment.");
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("Negotiation recommendation could not be evaluated. RequestId: {RequestId}; Outcome: {Outcome}; DurationMs: {DurationMs}", requestId, "DomainFailure", stopwatch.ElapsedMilliseconds);
            return Error(StatusCodes.Status422UnprocessableEntity, requestId, "negotiation_unavailable", "The recommendation could not be generated.");
        }
        catch (Exception)
        {
            logger.LogError("Negotiation recommendation failed unexpectedly. RequestId: {RequestId}; Outcome: {Outcome}; DurationMs: {DurationMs}", requestId, "UnexpectedFailure", stopwatch.ElapsedMilliseconds);
            return Error(StatusCodes.Status500InternalServerError, requestId, "unexpected_error", "An unexpected error occurred.");
        }
    }

    private static IResult Error(int statusCode, string requestId, string code, string message) =>
        Results.Json(new ApiErrorResponse(requestId, code, message, null), statusCode: statusCode);

    private static string GetRequestId(HttpContext context)
    {
        var suppliedRequestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(suppliedRequestId) ? context.TraceIdentifier : suppliedRequestId;
    }
}

public sealed record ApiErrorResponse(string RequestId, string Code, string Message, IReadOnlyDictionary<string, string[]>? Errors);
