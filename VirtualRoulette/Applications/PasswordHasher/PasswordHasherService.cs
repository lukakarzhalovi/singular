using System.Security.Cryptography;
using System.Text;

namespace VirtualRoulette.Applications.PasswordHasher;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public class PasswordHasherServiceService : IPasswordHasherService
{
    private const int SaltSize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var hash = ComputeHash(password, salt, Iterations);
        
        return $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            var parts = passwordHash.Split(':');
            if (parts.Length != 3)
                return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var storedHash = Convert.FromBase64String(parts[2]);

            var computedHash = ComputeHash(password, salt, iterations);
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt, int iterations)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        
        var hash = sha256.ComputeHash(combined);

        for (var i = 1; i < iterations; i++)
        {
            var temp = new byte[hash.Length + salt.Length];
            Buffer.BlockCopy(hash, 0, temp, 0, hash.Length);
            Buffer.BlockCopy(salt, 0, temp, hash.Length, salt.Length);
            hash = sha256.ComputeHash(temp);
        }

        return hash;
    }
}
