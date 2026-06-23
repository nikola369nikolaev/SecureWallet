namespace SecureWallet.Application.Interfaces.Security;

public interface ITotpSecretProtector
{
    string Protect(string secret);

    string Unprotect(string protectedSecret);
}
