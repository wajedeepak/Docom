using System.Security.Cryptography;
using System.Text;
using Docom.Application.DTOs.Auth;
using Docom.Application.Interfaces;
using Docom.Domain.Entities;
using Docom.Domain.Interfaces.Repositories;
using Docom.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Docom.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IOtpRepository _otpRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IOtpService _otpService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IOtpRepository otpRepo,
        IDoctorRepository doctorRepo,
        IOtpService otpService,
        IPasswordService passwordService,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _otpRepo = otpRepo;
        _doctorRepo = doctorRepo;
        _otpService = otpService;
        _passwordService = passwordService;
        _config = config;
    }

    public async Task RequestOtpAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("No account found for this email.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var otp = GenerateOtp();
        var hash = HashOtp(otp);

        await _otpRepo.InvalidatePreviousAsync(email);

        await _otpRepo.CreateAsync(new OtpRequest
        {
            Email = email,
            OtpHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        await _otpService.SendOtpAsync(email, otp);
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(string email, string otp)
    {
        var otpRequest = await _otpRepo.GetLatestValidAsync(email)
            ?? throw new UnauthorizedAccessException("OTP expired or not found.");

        if (otpRequest.IsUsed)
            throw new UnauthorizedAccessException("OTP already used.");

        if (otpRequest.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("OTP has expired.");

        if (!VerifyOtpHash(otp, otpRequest.OtpHash))
            throw new UnauthorizedAccessException("Invalid OTP.");

        await _otpRepo.MarkUsedAsync(otpRequest.Id);

        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("User not found.");

        Doctor? doctor = null;
        if (user.Doctor != null)
            doctor = await _doctorRepo.GetByUserIdAsync(user.Id);

        var token = GenerateJwt(user, doctor);

        return new AuthResponseDto(
            Token: token,
            Email: user.Email,
            Name: user.Name,
            Role: user.Role.ToString(),
            DoctorId: doctor?.Id,
            DoctorSlug: doctor?.Slug,
            HasPasswordSet: user.HasPasswordSet
        );
    }

    private string GenerateJwt(User user, Doctor? doctor)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (doctor != null)
        {
            claims.Add(new Claim("doctorId", doctor.Id.ToString()));
            claims.Add(new Claim("doctorSlug", doctor.Slug));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp()
        => Random.Shared.Next(100000, 999999).ToString();

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyOtpHash(string otp, string hash)
        => HashOtp(otp).Equals(hash, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Verify password-based login
    /// </summary>
    public async Task<AuthResponseDto> VerifyPasswordAsync(string email, string password)
    {
        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("No account found for this email.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        if (!user.HasPasswordSet || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidOperationException("Password not set. Please use OTP login first to set a password.");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        Doctor? doctor = null;
        if (user.Doctor != null)
            doctor = await _doctorRepo.GetByUserIdAsync(user.Id);

        var token = GenerateJwt(user, doctor);

        return new AuthResponseDto(
            Token: token,
            Email: user.Email,
            Name: user.Name,
            Role: user.Role.ToString(),
            DoctorId: doctor?.Id,
            DoctorSlug: doctor?.Slug,
            HasPasswordSet: user.HasPasswordSet
        );
    }

    /// <summary>
    /// Set password for user (first-time setup or password reset)
    /// </summary>
    public async Task SetPasswordAsync(string email, string newPassword)
    {
        _passwordService.ValidatePasswordStrength(newPassword);

        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var passwordHash = _passwordService.HashPassword(newPassword);
        user.PasswordHash = passwordHash;
        user.HasPasswordSet = true;
        user.LastPasswordChangedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
    }

    /// <summary>
    /// Request password reset OTP
    /// </summary>
    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("No account found for this email.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var otp = GenerateOtp();
        var hash = HashOtp(otp);

        await _otpRepo.InvalidatePreviousAsync(email);

        await _otpRepo.CreateAsync(new OtpRequest
        {
            Email = email,
            OtpHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        await _otpService.SendOtpAsync(email, otp, "Password Reset");
    }

    /// <summary>
    /// Verify password reset OTP and set new password
    /// </summary>
    public async Task<AuthResponseDto> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
    {
        _passwordService.ValidatePasswordStrength(newPassword);

        var otpRequest = await _otpRepo.GetLatestValidAsync(email)
            ?? throw new UnauthorizedAccessException("Reset code expired or not found.");

        if (otpRequest.IsUsed)
            throw new UnauthorizedAccessException("Reset code already used.");

        if (otpRequest.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Reset code has expired.");

        if (!VerifyOtpHash(otp, otpRequest.OtpHash))
            throw new UnauthorizedAccessException("Invalid reset code.");

        await _otpRepo.MarkUsedAsync(otpRequest.Id);

        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("User not found.");

        var passwordHash = _passwordService.HashPassword(newPassword);
        user.PasswordHash = passwordHash;
        user.HasPasswordSet = true;
        user.LastPasswordChangedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);

        Doctor? doctor = null;
        if (user.Doctor != null)
            doctor = await _doctorRepo.GetByUserIdAsync(user.Id);

        var token = GenerateJwt(user, doctor);

        return new AuthResponseDto(
            Token: token,
            Email: user.Email,
            Name: user.Name,
            Role: user.Role.ToString(),
            DoctorId: doctor?.Id,
            DoctorSlug: doctor?.Slug,
            HasPasswordSet: user.HasPasswordSet
        );
    }
}
