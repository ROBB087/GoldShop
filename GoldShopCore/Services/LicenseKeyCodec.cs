using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public static class LicenseKeyCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string CreateKey(AppLicense license, string privateKeyXml)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(license, JsonOptions));

        using var rsa = RSA.Create();
        rsa.FromXmlString(privateKeyXml);
        var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{ToBase64Url(payloadBytes)}.{ToBase64Url(signature)}";
    }

    public static bool TryReadKey(string key, string publicKeyXml, out AppLicense? license, out string error)
    {
        license = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(key))
        {
            error = "License key is empty.";
            return false;
        }

        var parts = key.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            error = "License key format is invalid.";
            return false;
        }

        byte[] payloadBytes;
        byte[] signatureBytes;

        try
        {
            payloadBytes = FromBase64Url(parts[0]);
            signatureBytes = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            error = "License key format is invalid.";
            return false;
        }

        using var rsa = RSA.Create();
        rsa.FromXmlString(publicKeyXml);
        if (!rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        {
            error = "License signature is invalid.";
            return false;
        }

        try
        {
            license = JsonSerializer.Deserialize<AppLicense>(payloadBytes, JsonOptions);
        }
        catch (JsonException)
        {
            error = "License payload is invalid.";
            return false;
        }

        if (license == null)
        {
            error = "License payload is invalid.";
            return false;
        }

        return true;
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4)
        {
            base64 = base64.PadRight(base64.Length + padding, '=');
        }

        return Convert.FromBase64String(base64);
    }
}
