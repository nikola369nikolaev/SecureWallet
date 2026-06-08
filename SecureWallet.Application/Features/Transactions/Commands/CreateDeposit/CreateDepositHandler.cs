using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;
using SecureWallet.Domain.Enums;

namespace SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;

public class CreateDepositHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITotpService _totpService;

    public CreateDepositHandler(
        IUserRepository userRepository,
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITotpService totpService)
    {
        _userRepository = userRepository;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _totpService = totpService;
    }

    public async Task<DepositResultDto> Handle(CreateDepositCommand command, CancellationToken cancellationToken = default)
    {
        ValidateAmount(command.Amount);
        AuthInputValidator.ValidateRequiredField(command.TotpCode, "Кодът от authenticator приложението");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.TotpCode, "Кодът от authenticator приложението");

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new InvalidOperationException("Първо трябва да включиш двуфакторната защита.");
        }

        bool isTotpCodeValid = _totpService.VerifyCode(user.TotpSecret, command.TotpCode);
        if (!isTotpCodeValid)
        {
            throw new InvalidOperationException("Кодът от authenticator приложението е грешен.");
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
            Message = "Депозитът беше добавен успешно.",
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            Currency = wallet.Currency,
            UpdatedBalance = wallet.Balance,
            Reference = transaction.Reference,
            Status = FormatStatus(transaction.Status),
            CreatedAtUtc = transaction.CreatedAtUtc
        };
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Сумата трябва да е по-голяма от 0.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new InvalidOperationException("Сумата може да има най-много 2 знака след десетичната запетая.");
        }
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
