namespace SecureWallet.Application.Features.Admin.DTOs;

public class AdminTransactionHistorySummaryDto
{
    public int TransferCount { get; set; }

    public decimal TransferTotal { get; set; }

    public int DepositCount { get; set; }

    public decimal DepositTotal { get; set; }

    public int OperationCount { get; set; }

    public int VisibleUserCount { get; set; }

    public string Currency { get; set; } = "EUR";
}
