# Redis Caching Strategy

This document describes the Redis caching implementation for the Identity Server API to improve performance and reduce database load.

## Overview

Redis distributed caching has been implemented for frequently accessed API endpoints that primarily serve read operations. The caching strategy focuses on:

- **Permissions**: Static/semi-static data that rarely changes
- **Roles**: Role definitions and their associated permissions
- **OAuth Clients**: Client configurations and metadata (excluding secrets)

## Configuration

### Redis Connection

Configure Redis connection in `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "IdentityServer",
    "DefaultCacheExpiration": "00:30:00",
    "SlidingExpiration": "00:15:00"
  }
}
```

### Configuration Options

- **ConnectionString**: Redis server connection string
- **InstanceName**: Instance name for cache key prefixing (default: "IdentityServer")
- **DefaultCacheExpiration**: Default absolute expiration time (default: 30 minutes)
- **SlidingExpiration**: Sliding expiration time (default: 15 minutes)

## Cached Endpoints

### Permissions API

- `GET /api/permissions` - All permissions (cache key: `permissions:all`)
- `GET /api/permissions/by-category` - Permissions grouped by category (cache key: `permissions:by-category`)
- `GET /api/permissions/{id}` - Individual permission (cache key: `IdentityServer:permissions:{id}`)

**Cache Expiration**: 30 minutes (permissions are static data)

### Roles API

- `GET /api/roles` - All roles with permissions (cache key: `roles:all`)
- `GET /api/roles/{id}` - Individual role with permissions (cache key: `IdentityServer:roles:{id}`)

**Cache Expiration**: 30 minutes
**Cache Invalidation**: Triggered on role create/update/delete operations

### OAuth Clients API

- `GET /api/oauthclients` - Paginated OAuth clients (cache key: `IdentityServer:oauth-clients:list:{page}:{pageSize}`)
- `GET /api/oauthclients/{id}` - Individual OAuth client (cache key: `IdentityServer:oauth-clients:{id}`)

**Cache Expiration**: 
- List endpoints: 10 minutes (shorter due to pagination)
- Individual clients: 30 minutes

**Cache Invalidation**: Triggered on client create/update/delete/status change operations

## Cache Key Naming Convention

Cache keys follow the pattern: `{InstanceName}:{EntityType}[:{Identifier}]`

Examples:
- `IdentityServer:permissions:42` - Permission with ID 42
- `IdentityServer:roles:abc-123` - Role with ID abc-123
- `IdentityServer:oauth-clients:list:1:10` - OAuth clients list, page 1, 10 items per page

## Cache Invalidation Strategy

### Automatic Invalidation

Cache invalidation is automatically triggered for:

1. **Role Operations**:
   - Create: Invalidates `roles:all`
   - Update: Invalidates `roles:all` and `IdentityServer:roles:{id}`
   - Delete: Invalidates `roles:all` and `IdentityServer:roles:{id}`

2. **OAuth Client Operations**:
   - Create: Invalidates paginated list caches
   - Update: Invalidates `IdentityServer:oauth-clients:{id}` and paginated list caches
   - Status Change: Invalidates `IdentityServer:oauth-clients:{id}` and paginated list caches
   - Secret Regeneration: Invalidates `IdentityServer:oauth-clients:{id}` and paginated list caches
   - Delete: Invalidates `IdentityServer:oauth-clients:{id}` and paginated list caches

### Manual Cache Management

For manual cache operations, the `ICacheService` provides:

```csharp
// Get cached value
var cached = await _cacheService.GetAsync<T>("cache-key");

// Set cache value
await _cacheService.SetAsync("cache-key", value, TimeSpan.FromMinutes(30));

// Remove specific cache entry
await _cacheService.RemoveAsync("cache-key");

// Generate consistent cache keys
var key = _cacheService.GenerateKey("prefix", identifier);
```

## Performance Benefits

- **Reduced Database Load**: Frequently accessed endpoints serve from Redis
- **Improved Response Times**: Sub-millisecond cache retrievals vs. database queries
- **Scalability**: Shared cache across multiple application instances
- **Consistency**: Automatic cache invalidation ensures data consistency

## Best Practices

1. **Cache What's Read Often**: Focus on GET endpoints with high traffic
2. **Appropriate Expiration**: Balance between freshness and performance
3. **Invalidate Aggressively**: Prefer fresh data over stale cache
4. **Monitor Cache Hit Rates**: Track cache effectiveness
5. **Handle Cache Failures**: Application should work even if Redis is unavailable

## Monitoring and Debugging

Enable debug logging to monitor cache operations:

```json
{
  "Logging": {
    "LogLevel": {
      "IdentityServer.Services.CacheService": "Debug",
      "IdentityServer.Controllers": "Debug"
    }
  }
}
```

Cache operations will be logged with details about cache hits, misses, and invalidations.

## Development Setup

For local development without Redis:

1. Install Redis locally or use Docker:
   ```bash
   docker run -d -p 6379:6379 redis:latest
   ```

2. Or disable caching by commenting out Redis configuration in `Program.cs`

## Production Considerations

- **Redis High Availability**: Configure Redis clustering or replication
- **Memory Management**: Monitor Redis memory usage and set appropriate limits
- **Network Latency**: Place Redis close to application servers
- **Security**: Use Redis AUTH and secure network connections
- **Backup**: Consider Redis persistence configuration for cache warmup