namespace SecureWallet.Application.Interfaces.Security;

public interface ICaptchaVerificationService
{
    string GenerateCaptchaCode();

    string GenerateCaptchaImageBase64(string captchaCode);

    bool IsValid(string? providedCaptchaToken, string expectedCaptchaCode);
}
