using Docom.Domain.Enums;

namespace Docom.Domain.Entities;

public class Session
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Created;
    public int CurrentTokenNumber { get; set; } = 0;
    public int LastIssuedTokenNumber { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
}
