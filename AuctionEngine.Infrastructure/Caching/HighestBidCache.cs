using AuctionEngine.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

public class HighestBidCache : IHighestBidCache
{
    public IDistributedCache _cache;

    public HighestBidCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<decimal?> GetHighestBidAsync(Guid auctionId)
    {
        var value = await _cache.GetStringAsync($"highest-bid:{auctionId}");

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, out var result))
            return result;

        return null;
    }

    public async Task SetHighestBidAsync(Guid auctionId, decimal amount)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        };
        await _cache.SetStringAsync($"highest-bid:{auctionId}", amount.ToString(), options);
    }
}