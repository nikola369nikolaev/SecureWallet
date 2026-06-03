namespace SecureWallet.Application.Features.Auth.Exceptions;

public class LoginProtectionException : InvalidOperationException
{
    public LoginProtectionException(
        string message,
        bool requiresCaptcha = false,
        string? captchaImageBase64 = null,
        int? lockoutSeconds = null)
        : base(message)
    {
        RequiresCaptcha = requiresCaptcha;
        CaptchaImageBase64 = captchaImageBase64;
        LockoutSeconds = lockoutSeconds;
    }

    public bool RequiresCaptcha { get; }

    public string? CaptchaImageBase64 { get; }

    public int? LockoutSeconds { get; }
}
