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

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string upperEmail = email.ToUpperInvariant();

        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email.ToUpper() == upperEmail, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        string upperUsername = username.ToUpperInvariant();

        return await _appDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Username.ToUpper() == upperUsername, cancellationToken);
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
