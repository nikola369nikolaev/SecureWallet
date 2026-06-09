namespace SecureWallet.Application.Interfaces.Security;

public interface ISmsVerificationService
{
    Task<SmsVerificationDispatchResult> SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}

public class SmsVerificationDispatchResult
{
    public string Message { get; set; } = string.Empty;
}
