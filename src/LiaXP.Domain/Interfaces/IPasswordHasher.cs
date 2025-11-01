namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Service for hashing and verifying passwords
/// Uses PBKDF2 with HMACSHA256 (recommended by OWASP)
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password with salt</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a plain text password against a hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password with salt</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
