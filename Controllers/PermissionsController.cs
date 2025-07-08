using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Authorization;
using IdentityServer.Data;
using IdentityServer.Models.DTOs;

namespace IdentityServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(ApplicationDbContext context, ILogger<PermissionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _context.Permissions.ToListAsync();

        var permissionDtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            CreatedAt = p.CreatedAt
        }).ToList();

        return Ok(permissionDtos);
    }

    /// <summary>
    /// Get permissions grouped by category
    /// </summary>
    [HttpGet("by-category")]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermissionsByCategory()
    {
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

        return Ok(groupedPermissions);
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("{id}")]
    [PermissionAuthorize("permissions.read")]
    public async Task<IActionResult> GetPermission(int id)
    {
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

        return Ok(permissionDto);
    }
}