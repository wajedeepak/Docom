namespace Docom.Application.DTOs.Doctor;

public record DoctorDto(
    int Id,
    string Slug,
    string Name,
    string Specialization,
    string? Address,
    string? Pincode,
    bool IsActive,
    DateTime CreatedAt
);
