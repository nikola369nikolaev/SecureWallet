namespace SecureWallet.Application.Features.Admin.DTOs;

public class AdminUserDetailsDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Guid WalletId { get; set; }

    public decimal WalletBalance { get; set; }

    public string WalletCurrency { get; set; } = string.Empty;

    public bool WalletIsActive { get; set; }

    public DateTime WalletCreatedAtUtc { get; set; }
}
