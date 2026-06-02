using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);

    Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
