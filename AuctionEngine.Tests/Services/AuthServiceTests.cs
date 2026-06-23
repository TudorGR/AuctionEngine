using AuctionEngine.Core.DTOs;
using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AuctionEngine.Tests.Services;

public class AuthServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task RegisterAsync_Fails_When_Email_Already_Exists()
    {
        var userManager = CreateUserManager();

        userManager
       .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
       .ReturnsAsync(IdentityResult.Failed(
           new IdentityError { Description = "Email already taken" }));

        var config = new Mock<IConfiguration>();
        var service = new AuthService(userManager.Object, config.Object);
        var request = new RegisterRequest("test@test.com", "Password123!");

        var result = await service.RegisterAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Email already taken", result.Error);
    }
}