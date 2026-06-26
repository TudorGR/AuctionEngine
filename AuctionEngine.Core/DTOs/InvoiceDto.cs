public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public string AuctionTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPaid { get; set; } = false;
}