namespace SecureWallet.Application.Features.Transactions.DTOs;

public class DepositResultDto
{
    public string Message { get; set; } = string.Empty;

    public Guid TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal UpdatedBalance { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
