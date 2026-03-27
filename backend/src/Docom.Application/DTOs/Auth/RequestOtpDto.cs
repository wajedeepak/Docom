using System.ComponentModel.DataAnnotations;

namespace Docom.Application.DTOs.Auth;

public record RequestOtpDto(
    [Required, EmailAddress] string Email
);
