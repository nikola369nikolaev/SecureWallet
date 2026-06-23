using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class TotpService : ITotpService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretSizeBytes = 20;
    private const int TotpDigits = 6;
    private const int TotpPeriodSeconds = 30;
    private const int AllowedTimeStepDrift = 1;
    private readonly ILogger<TotpService> _logger;

    public TotpService(ILogger<TotpService> logger)
    {
        _logger = logger;
    }

    public string GenerateSecret()
    {
        byte[] secretBytes = RandomNumberGenerator.GetBytes(SecretSizeBytes);
        return EncodeBase32(secretBytes);
    }

    public string BuildSetupCodeUri(string issuer, string accountName, string secret)
    {
        string encodedIssuer = Uri.EscapeDataString(issuer);
        string encodedAccountName = Uri.EscapeDataString(accountName);

        return $"otpauth://totp/{encodedIssuer}:{encodedAccountName}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={TotpDigits}&period={TotpPeriodSeconds}";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        string normalizedCode = code.Replace(" ", string.Empty).Trim();
        if (normalizedCode.Length != TotpDigits || !normalizedCode.All(char.IsDigit))
        {
            return false;
        }

        byte[] secretBytes = DecodeBase32(secret);
        long currentTimeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpPeriodSeconds;

        for (int offset = -AllowedTimeStepDrift; offset <= AllowedTimeStepDrift; offset++)
        {
            string generatedCode = GenerateTotpCode(secretBytes, currentTimeStep + offset);
            if (generatedCode == normalizedCode)
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateTotpCode(byte[] secretBytes, long timeStep)
    {
        byte[] counterBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        byte[] paddedCounter = new byte[8];
        Buffer.BlockCopy(counterBytes, 0, paddedCounter, 8 - counterBytes.Length, counterBytes.Length);

        using HMACSHA1 hmac = new(secretBytes);
        byte[] hash = hmac.ComputeHash(paddedCounter);

        int offset = hash[^1] & 0x0F;
        int binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        int otp = binaryCode % (int)Math.Pow(10, TotpDigits);
        return otp.ToString(new string('0', TotpDigits));
    }

    private static string EncodeBase32(byte[] data)
    {
        StringBuilder result = new();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (byte value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                result.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 0x1F]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            result.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return result.ToString();
    }

    private byte[] DecodeBase32(string input)
    {
        string normalizedInput = input
            .Trim()
            .TrimEnd('=')
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        List<byte> result = new();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char character in normalizedInput)
        {
            int index = Base32Alphabet.IndexOf(character);
            if (index < 0)
            {
                _logger.LogError("Тайната за временния код съдържа невалидни Base32 символи.");
                throw new InvalidOperationException("Възникна проблем. Опитай по-късно.");
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return result.ToArray();
    }
}
