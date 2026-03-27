using System.ComponentModel.DataAnnotations;

namespace Docom.Application.DTOs.Doctor;

public record CreateDoctorDto(
    [Required, MinLength(2)] string Name,
    [Required, MinLength(2)] string Specialization,
    [Required, RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")]
    string Slug,
    [Required, EmailAddress] string Email,
    string? Address = null,
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    string? Pincode = null
);
