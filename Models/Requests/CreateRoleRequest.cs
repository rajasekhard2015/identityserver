using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models.Requests;

public class CreateRoleRequest
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(512)]
    public string? Description { get; set; }
    
    public List<int> PermissionIds { get; set; } = new();
}