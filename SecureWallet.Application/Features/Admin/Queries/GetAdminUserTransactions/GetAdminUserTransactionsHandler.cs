using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Domain.Enums;

namespace SecureWallet.Application.Features.Admin.Queries.GetAdminUserTransactions;

public class GetAdminUserTransactionsHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetAdminUserTransactionsHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyCollection<TransactionHistoryItemDto>> Handle(Guid userId, CancellationToken cancellationToken = default)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("Портфейлът на потребителя не беше намерен.");
        }

        IReadOnlyCollection<Transaction> transactions = await _transactionRepository.GetByWalletIdAsync(wallet.Id, cancellationToken);

        return transactions
            .Select(transaction => MapTransaction(wallet, transaction))
            .ToList();
    }

    private static TransactionHistoryItemDto MapTransaction(Wallet currentWallet, Transaction transaction)
    {
        bool isIncoming = transaction.ReceiverWalletId == currentWallet.Id;
        Wallet? counterpartyWallet = isIncoming ? transaction.SenderWallet : transaction.ReceiverWallet;

        return new TransactionHistoryItemDto
        {
            TransactionId = transaction.Id,
            IsIncoming = isIncoming,
            Amount = transaction.Amount,
            Currency = currentWallet.Currency,
            CounterpartyUsername = counterpartyWallet?.User?.Username ?? string.Empty,
            Description = transaction.Description ?? string.Empty,
            Status = FormatStatus(transaction.Status),
            Reference = transaction.Reference,
            CreatedAtUtc = transaction.CreatedAtUtc
        };
    }

    private static string FormatStatus(TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.Pending => "Чакаща",
            TransactionStatus.Completed => "Завършен",
            TransactionStatus.Failed => "Неуспешен",
            TransactionStatus.Cancelled => "Отказан",
            _ => status.ToString()
        };
    }
}
