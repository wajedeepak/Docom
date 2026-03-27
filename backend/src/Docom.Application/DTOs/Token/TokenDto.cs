namespace Docom.Application.DTOs.Token;

public record TokenDto(
    long Id,
    int TokenNumber,
    string? PatientName,
    string? PhoneNumber,
    string Status,
    int QueueOrder,
    DateTime CreatedAt
);
