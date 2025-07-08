using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models.Requests;

public class UpdateUserRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public List<string> Roles { get; set; } = new();
}