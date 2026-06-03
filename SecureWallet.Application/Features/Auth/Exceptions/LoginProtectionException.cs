namespace SecureWallet.Application.Features.Auth.Exceptions;

public class LoginProtectionException : InvalidOperationException
{
    public LoginProtectionException(
        string message,
        bool requiresCaptcha = false,
        string? captchaCode = null,
        int? lockoutSeconds = null)
        : base(message)
    {
        RequiresCaptcha = requiresCaptcha;
        CaptchaCode = captchaCode;
        LockoutSeconds = lockoutSeconds;
    }

    public bool RequiresCaptcha { get; }

    public string? CaptchaCode { get; }

    public int? LockoutSeconds { get; }
}
