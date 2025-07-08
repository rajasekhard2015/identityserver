using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class RolePermission
{
    public int Id { get; set; }
    
    [Required]
    public string RoleId { get; set; } = string.Empty;
    
    [Required]
    public int PermissionId { get; set; }
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ApplicationRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}