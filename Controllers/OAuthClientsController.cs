using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using IdentityServer.Authorization;
using IdentityServer.Data;
using IdentityServer.Models;
using IdentityServer.Models.DTOs;
using IdentityServer.Models.Requests;
using IdentityServer.Services;

namespace IdentityServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OAuthClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OAuthClientsController> _logger;
    private readonly ICacheService _cacheService;

    public OAuthClientsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OAuthClientsController> logger,
        ICacheService cacheService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get all OAuth clients (without secrets)
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("oauth.read")]
    public async Task<IActionResult> GetOAuthClients([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var cacheKey = _cacheService.GenerateKey("oauth-clients", "list", page, pageSize);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<object>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug("Returning OAuth clients from cache for page: {Page}, pageSize: {PageSize}", page, pageSize);
            return Ok(cachedResult);
        }

        // If not in cache, get from database
        var clients = await _context.OAuthClients
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new OAuthClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ClientId = c.ClientId,
                RedirectUri = c.RedirectUri,
                PostLogoutRedirectUri = c.PostLogoutRedirectUri,
                AllowedScopes = c.AllowedScopes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                LastUsedAt = c.LastUsedAt,
                CreatedBy = c.CreatedBy
            })
            .ToListAsync();

        var totalCount = await _context.OAuthClients.CountAsync();

        var result = new
        {
            clients,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };

        // Cache the result with shorter expiration for paginated data
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        _logger.LogDebug("OAuth clients cached for page: {Page}, pageSize: {PageSize}, key: {CacheKey}", page, pageSize, cacheKey);

        return Ok(result);
    }

    /// <summary>
    /// Get OAuth client by ID (without secret)
    /// </summary>
    [HttpGet("{id}")]
    [PermissionAuthorize("oauth.read")]
    public async Task<IActionResult> GetOAuthClient(int id)
    {
        var cacheKey = _cacheService.GenerateKey("oauth-clients", id);
        
        // Try to get from cache first
        var cachedClient = await _cacheService.GetAsync<OAuthClientDto>(cacheKey);
        if (cachedClient != null)
        {
            _logger.LogDebug("Returning OAuth client from cache for ID: {Id}", id);
            return Ok(cachedClient);
        }

        // If not in cache, get from database
        var client = await _context.OAuthClients.FindAsync(id);
        
        if (client == null)
        {
            return NotFound(new { message = "OAuth client not found" });
        }

        var clientDto = new OAuthClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            ClientId = client.ClientId,
            RedirectUri = client.RedirectUri,
            PostLogoutRedirectUri = client.PostLogoutRedirectUri,
            AllowedScopes = client.AllowedScopes,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            CreatedBy = client.CreatedBy
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, clientDto);
        _logger.LogDebug("OAuth client cached for ID: {Id}, key: {CacheKey}", id, cacheKey);

        return Ok(clientDto);
    }

    /// <summary>
    /// Create a new OAuth client
    /// </summary>
    [HttpPost]
    [PermissionAuthorize("oauth.create")]
    public async Task<IActionResult> CreateOAuthClient([FromBody] CreateOAuthClientRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var clientId = GenerateClientId();
        var clientSecret = GenerateClientSecret();

        var client = new OAuthClient
        {
            Name = request.Name,
            Description = request.Description,
            ClientId = clientId,
            ClientSecret = HashSecret(clientSecret),
            RedirectUri = request.RedirectUri,
            PostLogoutRedirectUri = request.PostLogoutRedirectUri,
            AllowedScopes = request.AllowedScopes,
            CreatedBy = user.Email!
        };

        _context.OAuthClients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("OAuth client {ClientName} created successfully by {CurrentUser}", 
            request.Name, User.Identity?.Name);

        // Invalidate OAuth client cache
        await InvalidateOAuthClientCacheAsync();

        // Return the plain text client secret only once during creation
        return CreatedAtAction(nameof(GetOAuthClient), new { id = client.Id }, new
        {
            id = client.Id,
            clientId = client.ClientId,
            clientSecret = clientSecret, // Only returned during creation
            message = "OAuth client created successfully. Store the client secret securely as it won't be shown again."
        });
    }

    /// <summary>
    /// Update an OAuth client
    /// </summary>
    [HttpPut("{id}")]
    [PermissionAuthorize("oauth.update")]
    public async Task<IActionResult> UpdateOAuthClient(int id, [FromBody] CreateOAuthClientRequest request)
    {
        var client = await _context.OAuthClients.FindAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "OAuth client not found" });
        }

        client.Name = request.Name;
        client.Description = request.Description;
        client.RedirectUri = request.RedirectUri;
        client.PostLogoutRedirectUri = request.PostLogoutRedirectUri;
        client.AllowedScopes = request.AllowedScopes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("OAuth client {ClientId} updated successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        // Invalidate OAuth client cache
        await InvalidateOAuthClientCacheAsync(id);

        return Ok(new { message = "OAuth client updated successfully" });
    }

    /// <summary>
    /// Regenerate client secret
    /// </summary>
    [HttpPost("{id}/regenerate-secret")]
    [PermissionAuthorize("oauth.update")]
    public async Task<IActionResult> RegenerateClientSecret(int id)
    {
        var client = await _context.OAuthClients.FindAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "OAuth client not found" });
        }

        var newClientSecret = GenerateClientSecret();
        client.ClientSecret = HashSecret(newClientSecret);

        await _context.SaveChangesAsync();

        _logger.LogInformation("OAuth client {ClientId} secret regenerated by {CurrentUser}", 
            id, User.Identity?.Name);

        // Invalidate OAuth client cache
        await InvalidateOAuthClientCacheAsync(id);

        return Ok(new
        {
            clientSecret = newClientSecret,
            message = "Client secret regenerated successfully. Store it securely as it won't be shown again."
        });
    }

    /// <summary>
    /// Activate/deactivate OAuth client
    /// </summary>
    [HttpPatch("{id}/status")]
    [PermissionAuthorize("oauth.update")]
    public async Task<IActionResult> UpdateClientStatus(int id, [FromBody] bool isActive)
    {
        var client = await _context.OAuthClients.FindAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "OAuth client not found" });
        }

        client.IsActive = isActive;
        await _context.SaveChangesAsync();

        _logger.LogInformation("OAuth client {ClientId} status changed to {Status} by {CurrentUser}", 
            id, isActive ? "active" : "inactive", User.Identity?.Name);

        // Invalidate OAuth client cache
        await InvalidateOAuthClientCacheAsync(id);

        return Ok(new { message = $"OAuth client {(isActive ? "activated" : "deactivated")} successfully" });
    }

    /// <summary>
    /// Delete an OAuth client
    /// </summary>
    [HttpDelete("{id}")]
    [PermissionAuthorize("oauth.delete")]
    public async Task<IActionResult> DeleteOAuthClient(int id)
    {
        var client = await _context.OAuthClients.FindAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "OAuth client not found" });
        }

        _context.OAuthClients.Remove(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("OAuth client {ClientId} deleted successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        // Invalidate OAuth client cache
        await InvalidateOAuthClientCacheAsync(id);

        return Ok(new { message = "OAuth client deleted successfully" });
    }

    private async Task InvalidateOAuthClientCacheAsync(int? clientId = null)
    {
        // Remove specific client cache if provided
        if (clientId.HasValue)
        {
            var clientKey = _cacheService.GenerateKey("oauth-clients", clientId.Value);
            await _cacheService.RemoveAsync(clientKey);
        }
        
        // Remove all OAuth client list caches (since they contain pagination)
        // Note: This is a simplified approach. In production, consider implementing
        // pattern-based cache invalidation or tracking specific cache keys
        for (int page = 1; page <= 10; page++) // Assuming max 10 pages for invalidation
        {
            for (int pageSize = 10; pageSize <= 50; pageSize += 10) // Common page sizes
            {
                var listKey = _cacheService.GenerateKey("oauth-clients", "list", page, pageSize);
                await _cacheService.RemoveAsync(listKey);
            }
        }
        
        _logger.LogDebug("OAuth client cache invalidated for client ID: {ClientId}", clientId);
    }

    private static string GenerateClientId()
    {
        return $"client_{Guid.NewGuid():N}";
    }

    private static string GenerateClientSecret()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashSecret(string secret)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hashedBytes);
    }
}