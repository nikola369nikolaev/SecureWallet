using Microsoft.EntityFrameworkCore;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Domain.Entities;
using SecureWallet.Domain.Enums;
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
            .ThenInclude(wallet => wallet!.User)
            .Include(transaction => transaction.ReceiverWallet)
            .ThenInclude(wallet => wallet!.User)
            .Where(transaction =>
                transaction.SenderWalletId == walletId ||
                transaction.ReceiverWalletId == walletId)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionHistoryPageDto> GetHistoryPageAsync(
        Guid walletId,
        string currency,
        TransactionHistoryQueryParametersDto queryParameters,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> walletTransactionsQuery = _appDbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.SenderWalletId == walletId ||
                transaction.ReceiverWalletId == walletId);

        IQueryable<Transaction> baseFilteredQuery = ApplyDateRangeFilter(
            ApplySearchFilter(walletTransactionsQuery, queryParameters.SearchTerm),
            queryParameters.DateRange);

        TransactionHistorySummaryDto summary = new()
        {
            IncomingCount = await baseFilteredQuery.CountAsync(
                transaction => transaction.ReceiverWalletId == walletId && transaction.SenderWalletId != walletId,
                cancellationToken),
            IncomingTotal = await baseFilteredQuery
                .Where(transaction => transaction.ReceiverWalletId == walletId && transaction.SenderWalletId != walletId)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0m,
            OutgoingCount = await baseFilteredQuery.CountAsync(
                transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId != walletId,
                cancellationToken),
            OutgoingTotal = await baseFilteredQuery
                .Where(transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId != walletId)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0m,
            DepositCount = await baseFilteredQuery.CountAsync(
                transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId == walletId,
                cancellationToken),
            DepositTotal = await baseFilteredQuery
                .Where(transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId == walletId)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0m,
            Currency = currency
        };

        IQueryable<Transaction> typeFilteredQuery = ApplyTypeFilter(baseFilteredQuery, walletId, queryParameters.Type);
        int totalCount = await typeFilteredQuery.CountAsync(cancellationToken);

        List<Transaction> pageTransactions = await typeFilteredQuery
            .Include(transaction => transaction.SenderWallet)
            .ThenInclude(wallet => wallet!.User)
            .Include(transaction => transaction.ReceiverWallet)
            .ThenInclude(wallet => wallet!.User)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        return new TransactionHistoryPageDto
        {
            Items = pageTransactions
                .Select(transaction => MapTransaction(walletId, currency, transaction))
                .ToList(),
            Summary = summary,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount,
            HasMore = queryParameters.Page * queryParameters.PageSize < totalCount
        };
    }

    public async Task CreateCompletedTransferAsync(
        Transaction transaction,
        Wallet senderWallet,
        Wallet receiverWallet,
        CancellationToken cancellationToken = default)
    {
        Wallet trackedSenderWallet = await _appDbContext.Wallets
            .FirstAsync(wallet => wallet.Id == senderWallet.Id, cancellationToken);

        Wallet trackedReceiverWallet = await _appDbContext.Wallets
            .FirstAsync(wallet => wallet.Id == receiverWallet.Id, cancellationToken);

        trackedSenderWallet.Balance = senderWallet.Balance;
        trackedSenderWallet.UpdatedAtUtc = senderWallet.UpdatedAtUtc;
        trackedReceiverWallet.Balance = receiverWallet.Balance;
        trackedReceiverWallet.UpdatedAtUtc = receiverWallet.UpdatedAtUtc;

        await _appDbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateCompletedDepositAsync(
        Transaction transaction,
        Wallet wallet,
        CancellationToken cancellationToken = default)
    {
        Wallet trackedWallet = await _appDbContext.Wallets
            .FirstAsync(currentWallet => currentWallet.Id == wallet.Id, cancellationToken);

        trackedWallet.Balance = wallet.Balance;
        trackedWallet.UpdatedAtUtc = wallet.UpdatedAtUtc;

        await _appDbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
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

    private static IQueryable<Transaction> ApplySearchFilter(IQueryable<Transaction> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        string normalizedSearchTerm = searchTerm.Trim().ToLower();

        return query.Where(transaction =>
            transaction.Reference.ToLower().Contains(normalizedSearchTerm) ||
            (transaction.Description != null && transaction.Description.ToLower().Contains(normalizedSearchTerm)));
    }

    private static IQueryable<Transaction> ApplyDateRangeFilter(IQueryable<Transaction> query, string? dateRange)
    {
        if (string.Equals(dateRange, "Today", StringComparison.OrdinalIgnoreCase))
        {
            DateTime startOfToday = DateTime.UtcNow.Date;
            return query.Where(transaction => transaction.CreatedAtUtc >= startOfToday);
        }

        if (string.Equals(dateRange, "Last7Days", StringComparison.OrdinalIgnoreCase))
        {
            DateTime rangeStart = DateTime.UtcNow.AddDays(-7);
            return query.Where(transaction => transaction.CreatedAtUtc >= rangeStart);
        }

        if (string.Equals(dateRange, "Last30Days", StringComparison.OrdinalIgnoreCase))
        {
            DateTime rangeStart = DateTime.UtcNow.AddDays(-30);
            return query.Where(transaction => transaction.CreatedAtUtc >= rangeStart);
        }

        return query;
    }

    private static IQueryable<Transaction> ApplyTypeFilter(IQueryable<Transaction> query, Guid walletId, string? type)
    {
        if (string.Equals(type, "Incoming", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(transaction => transaction.ReceiverWalletId == walletId && transaction.SenderWalletId != walletId);
        }

        if (string.Equals(type, "Outgoing", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId != walletId);
        }

        if (string.Equals(type, "Deposits", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(transaction => transaction.SenderWalletId == walletId && transaction.ReceiverWalletId == walletId);
        }

        return query;
    }

    private static TransactionHistoryItemDto MapTransaction(Guid currentWalletId, string currency, Transaction transaction)
    {
        bool isSelfDeposit = transaction.SenderWalletId == currentWalletId &&
                             transaction.ReceiverWalletId == currentWalletId;
        bool isIncoming = transaction.ReceiverWalletId == currentWalletId;
        Wallet? counterpartyWallet = isIncoming ? transaction.SenderWallet : transaction.ReceiverWallet;

        return new TransactionHistoryItemDto
        {
            TransactionId = transaction.Id,
            IsIncoming = isIncoming,
            Amount = transaction.Amount,
            Currency = currency,
            CounterpartyUsername = isSelfDeposit
                ? "SecureWallet"
                : counterpartyWallet?.User?.Username ?? string.Empty,
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
