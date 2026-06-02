using Microsoft.EntityFrameworkCore;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Infrastructure.Data;

namespace SecureWallet.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _appDbContext;

    public RoleRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }
}
