using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuctionEngine.Core.DTOs;
using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuctionEngine.Core.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _config = configuration;
    }

    public string GenerateToken(ApplicationUser applicationUser)
    {
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, applicationUser.Id),
                new Claim(ClaimTypes.Email, applicationUser.Email!)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest loginRequest)
    {
        var user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user == null) return AuthResult.Fail();

        var valid = await _userManager.CheckPasswordAsync(user, loginRequest.Password);
        if (!valid) return AuthResult.Fail();

        var token = GenerateToken(user);

        return AuthResult.Ok(token);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest registerRequest)
    {
        var user = new ApplicationUser
        {
            UserName = registerRequest.Email,
            Email = registerRequest.Email
        };

        var result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded) return AuthResult.Fail(result.Errors);

        var token = GenerateToken(user);

        return AuthResult.Ok(token);
    }
}