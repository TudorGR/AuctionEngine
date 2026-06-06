using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Services;

namespace AuctionEngine.Tests.Services;

public class BidValidationServiceTests
{
    [Fact]
    public void Accepts_Higher_Bid()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            CurrentHighestBid = 100,
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };
        var exception = Record.Exception(() => BidValidationService.ValidateBid(auction, 150, 100, "buyer"));

        Assert.Null(exception);
    }

    [Fact]
    public void Rejects_Lower_Bid()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.Throws<InvalidOperationException>(() =>
            BidValidationService.ValidateBid(
                auction,
                90,
                100,
                "buyer"));
    }

    [Fact]
    public void Rejects_Equal_Bid()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.Throws<InvalidOperationException>(() =>
            BidValidationService.ValidateBid(
                auction,
                100,
                100,
                "buyer"));
    }

    [Fact]
    public void Rejects_Closed_Auction()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            IsClosed = true,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.Throws<InvalidOperationException>(() =>
            BidValidationService.ValidateBid(
                auction,
                150,
                100,
                "buyer"));
    }

    [Fact]
    public void Rejects_Expired_Auction()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.Throws<InvalidOperationException>(() =>
            BidValidationService.ValidateBid(
                auction,
                150,
                100,
                "buyer"));
    }

    [Fact]
    public void Rejects_Seller_Bidding_On_Own_Auction()
    {
        var auction = new AuctionItem
        {
            SellerId = "seller",
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.Throws<InvalidOperationException>(() =>
            BidValidationService.ValidateBid(
                auction,
                150,
                100,
                "seller"));
    }
}