using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Transactions.Queries.GetCurrentUserTransactionHistory;

public class GetCurrentUserTransactionHistoryHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetCurrentUserTransactionHistoryHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionHistoryPageDto> Handle(
        Guid userId,
        TransactionHistoryQueryParametersDto queryParameters,
        CancellationToken cancellationToken = default)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("Портфейлът на текущия потребител не беше намерен.");
        }

        TransactionHistoryQueryParametersDto normalizedQueryParameters = new()
        {
            Type = string.IsNullOrWhiteSpace(queryParameters.Type) ? "All" : queryParameters.Type,
            DateRange = string.IsNullOrWhiteSpace(queryParameters.DateRange) ? "All" : queryParameters.DateRange,
            Month = queryParameters.Month,
            Year = queryParameters.Year,
            SearchTerm = queryParameters.SearchTerm ?? string.Empty,
            Page = queryParameters.Page <= 0 ? 1 : queryParameters.Page,
            PageSize = queryParameters.PageSize <= 0 ? 20 : Math.Min(queryParameters.PageSize, 50)
        };

        if (string.Equals(normalizedQueryParameters.DateRange, "Month", StringComparison.OrdinalIgnoreCase))
        {
            DateTime now = DateTime.UtcNow;

            normalizedQueryParameters.Month = normalizedQueryParameters.Month is >= 1 and <= 12
                ? normalizedQueryParameters.Month
                : now.Month;

            normalizedQueryParameters.Year = normalizedQueryParameters.Year is > 0
                ? normalizedQueryParameters.Year
                : now.Year;
        }

        return await _transactionRepository.GetHistoryPageAsync(
            wallet.Id,
            wallet.Currency,
            normalizedQueryParameters,
            cancellationToken);
    }
}
