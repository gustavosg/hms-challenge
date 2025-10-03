using System.Security.Cryptography;

namespace Shared.Utils;

public static class PasswordHelper
{
    /// <summary>
    /// Generates a salted hash for the specified password using PBKDF2 with SHA-256.
    /// </summary>
    /// <remarks>The returned string can be stored for later password verification. The salt is randomly
    /// generated for each call, ensuring that the same password will produce different hashes. This method uses 100,000
    /// iterations of PBKDF2 with SHA-256 for key derivation.</remarks>
    /// <param name="password">The password to hash. Cannot be null.</param>
    /// <returns>A string containing the Base64-encoded salt and hash, separated by a colon.</returns>
    public static string Hash(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        byte[] hashBytes = pbkdf2.GetBytes(32);
        return $"{Convert.ToBase64String(saltBytes)}:{Convert.ToBase64String(hashBytes)}";
    }

    /// <summary>
    /// Verifies if the provided password matches the stored hash.
    /// </summary>
    /// <param name="password">Password from the request </param>
    /// <param name="passwordHash">Hash stored in database</param>
    /// <returns>Is a match</returns>
    public static bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split(':');
        if (parts.Length != 2)
            return false;

        byte[] saltBytes = Convert.FromBase64String(parts[0]);
        byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

        var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        byte[] computedHash = pbkdf2.GetBytes(32);

        // Comparação segura
        return CryptographicOperations.FixedTimeEquals(storedHashBytes, computedHash);
    }
}
