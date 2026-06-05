using SecureWallet.Application.Features.Wallets.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Wallets.Queries.GetCurrentUserWallet;

public class GetCurrentUserWalletHandler
{
    private readonly IWalletRepository _walletRepository;

    public GetCurrentUserWalletHandler(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<WalletSummaryDto> Handle(Guid userId, CancellationToken cancellationToken = default)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);

        if (wallet is null)
        {
            throw new InvalidOperationException("Wallet for the current user was not found.");
        }

        return new WalletSummaryDto
        {
            WalletId = wallet.Id,
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            IsActive = wallet.IsActive,
            CreatedAtUtc = wallet.CreatedAtUtc,
            UpdatedAtUtc = wallet.UpdatedAtUtc,
            Username = wallet.User?.Username ?? string.Empty,
            Email = wallet.User?.Email ?? string.Empty,
            Role = wallet.User?.Role?.Name ?? string.Empty
        };
    }
}
