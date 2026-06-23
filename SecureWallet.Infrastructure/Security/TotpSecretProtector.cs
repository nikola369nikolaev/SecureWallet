using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class TotpSecretProtector : ITotpSecretProtector
{
    private const string Prefix = "enc1:";
    private readonly byte[] _key;
    private readonly ILogger<TotpSecretProtector> _logger;

    public TotpSecretProtector(IConfiguration configuration, ILogger<TotpSecretProtector> logger)
    {
        string sourceKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(sourceKey));
        _logger = logger;
    }

    public string Protect(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");
        }

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] plainBytes = Encoding.UTF8.GetBytes(secret);
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        byte[] payload = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
        {
            throw new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");
        }

        if (!protectedSecret.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return protectedSecret;
        }

        try
        {
            byte[] payload = Convert.FromBase64String(protectedSecret[Prefix.Length..]);
            byte[] iv = payload[..16];
            byte[] cipherBytes = payload[16..];

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Неуспешно разкриптиране на TOTP secret.");
            throw new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");
        }
    }
}
