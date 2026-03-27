namespace Docom.Application.DTOs.Token;

public record TrackingResolveDto(
    string DoctorSlug,
    int SessionId,
    int TokenNumber
);
