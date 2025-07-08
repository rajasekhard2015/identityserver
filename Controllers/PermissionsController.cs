using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Authorization;
using IdentityServer.Data;
using IdentityServer.Models.DTOs;
using IdentityServer.Services;

namespace IdentityServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionsController> _logger;
    private readonly ICacheService _cacheService;

    public PermissionsController(
        ApplicationDbContext context, 
        ILogger<PermissionsController> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermissions()
    {
        const string cacheKey = "permissions:all";
        
        // Try to get from cache first
        var cachedPermissions = await _cacheService.GetAsync<List<PermissionDto>>(cacheKey);
        if (cachedPermissions != null)
        {
            _logger.LogDebug("Returning permissions from cache");
            return Ok(cachedPermissions);
        }

        // If not in cache, get from database
        var permissions = await _context.Permissions.ToListAsync();

        var permissionDtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            CreatedAt = p.CreatedAt
        }).ToList();

        // Cache the result
        await _cacheService.SetAsync(cacheKey, permissionDtos);
        _logger.LogDebug("Permissions cached for key: {CacheKey}", cacheKey);

        return Ok(permissionDtos);
    }

    /// <summary>
    /// Get permissions grouped by category
    /// </summary>
    [HttpGet("by-category")]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermissionsByCategory()
    {
        const string cacheKey = "permissions:by-category";
        
        // Try to get from cache first
        var cachedGroupedPermissions = await _cacheService.GetAsync<object>(cacheKey);
        if (cachedGroupedPermissions != null)
        {
            _logger.LogDebug("Returning grouped permissions from cache");
            return Ok(cachedGroupedPermissions);
        }

        // If not in cache, get from database
        var permissions = await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var groupedPermissions = permissions
            .GroupBy(p => p.Category)
            .Select(g => new
            {
                Category = g.Key,
                Permissions = g.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToList();

        // Cache the result
        await _cacheService.SetAsync(cacheKey, groupedPermissions);
        _logger.LogDebug("Grouped permissions cached for key: {CacheKey}", cacheKey);

        return Ok(groupedPermissions);
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("{id}")]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermission(int id)
    {
        var cacheKey = _cacheService.GenerateKey("permissions", id);
        
        // Try to get from cache first
        var cachedPermission = await _cacheService.GetAsync<PermissionDto>(cacheKey);
        if (cachedPermission != null)
        {
            _logger.LogDebug("Returning permission from cache for ID: {Id}", id);
            return Ok(cachedPermission);
        }

        // If not in cache, get from database
        var permission = await _context.Permissions.FindAsync(id);
        
        if (permission == null)
        {
            return NotFound(new { message = "Permission not found" });
        }

        var permissionDto = new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Category = permission.Category,
            CreatedAt = permission.CreatedAt
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, permissionDto);
        _logger.LogDebug("Permission cached for ID: {Id}, key: {CacheKey}", id, cacheKey);

        return Ok(permissionDto);
    }
}