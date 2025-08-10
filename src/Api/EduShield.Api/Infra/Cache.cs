using Microsoft.Extensions.Caching.Distributed;

namespace EduShield.Api.Infra;

public static class CacheKeys
{
    public static string Student(Guid id) => $"student:{id}";
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}

public sealed class DistributedCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var data = await cache.GetStringAsync(key, ct);
        return data is null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(data);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
        => cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);

    public Task RemoveAsync(string key, CancellationToken ct)
        => cache.RemoveAsync(key, ct);
}


