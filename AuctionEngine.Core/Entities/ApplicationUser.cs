using Microsoft.AspNetCore.Identity;

namespace AuctionEngine.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<AuctionItem> CreatedAuctions { get; set; } = new List<AuctionItem>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}