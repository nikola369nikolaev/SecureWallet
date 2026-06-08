namespace SecureWallet.Application.Features.Transactions.DTOs;

public class TransactionHistoryItemDto
{
    public Guid TransactionId { get; set; }

    public bool IsIncoming { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string CounterpartyUsername { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
