namespace AuctionEngine.Core.DTOs;

using System.Diagnostics.CodeAnalysis;

public class RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }

    [SetsRequiredMembers]
    public RegisterRequest(string v1, string v2)
    {
        Email = v1;
        Password = v2;
    }

}