namespace Docom.Application.DTOs.Token;

public record WalkInDto(
    string? PatientName = null,
    string? PhoneNumber = null
);
