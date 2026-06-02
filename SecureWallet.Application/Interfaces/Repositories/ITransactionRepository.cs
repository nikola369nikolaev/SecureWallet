using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
