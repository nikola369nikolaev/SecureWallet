namespace SecureWallet.Application.Features.Auth.Commands.Login;

public class LoginUserCommand
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? CaptchaToken { get; set; }
}
