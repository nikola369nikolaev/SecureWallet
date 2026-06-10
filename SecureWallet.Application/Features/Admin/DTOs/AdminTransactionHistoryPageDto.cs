namespace SecureWallet.Application.Features.Admin.DTOs;

public class AdminTransactionHistoryPageDto
{
    public IReadOnlyCollection<AdminTransactionHistoryItemDto> Items { get; set; } = [];

    public AdminTransactionHistorySummaryDto Summary { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public bool HasMore { get; set; }
}
