namespace SecureWallet.Application.Features.Transactions.DTOs;

public class TransactionHistorySummaryDto
{
    public int IncomingCount { get; set; }

    public decimal IncomingTotal { get; set; }

    public int OutgoingCount { get; set; }

    public decimal OutgoingTotal { get; set; }

    public int DepositCount { get; set; }

    public decimal DepositTotal { get; set; }

    public string Currency { get; set; } = string.Empty;
}
