using System.Security.Cryptography;
using LiaXP.Domain.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LiaXP.Infrastructure.Services;

/// <summary>
/// Implementation of password hasher using PBKDF2-HMACSHA256
/// Format: {iterations}.{salt}.{hash}
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 128 / 8; // 128 bits
    private const int HashSize = 256 / 8; // 256 bits
    private const int Iterations = 100000; // OWASP recommended minimum

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // Generate a random salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash the password with the salt
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize
        );

        // Format: {iterations}.{salt}.{hash}
        // This allows for future algorithm upgrades
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        if (string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            // Parse the hashed password
            var parts = hashedPassword.Split('.');
            if (parts.Length != 3)
                return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            // Hash the provided password with the same salt
            byte[] computedHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: hash.Length
            );

            // Compare hashes in constant time to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, hash);
        }
        catch
        {
            // Invalid format or conversion error
            return false;
        }
    }
}