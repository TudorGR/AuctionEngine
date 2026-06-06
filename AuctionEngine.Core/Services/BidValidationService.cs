using AuctionEngine.Core.Entities;

namespace AuctionEngine.Core.Services;

public static class BidValidationService
{
    public static void ValidateBid(
        AuctionItem auction,
        decimal bidAmount,
        decimal currentHighestBid,
        string userId)
    {
        if (auction.IsClosed)
            throw new InvalidOperationException("Auction is closed");

        if (auction.EndTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Auction has expired");

        if (auction.SellerId == userId)
            throw new InvalidOperationException(
                "You cannot bid on your own auction.");

        if (bidAmount <= 0)
            throw new InvalidOperationException(
                "Amount must be > 0");

        if (bidAmount <= currentHighestBid)
            throw new InvalidOperationException(
                $"Bid must be greater than {currentHighestBid}");
    }
}