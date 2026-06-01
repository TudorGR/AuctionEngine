namespace AuctionEngine.Core.Entities;

public class AuctionItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsClosed { get; set; }

    public Guid SellerId { get; set; }
    public User Seller { get; set; } = null!;

    // public byte[] RowVersion { get; set; } = [];

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}