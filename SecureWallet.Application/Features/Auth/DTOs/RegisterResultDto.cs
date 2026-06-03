namespace SecureWallet.Application.Features.Auth.DTOs;

public class RegisterResultDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
