namespace SecureWallet.Application.Features.Auth.Exceptions;

public class LoginProtectionException : InvalidOperationException
{
    public LoginProtectionException(
        string message,
        bool requiresCaptcha = false,
        bool requiresTotp = false,
        string? captchaImageBase64 = null,
        int? lockoutSeconds = null,
        bool requiresEmailVerification = false,
        string? email = null)
        : base(message)
    {
        RequiresCaptcha = requiresCaptcha;
        RequiresTotp = requiresTotp;
        CaptchaImageBase64 = captchaImageBase64;
        LockoutSeconds = lockoutSeconds;
        RequiresEmailVerification = requiresEmailVerification;
        Email = email;
    }

    public bool RequiresCaptcha { get; }

    public bool RequiresTotp { get; }

    public bool RequiresEmailVerification { get; }

    public string? Email { get; }

    public string? CaptchaImageBase64 { get; }

    public int? LockoutSeconds { get; }
}
