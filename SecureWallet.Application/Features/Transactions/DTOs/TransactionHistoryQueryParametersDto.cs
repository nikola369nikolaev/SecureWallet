namespace SecureWallet.Application.Features.Transactions.DTOs;

public class TransactionHistoryQueryParametersDto
{
    public string Type { get; set; } = "All";

    public string DateRange { get; set; } = "All";

    public int? Month { get; set; }

    public int? Year { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
