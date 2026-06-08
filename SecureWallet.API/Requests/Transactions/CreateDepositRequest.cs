namespace SecureWallet.API.Requests.Transactions;

public class CreateDepositRequest
{
    public decimal Amount { get; set; }

    public string TotpCode { get; set; } = string.Empty;
}
