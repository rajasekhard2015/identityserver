using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace IdentityServer.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultExpiration;
    private readonly TimeSpan _slidingExpiration;

    public CacheService(
        IDistributedCache distributedCache,
        IConfiguration configuration,
        ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _configuration = configuration;
        _logger = logger;
        
        // Load cache configuration
        _defaultExpiration = TimeSpan.Parse(_configuration["Redis:DefaultCacheExpiration"] ?? "00:30:00");
        _slidingExpiration = TimeSpan.Parse(_configuration["Redis:SlidingExpiration"] ?? "00:15:00");
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = _slidingExpiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options);
            _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting data to cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing data from cache for key: {Key}", key);
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // Note: This is a simplified implementation. 
            // For production use with Redis, consider using Redis-specific libraries
            // that support pattern-based key deletion (e.g., StackExchange.Redis directly)
            _logger.LogWarning("Pattern-based cache removal not fully implemented for pattern: {Pattern}", pattern);
            
            // For now, we'll log the intention. In a real implementation,
            // you would use Redis SCAN command with pattern matching
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing data from cache by pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }

    public string GenerateKey(string prefix, params object[] identifiers)
    {
        var key = $"{_configuration["Redis:InstanceName"]}:{prefix}";
        if (identifiers?.Length > 0)
        {
            key += ":" + string.Join(":", identifiers.Select(id => id.ToString()));
        }
        return key;
    }
}