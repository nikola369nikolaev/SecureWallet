using FluentValidation;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;
using SecureWallet.Domain.Enums;

namespace SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;

public class CreateDepositHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITotpService _totpService;
    private readonly IValidator<CreateDepositCommand> _validator;

    public CreateDepositHandler(
        IUserRepository userRepository,
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITotpService totpService,
        IValidator<CreateDepositCommand> validator)
    {
        _userRepository = userRepository;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _totpService = totpService;
        _validator = validator;
    }

    public async Task<DepositResultDto> Handle(CreateDepositCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new InvalidOperationException("Двуфакторната защита не е включена за този акаунт.");
        }

        bool isTotpCodeValid = _totpService.VerifyCode(user.TotpSecret, command.TotpCode);
        if (!isTotpCodeValid)
        {
            throw new InvalidOperationException("Временният код е грешен.");
        }

        Wallet? wallet = await _walletRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("Портфейлът не беше намерен.");
        }

        if (!wallet.IsActive)
        {
            throw new InvalidOperationException("Портфейлът не е активен.");
        }

        DateTime now = DateTime.UtcNow;
        wallet.Balance += command.Amount;
        wallet.UpdatedAtUtc = now;

        Transaction transaction = new()
        {
            SenderWalletId = wallet.Id,
            ReceiverWalletId = wallet.Id,
            Amount = command.Amount,
            Status = TransactionStatus.Completed,
            Reference = GenerateReference(),
            Description = "Депозит в портфейла",
            CreatedAtUtc = now
        };

        await _transactionRepository.CreateCompletedDepositAsync(transaction, wallet, cancellationToken);

        return new DepositResultDto
        {
            Message = "Депозитът е добавен.",
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            Currency = wallet.Currency,
            UpdatedBalance = wallet.Balance,
            Reference = transaction.Reference,
            Status = FormatStatus(transaction.Status),
            CreatedAtUtc = transaction.CreatedAtUtc
        };
    }

    private static string GenerateReference()
    {
        string shortGuid = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"DEP-{DateTime.UtcNow:yyyyMMddHHmmss}-{shortGuid}";
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
