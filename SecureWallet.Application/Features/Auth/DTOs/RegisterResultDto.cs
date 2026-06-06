namespace SecureWallet.Application.Features.Auth.DTOs;

public class RegisterResultDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool RequiresEmailVerification { get; set; }

    public bool SecuritySetupRequired { get; set; }

    public string? SetupAccessToken { get; set; }

    public DateTime? SetupAccessTokenExpiresAtUtc { get; set; }
}
