namespace Docom.Application.DTOs.Token;

public record TakeTokenDto(
    string? PatientName,
    string? PhoneNumber = null
);
