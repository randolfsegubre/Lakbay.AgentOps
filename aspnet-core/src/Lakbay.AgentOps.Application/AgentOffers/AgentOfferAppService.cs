using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

namespace Lakbay.AgentOps.AgentOffers;

/// <summary>
/// Aggregates Lakbay.AvailabilityApi's accommodations/activities into one agent-shaped
/// bundle, cached in Redis (ADR-0025) so repeated lookups for the same destination
/// during a live call don't re-query the upstream API every time.
/// </summary>
public class AgentOfferAppService : ApplicationService, IAgentOfferAppService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IAvailabilityApiClient _availabilityApiClient;
    private readonly IDistributedCache<AgentOfferDto> _cache;

    public AgentOfferAppService(IAvailabilityApiClient availabilityApiClient, IDistributedCache<AgentOfferDto> cache)
    {
        _availabilityApiClient = availabilityApiClient;
        _cache = cache;
    }

    public async Task<AgentOfferDto> GetOfferAsync(string destinationCode)
    {
        if (string.IsNullOrWhiteSpace(destinationCode))
            throw new ArgumentException("Destination code is required.", nameof(destinationCode));

        var cacheKey = $"agent-offer:{destinationCode}";

        return await _cache.GetOrAddAsync(cacheKey, async () =>
        {
            Logger.LogInformation("Cache miss for {CacheKey} - querying Lakbay.AvailabilityApi", cacheKey);

            var raw = await _availabilityApiClient.GetOfferDataAsync(destinationCode);

            return new AgentOfferDto
            {
                DestinationCode = destinationCode,
                DestinationName = raw.Accommodations.FirstOrDefault()?.Destination?.Name ?? destinationCode,
                Accommodations = raw.Accommodations.ConvertAll(a => new AccommodationOfferDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    OfficialRating = a.OfficialRating,
                }),
                Activities = raw.Activities,
                Packages = raw.Products.ConvertAll(p => new PackageOfferDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    FromPricePhp = p.PriceBands.Count > 0 ? p.PriceBands.Min(b => b.PricePhp) : null,
                }),
                CachedAt = DateTimeOffset.UtcNow,
            };
        }, () => new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
        }) ?? throw new InvalidOperationException("Cache factory unexpectedly returned null.");
    }

    /// <summary>
    /// Called when Lakbay.AgentOps consumes a CatalogSyncEvent (same Service Bus event
    /// Lakbay.AvailabilityApi.Sync already handles, ADR-0013) - invalidates just the
    /// affected destination's entry, never a blanket flush (ADR-0025).
    /// </summary>
    public async Task InvalidateAsync(string destinationCode)
    {
        await _cache.RemoveAsync($"agent-offer:{destinationCode}");
    }
}
