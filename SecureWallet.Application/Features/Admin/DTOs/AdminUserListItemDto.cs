namespace SecureWallet.Application.Features.Admin.DTOs;

public class AdminUserListItemDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
