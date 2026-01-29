namespace Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<CachedResponse?> GetCachedResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task CacheResponseAsync(string idempotencyKey, int statusCode, string body, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public record CachedResponse(int StatusCode, string Body);
