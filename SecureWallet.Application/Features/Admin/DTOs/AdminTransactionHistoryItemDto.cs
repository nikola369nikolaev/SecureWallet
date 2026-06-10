namespace SecureWallet.Application.Features.Admin.DTOs;

public class AdminTransactionHistoryItemDto
{
    public Guid TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public string SenderUsername { get; set; } = string.Empty;

    public string ReceiverUsername { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
