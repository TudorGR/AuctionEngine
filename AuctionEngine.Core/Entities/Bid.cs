namespace AuctionEngine.Core.Entities;

public class Bid
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }

    public Guid AuctionItemId { get; set; }
    public AuctionItem AuctionItem { get; set; } = null!;

    public Guid BidderId { get; set; }
    public User Bidder { get; set; } = null!;
}