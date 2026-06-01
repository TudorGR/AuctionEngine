namespace AuctionEngine.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<AuctionItem> CreatedAuctions { get; set; } = new List<AuctionItem>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}