using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Infrastructure.Data;
using SecureWallet.Infrastructure.Repositories;

namespace SecureWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }
}
