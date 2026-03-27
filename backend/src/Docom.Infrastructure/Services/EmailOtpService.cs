using Docom.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Docom.Infrastructure.Services;

public class EmailOtpService : IOtpService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailOtpService> _logger;

    public EmailOtpService(IConfiguration config, ILogger<EmailOtpService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOtpAsync(string email, string otp, string purpose = "Login")
    {
        var smtpSection = _config.GetSection("Smtp");
        var host = smtpSection["Host"]!;
        var port = int.Parse(smtpSection["Port"]!);
        var user = smtpSection["User"]!;
        var pass = smtpSection["Password"]!;
        var from = smtpSection["From"]!;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var subject = purpose switch
        {
            "Password Reset" => $"{otp} — Your Docom password reset code",
            _ => $"{otp} — Your Docom login code"
        };

        var message = new MailMessage
        {
            From = new MailAddress(from, "Docom"),
            Subject = subject,
            Body = $"""
                Your Docom {purpose.ToLower()} code is: {otp}

                This code expires in 10 minutes.
                If you did not request this, please ignore this email.
                """,
            IsBodyHtml = false
        };

        message.To.Add(email);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("OTP sent to {Email} for {Purpose}", email, purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP to {Email}", email);
            throw new InvalidOperationException("Failed to send code. Please try again.");
        }
    }
}
