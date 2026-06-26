using System.Data.Common;
using AuctionEngine.API.Hubs;
using AuctionEngine.Core.Entities;
using AuctionEngine.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AuctionEngine.API.Services;

public class AuctionCLoserService : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AuctionHub> _hubContext;
    private readonly ILogger<AuctionCLoserService> _logger;

    public AuctionCLoserService(
        IServiceScopeFactory scopeFactory,
        IHubContext<AuctionHub> hubContext,
        ILogger<AuctionCLoserService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CloseExpiredAuctionsAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while closing expired auctions.");
            }
        }
    }

    public async Task CloseExpiredAuctionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var expiredAuctionIds = await db.AuctionItems
            .AsNoTracking()
            .Where(a => !a.IsClosed && a.EndTime <= now)
            .OrderBy(a => a.EndTime)
            .Select(a => a.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (expiredAuctionIds.Count == 0) return;

        var closedEvents = new List<AuctionClosedEvent>();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var auctions = await db.AuctionItems.Where(a => expiredAuctionIds.Contains(a.Id)).ToListAsync(ct);

        foreach (var auction in auctions)
        {
            if (auction.IsClosed)
                continue;

            var highestBid = await db.Bids
                .Where(b => b.AuctionItemId == auction.Id)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.Timestamp)
                .FirstOrDefaultAsync(ct);

            var winningBidAmount = highestBid?.Amount ?? auction.CurrentHighestBid;
            var winningBidderId = highestBid?.BidderId;

            if (winningBidderId is not null)
            {
                db.Invoices.Add(new Invoice
                {
                    Id = Guid.NewGuid(),
                    AuctionId = auction.Id,
                    WinnerId = winningBidderId,
                    Amount = winningBidAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            auction.IsClosed = true;
            auction.CurrentHighestBid = winningBidAmount;

            closedEvents.Add(new AuctionClosedEvent(
                auction.Id,
                winningBidAmount,
                winningBidderId,
                auction.EndTime));
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        foreach (var evt in closedEvents)
        {
            await _hubContext.Clients
                .Group($"auction-{evt.AuctionId}")
                .SendAsync("AuctionClosed", evt, ct);

            _logger.LogInformation(
                "Auction {AuctionId} closed. WinnerBidderId={WinnerBidderId}, Amount={Amount}",
                evt.AuctionId,
                evt.WinnerBidderId,
                evt.WinningBidAmount);
        }
    }

}

public record AuctionClosedEvent(
    Guid AuctionId,
    decimal WinningBidAmount,
    string? WinnerBidderId,
    DateTime EndTime);