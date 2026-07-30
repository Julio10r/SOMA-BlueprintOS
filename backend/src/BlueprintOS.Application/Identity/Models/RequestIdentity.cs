namespace BlueprintOS.Application.Identity.Models;

/// <summary>Identidade mínima necessária para associar uma chamada a seu solicitante.</summary>
public sealed record RequestIdentity(Guid UserId, string Role);
