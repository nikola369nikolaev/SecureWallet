namespace SecureWallet.Application.Features.Auth.DTOs;

public class TotpVerificationResultDto
{
    public string Message { get; set; } = string.Empty;

    public bool TwoFactorEnabled { get; set; }

    public string? AccessToken { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public Guid? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public bool SecuritySetupRequired { get; set; }
}
