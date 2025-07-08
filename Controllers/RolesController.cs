using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RolesController> _logger;
    private readonly ICacheService _cacheService;

    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger<RolesController> logger,
        ICacheService cacheService)
    {
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("roles.read")]
    public async Task<IActionResult> GetRoles()
    {
        const string cacheKey = "roles:all";
        
        // Try to get from cache first
        var cachedRoles = await _cacheService.GetAsync<List<RoleDto>>(cacheKey);
        if (cachedRoles != null)
        {
            _logger.LogDebug("Returning roles from cache");
            return Ok(cachedRoles);
        }

        // If not in cache, get from database
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        var roleDtos = roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            Permissions = role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Category = rp.Permission.Category,
                CreatedAt = rp.Permission.CreatedAt
            }).ToList()
        }).ToList();

        // Cache the result
        await _cacheService.SetAsync(cacheKey, roleDtos);
        _logger.LogDebug("Roles cached for key: {CacheKey}", cacheKey);

        return Ok(roleDtos);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id}")]
    [PermissionAuthorize("roles.read")]
    public async Task<IActionResult> GetRole(string id)
    {
        var cacheKey = _cacheService.GenerateKey("roles", id);
        
        // Try to get from cache first
        var cachedRole = await _cacheService.GetAsync<RoleDto>(cacheKey);
        if (cachedRole != null)
        {
            _logger.LogDebug("Returning role from cache for ID: {Id}", id);
            return Ok(cachedRole);
        }

        // If not in cache, get from database
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        var roleDto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            Permissions = role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Category = rp.Permission.Category,
                CreatedAt = rp.Permission.CreatedAt
            }).ToList()
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, roleDto);
        _logger.LogDebug("Role cached for ID: {Id}, key: {CacheKey}", id, cacheKey);

        return Ok(roleDto);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [PermissionAuthorize("roles.create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var roleExists = await _roleManager.RoleExistsAsync(request.Name);
        if (roleExists)
        {
            return Conflict(new { message = "Role already exists!" });
        }

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "Role creation failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        // Assign permissions
        if (request.PermissionIds.Any())
        {
            var permissions = await _context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToListAsync();

            var rolePermissions = permissions.Select(p => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = p.Id
            });

            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Role {RoleName} created successfully by {CurrentUser}", 
            request.Name, User.Identity?.Name);

        // Invalidate role cache
        await InvalidateRoleCacheAsync(role.Id);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new { id = role.Id, message = "Role created successfully" });
    }

    /// <summary>
    /// Update a role
    /// </summary>
    [HttpPut("{id}")]
    [PermissionAuthorize("roles.update")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] CreateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        role.Description = request.Description;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "Role update failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        // Update permissions
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();
        
        _context.RolePermissions.RemoveRange(existingPermissions);

        if (request.PermissionIds.Any())
        {
            var permissions = await _context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToListAsync();

            var rolePermissions = permissions.Select(p => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = p.Id
            });

            _context.RolePermissions.AddRange(rolePermissions);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Role {RoleId} updated successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        // Invalidate role cache
        await InvalidateRoleCacheAsync(id);

        return Ok(new { message = "Role updated successfully" });
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{id}")]
    [PermissionAuthorize("roles.delete")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "Role deletion failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        _logger.LogInformation("Role {RoleId} deleted successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        // Invalidate role cache
        await InvalidateRoleCacheAsync(id);

        return Ok(new { message = "Role deleted successfully" });
    }

    private async Task InvalidateRoleCacheAsync(string roleId)
    {
        // Remove specific role cache
        var roleKey = _cacheService.GenerateKey("roles", roleId);
        await _cacheService.RemoveAsync(roleKey);
        
        // Remove all roles cache
        await _cacheService.RemoveAsync("roles:all");
        
        _logger.LogDebug("Role cache invalidated for role ID: {RoleId}", roleId);
    }
}