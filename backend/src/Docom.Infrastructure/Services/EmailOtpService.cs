using Docom.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
//using System.Net;
//using System.Net.Mail;
using MailKit.Net.Smtp;
using MimeKit;

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

    var subject = purpose switch
    {
      "Password Reset" => $"{otp} — Your Docom password reset code",
      _ => $"{otp} — Your Docom login code"
    };

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Docom", user));
    message.To.Add(new MailboxAddress("", email));
    message.Subject = subject;

    message.Body = new TextPart("plain")
    {
      Text = $"""
                Your Docom {purpose.ToLower()} code is: {otp}

                This code expires in 10 minutes.
                If you did not request this, please ignore this email.
                """
    };
    

    

    try
    {
      using var client = new SmtpClient();
      await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
      await client.AuthenticateAsync(user, pass);
      await client.SendAsync(message);
      await client.DisconnectAsync(true);
      _logger.LogInformation("OTP sent to {Email} for {Purpose}", email, purpose);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send OTP to {Email}", email);
      throw new InvalidOperationException("Failed to send code. Please try again.");
    }
  }

  //public async Task SendOtpAsync(string email, string otp, string purpose = "Login")
  //{
  //  var message = new MimeMessage();
  //  message.From.Add(new MailboxAddress("Docom", "support@docom.in"));
  //  message.To.Add(new MailboxAddress("", email));
  //  message.Subject = "Your OTP Code";

  //  message.Body = new TextPart("plain")
  //  {
  //    Text = $"Your OTP is {otp}"
  //  };

  //  using var client = new SmtpClient();
  //  await client.ConnectAsync("mdus-pp-wb11.webhostbox.net", 587, MailKit.Security.SecureSocketOptions.StartTls);
  //  await client.AuthenticateAsync("support@docom.in", "w6Aj4u_73");
  //  await client.SendAsync(message);
  //  await client.DisconnectAsync(true);
  //}
}
