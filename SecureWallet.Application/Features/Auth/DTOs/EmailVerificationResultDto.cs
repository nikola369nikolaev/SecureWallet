namespace SecureWallet.Application.Features.Auth.DTOs;

public class EmailVerificationResultDto
{
    public string Message { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool TwoFactorEnabled { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool SecuritySetupRequired { get; set; }
}
