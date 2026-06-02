namespace AuctionEngine.Core.Entities;

public class Bid
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }

    public Guid AuctionItemId { get; set; }
    public AuctionItem AuctionItem { get; set; } = null!;

    public string BidderId { get; set; } = string.Empty;
    public ApplicationUser Bidder { get; set; } = null!;
}