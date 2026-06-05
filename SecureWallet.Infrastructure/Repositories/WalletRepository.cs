using Microsoft.EntityFrameworkCore;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Infrastructure.Data;

namespace SecureWallet.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _appDbContext;

    public WalletRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Wallets
            .AsNoTracking()
            .Include(wallet => wallet.User)
            .ThenInclude(user => user!.Role)
            .FirstOrDefaultAsync(wallet => wallet.Id == walletId, cancellationToken);
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Wallets
            .AsNoTracking()
            .Include(wallet => wallet.User)
            .ThenInclude(user => user!.Role)
            .FirstOrDefaultAsync(wallet => wallet.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _appDbContext.Wallets.AddAsync(wallet, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _appDbContext.Wallets.Update(wallet);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
