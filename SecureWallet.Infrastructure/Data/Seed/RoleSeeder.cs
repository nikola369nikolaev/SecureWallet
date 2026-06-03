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

        bool hasUserRole = await appDbContext.Roles
            .AsNoTracking()
            .AnyAsync(role => role.Name == "User");

        if (hasUserRole)
        {
            return;
        }

        Role userRole = new()
        {
            Name = "User",
            Description = "Default role for registered users."
        };

        await appDbContext.Roles.AddAsync(userRole);
        await appDbContext.SaveChangesAsync();
    }
}
