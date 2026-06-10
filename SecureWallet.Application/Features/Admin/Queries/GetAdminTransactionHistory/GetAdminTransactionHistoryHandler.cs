using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;

namespace SecureWallet.Application.Features.Admin.Queries.GetAdminTransactionHistory;

public class GetAdminTransactionHistoryHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public GetAdminTransactionHistoryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<AdminTransactionHistoryPageDto> Handle(TransactionHistoryQueryParametersDto queryParameters, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetAdminHistoryPageAsync(queryParameters, cancellationToken);
    }
}
