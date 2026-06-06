namespace SecureWallet.Application.Interfaces.Security;

public interface IEmailVerificationSender
{
    Task SendRegistrationVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}
