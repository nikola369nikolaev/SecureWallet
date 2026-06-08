using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAndPhoneNumberAsync(string email, string phoneNumber, CancellationToken cancellationToken = default);

    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<int> CountByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByPasswordResetSessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task AddWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
