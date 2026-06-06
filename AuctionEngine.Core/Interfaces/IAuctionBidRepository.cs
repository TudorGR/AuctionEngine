using AuctionEngine.Core.Entities;

namespace AuctionEngine.Core.Interfaces;

public interface IAuctionBidRepository
{
    Task<AuctionItem?> GetAuctionAsync(Guid auctionId, CancellationToken cancellationToken);

    Task AddBidAsync(Bid bid, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
