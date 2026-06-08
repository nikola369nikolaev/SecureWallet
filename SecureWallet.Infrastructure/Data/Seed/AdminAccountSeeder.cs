using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Features.Wallets;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Data.Seed;

public static class AdminAccountSeeder
{
    private const string AdminRoleName = "Admin";
    private const string AdminUsername = "admin";
    private const string AdminEmail = "secure.wallet-admin@abv.bg";
    private const string AdminPassword = "Admin123";

    public static async Task SeedAdminAccountAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Role? adminRole = await appDbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Name == AdminRoleName);

        if (adminRole is null)
        {
            throw new InvalidOperationException("Ролята Admin не беше намерена по време на seed.");
        }

        User? existingAdmin = await appDbContext.Users
            .FirstOrDefaultAsync(user => user.Email == AdminEmail || user.Username == AdminUsername);

        if (existingAdmin is null)
        {
            User adminUser = new()
            {
                Username = AdminUsername,
                Email = AdminEmail,
                Password = passwordHasher.Hash(AdminPassword),
                FirstName = "Admin",
                LastName = "Secure Wallet",
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                RoleId = adminRole.Id
            };

            Wallet adminWallet = new()
            {
                UserId = adminUser.Id,
                Balance = 0m,
                Currency = "EUR",
                IsActive = true
            };

            WalletCardGenerator.ApplyNewCardDetails(adminWallet);

            await appDbContext.Users.AddAsync(adminUser);
            await appDbContext.Wallets.AddAsync(adminWallet);
            await appDbContext.SaveChangesAsync();
            return;
        }

        Wallet? existingWallet = await appDbContext.Wallets
            .FirstOrDefaultAsync(wallet => wallet.UserId == existingAdmin.Id);

        bool requiresUpdate = false;

        if (existingAdmin.RoleId != adminRole.Id)
        {
            existingAdmin.RoleId = adminRole.Id;
            requiresUpdate = true;
        }

        if (!string.Equals(existingAdmin.Email, AdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            existingAdmin.Email = AdminEmail;
            requiresUpdate = true;
        }

        if (!existingAdmin.IsEmailVerified)
        {
            existingAdmin.IsEmailVerified = true;
            existingAdmin.EmailVerificationCodeHash = null;
            existingAdmin.EmailVerificationCodeExpiresAtUtc = null;
            requiresUpdate = true;
        }

        if (existingWallet is null)
        {
            Wallet wallet = new()
            {
                UserId = existingAdmin.Id,
                Balance = 0m,
                Currency = "EUR",
                IsActive = true
            };

            WalletCardGenerator.ApplyNewCardDetails(wallet);
            await appDbContext.Wallets.AddAsync(wallet);
            requiresUpdate = true;
        }
        else
        {
            WalletCardGenerator.EnsureCardDetails(existingWallet);
            requiresUpdate = true;
        }

        if (requiresUpdate)
        {
            existingAdmin.UpdatedAtUtc = DateTime.UtcNow;
            await appDbContext.SaveChangesAsync();
        }
    }
}
