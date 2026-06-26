namespace AuctionEngine.Core.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public string WinnerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPaid { get; set; } = false;

    public AuctionItem Auction { get; set; } = null!;
    public ApplicationUser Winner { get; set; } = null!;
}