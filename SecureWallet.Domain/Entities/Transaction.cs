using SecureWallet.Domain.Enums;

namespace SecureWallet.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SenderWalletId { get; set; }

    public Guid ReceiverWalletId { get; set; }

    public decimal Amount { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public string Reference { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Wallet? SenderWallet { get; set; }

    public Wallet? ReceiverWallet { get; set; }
}
