namespace SecureWallet.Application.Features.Auth.DTOs;

public class PasswordResetCodeVerificationResultDto
{
    public string Message { get; set; } = string.Empty;

    public string ResetSessionToken { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
