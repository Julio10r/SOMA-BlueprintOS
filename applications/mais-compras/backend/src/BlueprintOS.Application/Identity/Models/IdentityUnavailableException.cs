namespace BlueprintOS.Application.Identity.Models;

/// <summary>Indica que não foi possível obter uma identidade adequada para a chamada.</summary>
public sealed class IdentityUnavailableException : Exception
{
    public IdentityUnavailableException(string message, bool isEnvironmentFailure)
        : base(message)
    {
        IsEnvironmentFailure = isEnvironmentFailure;
    }

    public bool IsEnvironmentFailure { get; }
}
