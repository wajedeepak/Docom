using System.Security.Cryptography;
using System.Text;

namespace Docom.Application.Services;

public interface IPasswordService
{
    /// <summary>
    /// Hash a password using PBKDF2
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against its PBKDF2 hash
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Validate password strength (minimum 6 characters)
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if password doesn't meet requirements</exception>
    void ValidatePasswordStrength(string password);
}

public class PasswordService : IPasswordService
{
    private const int MinPasswordLength = 6;
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 10000; // PBKDF2 iterations

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        // Generate random salt
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            // Derive password hash
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Combine salt and hash into one array for storage
                byte[] hashBytes = new byte[SaltSize + HashSize];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
                Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, HashSize);

                // Convert to base64 for storage
                return Convert.ToBase64String(hashBytes);
            }
        }
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            // Decode the hash
            byte[] hashBytes = Convert.FromBase64String(passwordHash);

            // Extract salt and hash
            if (hashBytes.Length != SaltSize + HashSize)
                return false;

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            // Derive hash from provided password using extracted salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Compare hashes
                byte[] storedHash = new byte[HashSize];
                Buffer.BlockCopy(hashBytes, SaltSize, storedHash, 0, HashSize);

                return CryptographicOperations.FixedTimeEquals(hash, storedHash);
            }
        }
        catch
        {
            return false;
        }
    }

    public void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        if (password.Length < MinPasswordLength)
            throw new ArgumentException(
                $"Password must be at least {MinPasswordLength} characters long.",
                nameof(password));
    }
}
