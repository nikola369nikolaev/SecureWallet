namespace SecureWallet.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;

    public string Iban { get; set; } = string.Empty;

    public string CardNumber { get; set; } = string.Empty;

    public string CardCvv { get; set; } = string.Empty;

    public DateTime CardCreatedAtUtc { get; set; }

    public DateTime CardExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();

    public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
}
