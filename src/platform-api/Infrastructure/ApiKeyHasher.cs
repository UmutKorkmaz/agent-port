using System.Security.Cryptography;
using System.Text;

namespace AgentPort.PlatformApi.Infrastructure;

public static class ApiKeyHasher
{
    public static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"ap_local_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    public static string GetPrefix(string rawKey)
    {
        return rawKey[..Math.Min(24, rawKey.Length)];
    }

    public static string Hash(string rawKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
