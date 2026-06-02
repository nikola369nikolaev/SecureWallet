using Microsoft.AspNetCore.Identity;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _passwordHasher;

    public PasswordHasher()
    {
        _passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
    }

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(new User(), password);
    }

    public bool Verify(string password, string hashedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
