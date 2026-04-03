using Docom.Application.DTOs.Auth;
using Docom.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>
    /// Sends a 6-digit OTP to the provided email.
    /// </summary>
    [HttpPost("request-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto)
    {
        await _authService.RequestOtpAsync(dto.Email);
        return Ok(new { message = "OTP sent to your email." });
    }

    /// <summary>
    /// Verifies the OTP and returns a JWT token.
    /// </summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var result = await _authService.VerifyOtpAsync(dto.Email, dto.Otp);
        return Ok(result);
    }

    /// <summary>
    /// Login using email and password. User must have set a password first.
    /// </summary>
    [HttpPost("login-password")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // Password not set
    public async Task<IActionResult> LoginPassword([FromBody] LoginPasswordDto dto)
    {
        try
        {
            var result = await _authService.VerifyPasswordAsync(dto.Email, dto.Password);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // User hasn't set password yet
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set password for new user after OTP verification. First-time password setup.
    /// </summary>
    [HttpPost("set-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
    {
        try
        {
            await _authService.SetPasswordAsync(dto.Email, dto.Password);
            return Ok(new { message = "Password set successfully. You can now login with your password." });
        }
        catch (ArgumentException ex)
        {
            // Password validation failed
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Request a password reset code (OTP) to be sent to email.
    /// </summary>
    [HttpPost("request-password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        try
        {
            await _authService.RequestPasswordResetAsync(dto.Email);
            return Ok(new { message = "Password reset code sent to your email." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reset password using the code sent to email. Verifies code and sets new password.
    /// Auto-logs in user after successful reset.
    /// </summary>
    [HttpPost("reset-password-with-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto dto)
    {
        try
        {
            var result = await _authService.ResetPasswordWithOtpAsync(dto.Email, dto.ResetCode, dto.NewPassword);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Password validation failed
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
