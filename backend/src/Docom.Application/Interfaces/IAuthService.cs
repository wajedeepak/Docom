using Docom.Application.DTOs.Auth;

namespace Docom.Application.Interfaces;

public interface IAuthService
{
    // OTP-based authentication
    Task RequestOtpAsync(string email);
    Task<AuthResponseDto> VerifyOtpAsync(string email, string otp);

    // Password-based authentication
    Task<AuthResponseDto> VerifyPasswordAsync(string email, string password);
    Task SetPasswordAsync(string email, string newPassword);

    // Password reset
    Task RequestPasswordResetAsync(string email);
    Task<AuthResponseDto> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
}
