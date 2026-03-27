namespace Docom.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Address { get; set; }
    public string? Pincode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
