using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Admin.Queries.GetAdminUserDetails;

public class GetAdminUserDetailsHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;

    public GetAdminUserDetailsHandler(IUserRepository userRepository, IWalletRepository walletRepository)
    {
        _userRepository = userRepository;
        _walletRepository = walletRepository;
    }

    public async Task<AdminUserDetailsDto> Handle(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("Портфейлът на потребителя не беше намерен.");
        }

        return new AdminUserDetailsDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            WalletId = wallet.Id,
            WalletBalance = wallet.Balance,
            WalletCurrency = wallet.Currency,
            WalletIsActive = wallet.IsActive,
            WalletCreatedAtUtc = wallet.CreatedAtUtc
        };
    }
}
