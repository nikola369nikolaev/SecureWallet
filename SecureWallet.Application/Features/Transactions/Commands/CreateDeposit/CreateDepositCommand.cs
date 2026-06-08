namespace SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;

public class CreateDepositCommand
{
    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string TotpCode { get; set; } = string.Empty;
}
