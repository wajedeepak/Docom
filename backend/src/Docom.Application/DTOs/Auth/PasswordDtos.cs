using System.ComponentModel.DataAnnotations;

namespace Docom.Application.DTOs.Auth;

/// <summary>
/// Login with email and password
/// </summary>
public record LoginPasswordDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

/// <summary>
/// Set password for first-time user or during reset
/// </summary>
public record SetPasswordDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

/// <summary>
/// Request password reset OTP
/// </summary>
public record RequestPasswordResetDto(
    [Required, EmailAddress] string Email
);

/// <summary>
/// Reset password using OTP code
/// </summary>
public record ResetPasswordWithOtpDto(
    [Required, EmailAddress] string Email,
    [Required, Length(6, 6)] string ResetCode,
    [Required, MinLength(6)] string NewPassword
);
