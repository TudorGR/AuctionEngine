using System.Text;
using AuctionEngine.Core.Entities;
using AuctionEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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


app.MapPost("/login", async (UserManager<ApplicationUser> UserManager, RegisterRequest request) =>
{
    var user = await UserManager.FindByEmailAsync(request.Email);

    if (user == null) return Results.Unauthorized();

    var valid = await UserManager.CheckPasswordAsync(user, request.Password);

    if (!valid) return Results.Unauthorized();

    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
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

app.MapPost("/auctions", async (CreateAuctionRequest request, AppDbContext context, HttpContext httpContext) =>
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
        EndTime = request.EndTime,
        IsClosed = false,
        SellerId = userId
    };

    context.AuctionItems.Add(auction);
    await context.SaveChangesAsync();

    return Results.Created($"/auctions/{auction.Id}", auction);
}).RequireAuthorization();

app.MapPost("/auctions/{id}/bids", async (Guid id, PlaceBidRequest request, AppDbContext context, HttpContext httpContext) =>
{
    var userId = httpContext.User
        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
        ?.Value;

    if (userId is null)
        return Results.Unauthorized();

    var auction = await context.AuctionItems.FirstOrDefaultAsync(a => a.Id == id);

    if (auction is null)
        return Results.NotFound();

    if (auction.IsClosed)
        return Results.BadRequest("Auction is closed");

    if (auction.EndTime <= DateTime.UtcNow)
        return Results.BadRequest("Auction has expired");

    if (auction.SellerId == userId)
        return Results.BadRequest("You cannot bid on your own auction.");

    if (request.Amount <= 0) return Results.BadRequest("Amount must be > 0");

    if (request.Amount <= auction.CurrentHighestBid)
        return Results.BadRequest($"Bid must be greater than {auction.CurrentHighestBid}");

    var bid = new Bid
    {
        Id = Guid.NewGuid(),
        Amount = request.Amount,
        Timestamp = DateTime.UtcNow,
        AuctionItemId = id,
        BidderId = userId
    };
    context.Bids.Add(bid);
    auction.CurrentHighestBid = request.Amount;
    try
    {
        await context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Results.Conflict("Another bid was placed simultaneously. Please try again.");
    }

    return Results.Created($"/auctions/{id}/bids/{bid.Id}",
        new { id = bid.Id, amount = bid.Amount, timestamp = bid.Timestamp });
}).RequireAuthorization();


app.UseHttpsRedirection();


app.Run();


public record CreateAuctionRequest(string Title, string Description, decimal StartingPrice, DateTime EndTime);
public record PlaceBidRequest(decimal Amount);