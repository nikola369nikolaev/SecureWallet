using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Data.Seed;

public static class RoleSeeder
{
    public static async Task SeedDefaultRolesAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureRoleAsync(appDbContext, "User", "Default role for registered users.");
        await EnsureRoleAsync(appDbContext, "Admin", "Administrative role with management permissions.");
        await EnsureRoleAsync(appDbContext, "Support", "Read-only support role for user and transaction review.");
    }

    private static async Task EnsureRoleAsync(AppDbContext appDbContext, string roleName, string description)
    {
        bool hasRole = await appDbContext.Roles
            .AsNoTracking()
            .AnyAsync(role => role.Name == roleName);

        if (hasRole)
        {
            return;
        }

        Role role = new()
        {
            Name = roleName,
            Description = description
        };

        await appDbContext.Roles.AddAsync(role);
        await appDbContext.SaveChangesAsync();
    }
}
