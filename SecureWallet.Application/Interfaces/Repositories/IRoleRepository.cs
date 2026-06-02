using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
