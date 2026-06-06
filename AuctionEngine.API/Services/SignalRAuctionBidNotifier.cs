using AuctionEngine.API.Hubs;
using AuctionEngine.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AuctionEngine.API.Services;

public sealed class SignalRAuctionBidNotifier : IAuctionBidNotifier
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalRAuctionBidNotifier(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyNewBidAsync(
        Guid auctionId,
        decimal amount,
        string bidderId,
        CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group($"auction-{auctionId}")
            .SendAsync("NewBid", new { auctionId, amount, bidderId }, cancellationToken);
    }
}
