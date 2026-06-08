namespace SecureWallet.Application.Features.Admin.DTOs;

public class SupportAccountResultDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
