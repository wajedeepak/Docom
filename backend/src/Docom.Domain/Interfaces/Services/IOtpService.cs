namespace Docom.Domain.Interfaces.Services;

public interface IOtpService
{
    Task SendOtpAsync(string email, string otp, string purpose = "Login");
}
