namespace SecureWallet.Application.Features.Transactions.DTOs;

public class TransactionHistoryPageDto
{
    public IReadOnlyCollection<TransactionHistoryItemDto> Items { get; set; } = [];

    public TransactionHistorySummaryDto Summary { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public bool HasMore { get; set; }
}
