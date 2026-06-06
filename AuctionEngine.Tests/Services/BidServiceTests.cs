using AuctionEngine.Core.Entities;
using AuctionEngine.Core.Interfaces;
using AuctionEngine.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuctionEngine.Tests.Services;

public class BidServiceTests
{
    [Fact]
    public async Task PlaceBidAsync_Accepts_Higher_Bid()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = "buyer";
        var auction = new AuctionItem
        {
            Id = auctionId,
            SellerId = "seller",
            CurrentHighestBid = 100,
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        var repository = new Mock<IAuctionBidRepository>();
        repository.Setup(x => x.GetAuctionAsync(auctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        repository.Setup(x => x.AddBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cache = new Mock<IHighestBidCache>();
        cache.Setup(x => x.GetHighestBidAsync(auctionId)).ReturnsAsync((decimal?)100);
        cache.Setup(x => x.SetHighestBidAsync(auctionId, 150)).Returns(Task.CompletedTask);

        var notifier = new Mock<IAuctionBidNotifier>();
        notifier.Setup(x => x.NotifyNewBidAsync(auctionId, 150, bidderId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AuctionBidService(repository.Object, cache.Object, notifier.Object);

        var result = await service.PlaceBidAsync(auctionId, bidderId, 150, CancellationToken.None);

        Assert.Equal(BidPlacementStatus.Created, result.Status);
        Assert.NotNull(result.BidId);
        Assert.Equal(150, result.Amount);
        repository.Verify(x => x.AddBidAsync(It.Is<Bid>(b => b.Amount == 150 && b.BidderId == bidderId), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.SetHighestBidAsync(auctionId, 150), Times.Once);
        notifier.Verify(x => x.NotifyNewBidAsync(auctionId, 150, bidderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceBidAsync_Returns_Conflict_On_Concurrency_Error()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = "buyer";
        var auction = new AuctionItem
        {
            Id = auctionId,
            SellerId = "seller",
            CurrentHighestBid = 100,
            IsClosed = false,
            EndTime = DateTime.UtcNow.AddMinutes(5)
        };

        var repository = new Mock<IAuctionBidRepository>();
        repository.Setup(x => x.GetAuctionAsync(auctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        repository.Setup(x => x.AddBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var cache = new Mock<IHighestBidCache>();
        cache.Setup(x => x.GetHighestBidAsync(auctionId)).ReturnsAsync((decimal?)100);

        var notifier = new Mock<IAuctionBidNotifier>();

        var service = new AuctionBidService(repository.Object, cache.Object, notifier.Object);

        var result = await service.PlaceBidAsync(auctionId, bidderId, 150, CancellationToken.None);

        Assert.Equal(BidPlacementStatus.Conflict, result.Status);
        Assert.Equal("Another bid was placed simultaneously. Please try again.", result.ErrorMessage);
        cache.Verify(x => x.SetHighestBidAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        notifier.Verify(x => x.NotifyNewBidAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
