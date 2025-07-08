using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class OAuthClient
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string ClientSecret { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string RedirectUri { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? PostLogoutRedirectUri { get; set; }
    
    [Required]
    [StringLength(255)]
    public string AllowedScopes { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
    
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;
}