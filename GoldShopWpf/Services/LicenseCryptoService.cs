using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GoldShopWpf.Services;

internal static class LicenseCryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] Encrypt<T>(T value, string purpose, string machineId)
    {
        var plainBytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var cipherBytes = new byte[plainBytes.Length];
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var associatedData = Encoding.UTF8.GetBytes($"{LicenseConstants.ProductCode}|{purpose}");

        using var aes = new AesGcm(DeriveKey(machineId, purpose), TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag, associatedData);

        var payload = new byte[1 + NonceSize + TagSize + cipherBytes.Length];
        payload[0] = 1;
        Buffer.BlockCopy(nonce, 0, payload, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, 1 + NonceSize + TagSize, cipherBytes.Length);
        return payload;
    }

    public static T Decrypt<T>(byte[] payload, string purpose, string machineId)
    {
        if (payload.Length < 1 + NonceSize + TagSize || payload[0] != 1)
        {
            throw new CryptographicException("License payload header is invalid.");
        }

        var nonce = payload.AsSpan(1, NonceSize);
        var tag = payload.AsSpan(1 + NonceSize, TagSize);
        var cipherBytes = payload.AsSpan(1 + NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];
        var associatedData = Encoding.UTF8.GetBytes($"{LicenseConstants.ProductCode}|{purpose}");

        using var aes = new AesGcm(DeriveKey(machineId, purpose), TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData);

        return JsonSerializer.Deserialize<T>(plainBytes)
            ?? throw new CryptographicException("License payload content is invalid.");
    }

    private static byte[] DeriveKey(string machineId, string purpose)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes($"{LicenseConstants.AppSecret}|{LicenseConstants.ProductCode}|{purpose}|{machineId}"));
    }
}
