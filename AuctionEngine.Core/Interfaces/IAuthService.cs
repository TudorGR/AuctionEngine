using AuctionEngine.Core.DTOs;
using AuctionEngine.Core.Entities;

namespace AuctionEngine.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest registerRequest);
    Task<AuthResult> LoginAsync(LoginRequest loginRequest);
    string GenerateToken(ApplicationUser applicationUser);
}