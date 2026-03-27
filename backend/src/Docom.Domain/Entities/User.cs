using Docom.Domain.Enums;

namespace Docom.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Password-based authentication
    public string? PasswordHash { get; set; }
    public bool HasPasswordSet { get; set; } = false;
    public DateTime? LastPasswordChangedAt { get; set; }
    
    public Doctor? Doctor { get; set; }
}
