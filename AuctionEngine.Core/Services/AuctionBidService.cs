using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuctionEngine.Core.Services;

public sealed class AuctionBidService
{
    private readonly IAuctionBidRepository _repository;
    private readonly IHighestBidCache _highestBidCache;
    private readonly IAuctionBidNotifier _notifier;

    public AuctionBidService(
        IAuctionBidRepository repository,
        IHighestBidCache highestBidCache,
        IAuctionBidNotifier notifier)
    {
        _repository = repository;
        _highestBidCache = highestBidCache;
        _notifier = notifier;
    }

    public async Task<BidPlacementResult> PlaceBidAsync(
        Guid auctionId,
        string userId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var auction = await _repository.GetAuctionAsync(auctionId, cancellationToken);

        if (auction is null)
            return BidPlacementResult.NotFound();

        var highestBid = await _highestBidCache.GetHighestBidAsync(auctionId) ?? auction.CurrentHighestBid;

        try
        {
            BidValidationService.ValidateBid(auction, amount, highestBid, userId);
        }
        catch (InvalidOperationException ex)
        {
            return BidPlacementResult.BadRequest(ex.Message);
        }

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Timestamp = DateTime.UtcNow,
            AuctionItemId = auctionId,
            BidderId = userId
        };

        auction.CurrentHighestBid = amount;

        await _repository.AddBidAsync(bid, cancellationToken);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BidPlacementResult.Conflict("Another bid was placed simultaneously. Please try again.");
        }

        await _highestBidCache.SetHighestBidAsync(auctionId, amount);
        await _notifier.NotifyNewBidAsync(auctionId, amount, userId, cancellationToken);

        return BidPlacementResult.Created(bid);
    }
}

public enum BidPlacementStatus
{
    Created,
    NotFound,
    BadRequest,
    Conflict
}

public sealed record BidPlacementResult(
    BidPlacementStatus Status,
    string? ErrorMessage = null,
    Guid? BidId = null,
    decimal? Amount = null,
    DateTime? Timestamp = null)
{
    public static BidPlacementResult Created(Bid bid) =>
        new(BidPlacementStatus.Created, null, bid.Id, bid.Amount, bid.Timestamp);

    public static BidPlacementResult NotFound() =>
        new(BidPlacementStatus.NotFound);

    public static BidPlacementResult BadRequest(string errorMessage) =>
        new(BidPlacementStatus.BadRequest, errorMessage);

    public static BidPlacementResult Conflict(string errorMessage) =>
        new(BidPlacementStatus.Conflict, errorMessage);
}
