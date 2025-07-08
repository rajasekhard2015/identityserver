using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Models;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property for permissions
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}