using Docom.Domain.Enums;

namespace Docom.Domain.Entities;

public class Token
{
    public long Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public int TokenNumber { get; set; }
    public string? PatientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string PublicTokenId { get; set; } = string.Empty;
    public TokenStatus Status { get; set; } = TokenStatus.Waiting;
    public int QueueOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ServedAt { get; set; }
}
