namespace SecureWallet.Application.Features.Transactions.Commands.CreateTransfer;

public class CreateTransferCommand
{
    public Guid SenderUserId { get; set; }

    public string RecipientType { get; set; } = string.Empty;

    public string RecipientValue { get; set; } = string.Empty;

    public string TotpCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}
