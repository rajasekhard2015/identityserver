using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models.Requests;

public class CreateOAuthClientRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    [StringLength(500)]
    public string RedirectUri { get; set; } = string.Empty;
    
    [Url]
    [StringLength(500)]
    public string? PostLogoutRedirectUri { get; set; }
    
    [Required]
    [StringLength(255)]
    public string AllowedScopes { get; set; } = "openid profile";
}