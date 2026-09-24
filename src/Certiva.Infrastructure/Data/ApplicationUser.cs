using Microsoft.AspNetCore.Identity;

namespace Certiva.Infrastructure.Data;

// Mirrors the extra columns already present in the shared AspNetUsers table.
public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
