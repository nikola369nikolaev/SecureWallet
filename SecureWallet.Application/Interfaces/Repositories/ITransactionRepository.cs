using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<TransactionHistoryPageDto> GetHistoryPageAsync(
        Guid walletId,
        string currency,
        TransactionHistoryQueryParametersDto queryParameters,
        CancellationToken cancellationToken = default);

    Task CreateCompletedTransferAsync(
        Transaction transaction,
        Wallet senderWallet,
        Wallet receiverWallet,
        CancellationToken cancellationToken = default);

    Task CreateCompletedDepositAsync(
        Transaction transaction,
        Wallet wallet,
        CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
