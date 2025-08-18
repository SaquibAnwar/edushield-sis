using Microsoft.Extensions.Caching.Distributed;

namespace EduShield.Api.Infra;

public static class CacheKeys
{
    // Student caching
    public static string Student(Guid id) => $"student:{id}";
    public static string StudentList() => "students:list";
    public static string StudentByEmail(string email) => $"student:email:{email}";
    
    // Faculty caching
    public static string Faculty(Guid id) => $"faculty:{id}";
    public static string FacultyList() => "faculty:list";
    public static string FacultyByDepartment(string department) => $"faculty:dept:{department}";
    
    // Performance caching
    public static string Performance(Guid id) => $"performance:{id}";
    public static string PerformanceByStudent(Guid studentId) => $"performance:student:{studentId}";
    public static string PerformanceByFaculty(Guid facultyId) => $"performance:faculty:{facultyId}";
    
    // Fee caching
    public static string Fee(Guid id) => $"fee:{id}";
    public static string FeeList() => "fees:list";
    public static string FeeByStudent(Guid studentId) => $"fees:student:{studentId}";
    public static string FeeByType(int type) => $"fees:type:{type}";
    public static string FeeByStatus(int status) => $"fees:status:{status}";
    
    // User caching
    public static string User(Guid id) => $"user:{id}";
    public static string UserList() => "users:list";
    public static string UserByEmail(string email) => $"user:email:{email}";
    public static string UserByRole(int role) => $"users:role:{role}";
    
    // Configuration caching
    public static string Configuration(string key) => $"config:{key}";
    public static string AuthConfiguration() => "config:auth";
    
    // Security caching
    public static string SecurityAlert(Guid id) => $"security:alert:{id}";
    public static string SuspiciousIp(string ip) => $"security:ip:{ip}";
    public static string SuspiciousUser(string userId) => $"security:user:{userId}";
    
    // Cache TTL constants
    public static class TTL
    {
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan Long = TimeSpan.FromHours(1);
        public static readonly TimeSpan VeryLong = TimeSpan.FromHours(6);
        public static readonly TimeSpan Daily = TimeSpan.FromHours(24);
    }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
}

public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            if (data is null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }
            
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return System.Text.Json.JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                SlidingExpiration = ttl > TimeSpan.FromMinutes(10) ? TimeSpan.FromMinutes(10) : null
            };

            var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedValue, options, ct);
            
            _logger.LogDebug("Cached value for key: {Key} with TTL: {TTL}", key, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // Note: Redis supports pattern-based deletion, but IDistributedCache doesn't
        // In a production environment, you might want to use StackExchange.Redis directly
        // For now, we'll log this as a limitation
        _logger.LogInformation("Pattern-based cache removal requested for: {Pattern}. This requires Redis-specific implementation.", pattern);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            return data != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        var cachedValue = await GetAsync<T>(key, ct);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();
        await SetAsync(key, value, ttl, ct);
        return value;
    }
}





