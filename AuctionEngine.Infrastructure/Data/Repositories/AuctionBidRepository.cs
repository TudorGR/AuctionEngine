using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Interfaces;
using AuctionEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionEngine.Infrastructure.Data.Repositories;

public sealed class AuctionBidRepository : IAuctionBidRepository
{
    private readonly AppDbContext _context;

    public AuctionBidRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AuctionItem?> GetAuctionAsync(Guid auctionId, CancellationToken cancellationToken) =>
        _context.AuctionItems.FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

    public Task AddBidAsync(Bid bid, CancellationToken cancellationToken)
    {
        _context.Bids.Add(bid);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
