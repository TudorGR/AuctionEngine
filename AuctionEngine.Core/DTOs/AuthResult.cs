namespace AuctionEngine.Core.DTOs;

using Microsoft.AspNetCore.Identity;

public class AuthResult
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public string? Error { get; init; }

    public static AuthResult Ok(string token)
        => new() { Success = true, Token = token };

    public static AuthResult Fail(string error = "Unauthorized")
        => new() { Success = false, Error = error };

    public static AuthResult Fail(IEnumerable<IdentityError> errors)
    => new() { Success = false, Error = string.Join(", ", errors.Select(e => e.Description)) };
}