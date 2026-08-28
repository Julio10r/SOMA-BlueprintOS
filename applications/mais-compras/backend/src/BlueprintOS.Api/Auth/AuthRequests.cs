namespace BlueprintOS.Api.Auth;

public sealed record OtpRequestRequest(string? Email);

public sealed record OtpVerifyRequest(string? Email, string? Codigo);
