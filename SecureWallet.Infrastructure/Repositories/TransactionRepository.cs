using Microsoft.EntityFrameworkCore;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Infrastructure.Data;

namespace SecureWallet.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _appDbContext;

    public TransactionRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.SenderWallet)
            .Include(transaction => transaction.ReceiverWallet)
            .FirstOrDefaultAsync(transaction => transaction.Id == transactionId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.SenderWallet)
            .Include(transaction => transaction.ReceiverWallet)
            .Where(transaction =>
                transaction.SenderWalletId == walletId ||
                transaction.ReceiverWalletId == walletId)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _appDbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _appDbContext.Transactions.Update(transaction);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
