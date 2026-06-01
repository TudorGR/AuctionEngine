using AuctionEngine.Core.Entities;
using AuctionEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/auctions", async (AppDbContext context) =>
{
    var activeAuctions = await context.AuctionItems.Where(a => !a.IsClosed).ToListAsync();
    return Results.Ok(activeAuctions);
});

app.MapPost("/auctions", async (CreateAuctionRequest request, AppDbContext context) =>
{
    var auction = new AuctionItem
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        StartingPrice = request.StartingPrice,
        EndTime = request.EndTime,
        IsClosed = false,
        SellerId = request.SellerId
    };

    context.AuctionItems.Add(auction);
    await context.SaveChangesAsync();

    return Results.Created($"/auctions/{auction.Id}", auction);
});


app.UseHttpsRedirection();


app.Run();


public record CreateAuctionRequest(string Title, string Description, decimal StartingPrice, DateTime EndTime, Guid SellerId);