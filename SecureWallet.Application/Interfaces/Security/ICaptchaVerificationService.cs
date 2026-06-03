namespace SecureWallet.Application.Interfaces.Security;

public interface ICaptchaVerificationService
{
    string GenerateCaptchaCode();

    bool IsValid(string? providedCaptchaToken, string expectedCaptchaCode);
}
