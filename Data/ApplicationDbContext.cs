using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Models;

namespace IdentityServer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<OAuthClient> OAuthClients { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Permission entity
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        });

        // Configure RolePermission entity
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OAuthClient entity
        builder.Entity<OAuthClient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RedirectUri).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PostLogoutRedirectUri).HasMaxLength(500);
            entity.Property(e => e.AllowedScopes).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
        });

        // Seed default permissions
        SeedPermissions(builder);
    }

    private static void SeedPermissions(ModelBuilder builder)
    {
        var permissions = new[]
        {
            new Permission { Id = 1, Name = "users.read", Description = "View users", Category = "Users" },
            new Permission { Id = 2, Name = "users.create", Description = "Create users", Category = "Users" },
            new Permission { Id = 3, Name = "users.update", Description = "Update users", Category = "Users" },
            new Permission { Id = 4, Name = "users.delete", Description = "Delete users", Category = "Users" },
            new Permission { Id = 5, Name = "roles.read", Description = "View roles", Category = "Roles" },
            new Permission { Id = 6, Name = "roles.create", Description = "Create roles", Category = "Roles" },
            new Permission { Id = 7, Name = "roles.update", Description = "Update roles", Category = "Roles" },
            new Permission { Id = 8, Name = "roles.delete", Description = "Delete roles", Category = "Roles" },
            new Permission { Id = 9, Name = "permissions.read", Description = "View permissions", Category = "Permissions" },
            new Permission { Id = 10, Name = "oauth.read", Description = "View OAuth clients", Category = "OAuth" },
            new Permission { Id = 11, Name = "oauth.create", Description = "Create OAuth clients", Category = "OAuth" },
            new Permission { Id = 12, Name = "oauth.update", Description = "Update OAuth clients", Category = "OAuth" },
            new Permission { Id = 13, Name = "oauth.delete", Description = "Delete OAuth clients", Category = "OAuth" }
        };

        builder.Entity<Permission>().HasData(permissions);
    }
}