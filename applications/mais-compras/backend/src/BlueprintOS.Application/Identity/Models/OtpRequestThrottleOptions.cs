namespace BlueprintOS.Application.Identity.Models;

/// <summary>Defaults conforme security-design-auth-o1.4.md §3.1 — configuráveis, nunca fixos além do
/// default (O1.4.2.1, Achado A).</summary>
public sealed class OtpRequestThrottleOptions
{
    public const string SectionName = "Identity:Otp:Throttle";

    public int MaxSolicitacoesPorJanela { get; set; } = 3;
    public int JanelaMinutos { get; set; } = 15;
    public int CooldownSegundos { get; set; } = 60;
}
