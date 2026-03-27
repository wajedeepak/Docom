namespace Docom.Application.DTOs.Session;

public record SessionDto(
    int Id,
    string Label,
    string Status,
    int CurrentTokenNumber,
    int LastIssuedTokenNumber,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? EndedAt
);
