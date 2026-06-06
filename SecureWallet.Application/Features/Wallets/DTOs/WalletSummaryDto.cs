namespace SecureWallet.Application.Features.Wallets.DTOs;

public class WalletSummaryDto
{
    public Guid WalletId { get; set; }

    public decimal Balance { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }
}
