namespace AuctionEngine.Core.Interfaces;

public interface IHighestBidCache
{
    Task<decimal?> GetHighestBidAsync(Guid auctionId);

    Task SetHighestBidAsync(
        Guid auctionId,
        decimal amount);
}