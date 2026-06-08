namespace SecureWallet.Infrastructure.Options;

public class EmailOptions
{
    public string SmtpClient { get; set; } = string.Empty;

    public int SmtpPort { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "SecureWallet";
}
