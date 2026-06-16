using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Infrastructure.Data;
using SecureWallet.Infrastructure.Email;
using SecureWallet.Infrastructure.Options;
using SecureWallet.Infrastructure.Repositories;
using SecureWallet.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace SecureWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string defaultConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не беше намерен connection string 'DefaultConnection'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(defaultConnection));

        services.Configure<EmailOptions>(configuration.GetSection("Email"));

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
        services.AddScoped<IEmailVerificationSender, SmtpEmailVerificationSender>();

        return services;
    }
}
