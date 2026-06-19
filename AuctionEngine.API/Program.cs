using System.Text;
using AuctionEngine.API.Hubs;
using AuctionEngine.API.Services;
using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Interfaces;
using AuctionEngine.Core.Services;
using AuctionEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AuctionEngine.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"]!))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<IHighestBidCache, HighestBidCache>();
builder.Services.AddScoped<IAuctionBidRepository, AuctionBidRepository>();
builder.Services.AddScoped<IAuctionBidNotifier, SignalRAuctionBidNotifier>();
builder.Services.AddScoped<AuctionBidService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddSignalR();
builder.Services.AddHostedService<AuctionCLoserService>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHub<AuctionHub>("/hubs/auction");

app.MapPost("/register", async (UserManager<ApplicationUser> UserManager, RegisterRequest request) =>
{
    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email
    };

    var result = await UserManager.CreateAsync(user, request.Password);

    if (!result.Succeeded) return Results.BadRequest(result.Errors);

    return Results.Ok();
});


app.MapPost("/login", async (UserManager<ApplicationUser> UserManager, LoginRequest request) =>
{
    var user = await UserManager.FindByEmailAsync(request.Email);

    if (user == null) return Results.Unauthorized();

    var valid = await UserManager.CheckPasswordAsync(user, request.Password);

    if (!valid) return Results.Unauthorized();

    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email!)
    }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"],
        Audience = builder.Configuration["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = tokenString });
});

app.MapGet("/auctions", async (AppDbContext context) =>
{
    var activeAuctions = await context.AuctionItems.Where(a => !a.IsClosed).ToListAsync();
    return Results.Ok(activeAuctions);
});

app.MapGet("/auctions/{id}", async (Guid id, AppDbContext context) =>
{
    var auction = await context.AuctionItems.FirstOrDefaultAsync(a => a.Id == id);
    return Results.Ok(auction);
});

app.MapPost("/auctions", async (CreateAuctionRequest request, IHighestBidCache highestBidCache, AppDbContext context, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (userId == null) return Results.Unauthorized();

    var auction = new AuctionItem
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        StartingPrice = request.StartingPrice,
        CurrentHighestBid = request.StartingPrice,
        EndTime = request.EndTime.ToUniversalTime(),
        IsClosed = false,
        SellerId = userId
    };

    context.AuctionItems.Add(auction);
    await context.SaveChangesAsync();
    await highestBidCache.SetHighestBidAsync(auction.Id, auction.CurrentHighestBid);

    return Results.Created($"/auctions/{auction.Id}", auction);
}).RequireAuthorization();

app.MapPost("/auctions/{id}/bids", async (Guid id, PlaceBidRequest request, AuctionBidService bidService, HttpContext httpContext) =>
{

    var userId = httpContext.User
        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
        ?.Value;

    if (userId is null)
        return Results.Unauthorized();

    var result = await bidService.PlaceBidAsync(id, userId, request.Amount, httpContext.RequestAborted);

    return result.Status switch
    {
        BidPlacementStatus.Created => Results.Created($"/auctions/{id}/bids/{result.BidId}",
            new { id = result.BidId, amount = result.Amount, timestamp = result.Timestamp }),
        BidPlacementStatus.NotFound => Results.NotFound(),
        BidPlacementStatus.BadRequest => Results.BadRequest(result.ErrorMessage),
        BidPlacementStatus.Conflict => Results.Conflict(result.ErrorMessage),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
}).RequireAuthorization();


app.UseHttpsRedirection();

app.UseStaticFiles();

app.Run();


public record CreateAuctionRequest(string Title, string Description, decimal StartingPrice, DateTime EndTime);
public record PlaceBidRequest(decimal Amount);
