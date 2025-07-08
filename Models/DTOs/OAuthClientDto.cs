namespace IdentityServer.Models.DTOs;

public class OAuthClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    // Note: ClientSecret is NOT included for security
    public string RedirectUri { get; set; } = string.Empty;
    public string? PostLogoutRedirectUri { get; set; }
    public string AllowedScopes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}