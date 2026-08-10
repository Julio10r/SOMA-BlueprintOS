namespace BlueprintOS.Application.Identity.Models;

/// <summary>Defaults seguros conforme security-design-auth-o1.4.md §3.2 — configuráveis, nunca fixos no código
/// além do default.</summary>
public sealed class AuthSessionOptions
{
    public const string SectionName = "Identity:Session";

    public int AbsoluteExpirationHours { get; set; } = 12;
    public int InactivityTimeoutMinutes { get; set; } = 30;
}
