using System.Security.Cryptography;
using System.Text;

namespace GoldShopCore.Services;

public static class TokenHasher
{
    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hashBytes);
    }
}
