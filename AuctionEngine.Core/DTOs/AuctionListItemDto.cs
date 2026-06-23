namespace AuctionEngine.Core.DTOs;

public class AuctionListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentHighestBid { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsClosed { get; set; }

    public string SellerName { get; set; } = string.Empty;
}