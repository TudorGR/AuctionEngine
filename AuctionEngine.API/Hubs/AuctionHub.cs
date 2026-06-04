using AuctionEngine.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AuctionEngine.API.Hubs;

public class AuctionHub : Hub
{
    private readonly AppDbContext _context;

    public AuctionHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task JoinAuction(Guid auctionId)
    {
        var exists = await _context.AuctionItems
            .AnyAsync(a => a.Id == auctionId);

        if (!exists)
            throw new HubException("Auction does not exist");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"auction-{auctionId}");
    }
}