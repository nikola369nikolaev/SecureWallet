namespace SecureWallet.API.Requests.Transactions;

public class CreateTransferRequest
{
    public string RecipientType { get; set; } = string.Empty;

    public string RecipientValue { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}
