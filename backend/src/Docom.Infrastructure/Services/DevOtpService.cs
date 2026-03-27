using Docom.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Docom.Infrastructure.Services;

/// <summary>
/// Development-only OTP service. Prints the OTP to the console instead of sending email.
/// NEVER register this in Production — use EmailOtpService instead.
/// </summary>
public class DevOtpService : IOtpService
{
    private readonly ILogger<DevOtpService> _logger;

    public DevOtpService(ILogger<DevOtpService> logger) => _logger = logger;

    public Task SendOtpAsync(string email, string otp, string purpose = "Login")
    {
        _logger.LogWarning("========================================");
        _logger.LogWarning("DEV OTP for {Email} ({Purpose}): {Otp}", email, purpose, otp);
        _logger.LogWarning("========================================");
        return Task.CompletedTask;
    }
}
