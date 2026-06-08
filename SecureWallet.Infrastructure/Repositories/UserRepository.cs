using Microsoft.EntityFrameworkCore;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Infrastructure.Data;

namespace SecureWallet.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _appDbContext;

    public UserRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string lowerEmail = email.ToLowerInvariant();

        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email.ToLower() == lowerEmail, cancellationToken);
    }

    public async Task<User?> GetByEmailAndPhoneNumberAsync(string email, string phoneNumber, CancellationToken cancellationToken = default)
    {
        string lowerEmail = email.ToLowerInvariant();

        return await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email.ToLower() == lowerEmail && user.PhoneNumber == phoneNumber,
                cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<int> CountByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .CountAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        string lowerUsername = username.ToLowerInvariant();

        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Username.ToLower() == lowerUsername, cancellationToken);
    }

    public async Task<User?> GetByPasswordResetSessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.PasswordResetSessionToken == sessionToken, cancellationToken);
    }

    public async Task AddWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _appDbContext.Users.AddAsync(user, cancellationToken);
        await _appDbContext.Wallets.AddAsync(wallet, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _appDbContext.Users.AddAsync(user, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _appDbContext.Users.Update(user);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
