namespace AuctionEngine.Core.Entities;

public class AuctionItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentHighestBid { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsClosed { get; set; }

    public string SellerId { get; set; } = string.Empty;
    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}