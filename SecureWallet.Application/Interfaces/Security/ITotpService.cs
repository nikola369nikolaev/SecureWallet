namespace SecureWallet.Application.Interfaces.Security;

public interface ITotpService
{
    string GenerateSecret();

    string BuildSetupCodeUri(string issuer, string accountName, string secret);

    bool VerifyCode(string secret, string code);
}
