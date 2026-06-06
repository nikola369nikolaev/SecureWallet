using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Infrastructure.Data;
using SecureWallet.Infrastructure.Repositories;
using SecureWallet.Infrastructure.Security;

namespace SecureWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("SecureWalletDb"));

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICaptchaVerificationService, TestCaptchaVerificationService>();
        services.AddScoped<ISmsVerificationService, TestSmsVerificationService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IQrCodeService, QrCodeService>();

        return services;
    }
}
