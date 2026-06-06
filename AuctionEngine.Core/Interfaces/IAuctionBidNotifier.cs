namespace AuctionEngine.Core.Interfaces;

public interface IAuctionBidNotifier
{
    Task NotifyNewBidAsync(
        Guid auctionId,
        decimal amount,
        string bidderId,
        CancellationToken cancellationToken);
}
