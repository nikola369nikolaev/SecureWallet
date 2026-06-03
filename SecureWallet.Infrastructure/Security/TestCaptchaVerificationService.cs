using SecureWallet.Application.Interfaces.Security;
using System.Globalization;
using System.Security.Cryptography;

namespace SecureWallet.Infrastructure.Security;

public class TestCaptchaVerificationService : ICaptchaVerificationService
{
    public string GenerateCaptchaCode()
    {
        int code = RandomNumberGenerator.GetInt32(1000, 10000);
        return code.ToString(CultureInfo.InvariantCulture);
    }

    public bool IsValid(string? providedCaptchaToken, string expectedCaptchaCode)
    {
        return !string.IsNullOrWhiteSpace(providedCaptchaToken) &&
               providedCaptchaToken == expectedCaptchaCode;
    }
}
