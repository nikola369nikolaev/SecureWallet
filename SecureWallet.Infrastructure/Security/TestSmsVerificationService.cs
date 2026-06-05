using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class TestSmsVerificationService : ISmsVerificationService
{
    public Task<SmsVerificationDispatchResult> SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        SmsVerificationDispatchResult result = new()
        {
            Message = $"SMS code was generated for {phoneNumber}.",
            DevelopmentCodePreview = code
        };

        return Task.FromResult(result);
    }
}
