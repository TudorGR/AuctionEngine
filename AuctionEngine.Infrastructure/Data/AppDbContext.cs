using AuctionEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionEngine.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<AuctionItem> AuctionItems { get; set; }
    public DbSet<Bid> Bids { get; set; }
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuctionItem>()
        .HasOne(a => a.Seller)
        .WithMany(u => u.CreatedAuctions)
        .HasForeignKey(a => a.SellerId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bid>()
        .HasOne(a => a.Bidder)
        .WithMany(u => u.Bids)
        .HasForeignKey(b => b.BidderId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bid>()
        .HasOne(b => b.AuctionItem)
        .WithMany(a => a.Bids)
        .HasForeignKey(b => b.AuctionItemId)
        .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<AuctionItem>()
        // .Property(a => a.RowVersion)
        // .IsRowVersion();
    }
}