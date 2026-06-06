namespace SecureWallet.Application.Features.Auth.DTOs;

public class EmailVerificationCodeDispatchResultDto
{
    public string Message { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
