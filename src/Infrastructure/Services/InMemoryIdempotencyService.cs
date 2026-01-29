using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;

/// <summary>
/// In-memory implementation of idempotency service.
/// For production, consider using a distributed cache like Redis.
/// </summary>
public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public InMemoryIdempotencyService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<CachedResponse?> GetCachedResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(idempotencyKey);
        _cache.TryGetValue(cacheKey, out CachedResponse? response);
        return Task.FromResult(response);
    }

    public Task CacheResponseAsync(
        string idempotencyKey,
        int statusCode,
        string body,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(idempotencyKey);
        var response = new CachedResponse(statusCode, body);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        _cache.Set(cacheKey, response, cacheOptions);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(idempotencyKey);
        return Task.FromResult(_cache.TryGetValue(cacheKey, out _));
    }

    private static string GetCacheKey(string idempotencyKey) => $"idempotency:{idempotencyKey}";
}
