namespace Docom.Application.DTOs.Auth;

public record AuthResponseDto(
    string Token,
    string Email,
    string Name,
    string Role,
    int? DoctorId,
    string? DoctorSlug,
    bool HasPasswordSet = false  // Indicates if user has set a password
);
