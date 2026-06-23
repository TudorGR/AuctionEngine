namespace AuctionEngine.Core.DTOs;

public sealed class LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? TwoFactorCode { get; init; }
    public string? TwoFactorRecoveryCode { get; init; }
}
