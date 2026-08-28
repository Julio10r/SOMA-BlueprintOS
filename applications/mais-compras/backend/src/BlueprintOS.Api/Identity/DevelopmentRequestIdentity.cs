using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Api.Identity;

/// <summary>Adaptador temporário de identidade, permitido apenas em ambiente de desenvolvimento.</summary>
public sealed class DevelopmentRequestIdentity : ICurrentIdentity
{
    private const string UserIdHeader = "X-Development-User-Id";
    private const string RoleHeader = "X-Development-Role";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;

    public DevelopmentRequestIdentity(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
    }

    public RequestIdentity GetRequired()
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            throw new IdentityUnavailableException("Temporary identity is unavailable outside Development.", true);
        }

        var context = _httpContextAccessor.HttpContext;
        var userIdValue = context?.Request.Headers[UserIdHeader].FirstOrDefault();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new IdentityUnavailableException("A valid development identity is required.", false);
        }

        var role = context!.Request.Headers[RoleHeader].FirstOrDefault();
        return new RequestIdentity(userId, string.IsNullOrWhiteSpace(role) ? "Buyer" : role);
    }
}
