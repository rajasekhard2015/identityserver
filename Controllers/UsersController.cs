using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Authorization;
using IdentityServer.Models;
using IdentityServer.Models.DTOs;
using IdentityServer.Models.Requests;

namespace IdentityServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("users.read")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var users = await _userManager.Users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserDto>();
        
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles
            });
        }

        var totalCount = await _userManager.Users.CountAsync();
        
        return Ok(new
        {
            users = userDtos,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [PermissionAuthorize("users.read")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        
        var userDto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [PermissionAuthorize("users.create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var userExists = await _userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
        {
            return Conflict(new { message = "User already exists!" });
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "User creation failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        // Assign roles
        if (request.Roles.Any())
        {
            var validRoles = request.Roles.Where(role => _roleManager.RoleExistsAsync(role).Result);
            if (validRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, validRoles);
            }
        }

        _logger.LogInformation("User {Email} created successfully by {CurrentUser}", 
            request.Email, User.Identity?.Name);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { id = user.Id, message = "User created successfully" });
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id}")]
    [PermissionAuthorize("users.update")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "User update failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        
        if (request.Roles.Any())
        {
            var validRoles = request.Roles.Where(role => _roleManager.RoleExistsAsync(role).Result);
            if (validRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, validRoles);
            }
        }

        _logger.LogInformation("User {UserId} updated successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        return Ok(new { message = "User updated successfully" });
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    [PermissionAuthorize("users.delete")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { 
                message = "User deletion failed", 
                errors = result.Errors.Select(e => e.Description) 
            });
        }

        _logger.LogInformation("User {UserId} deleted successfully by {CurrentUser}", 
            id, User.Identity?.Name);

        return Ok(new { message = "User deleted successfully" });
    }
}