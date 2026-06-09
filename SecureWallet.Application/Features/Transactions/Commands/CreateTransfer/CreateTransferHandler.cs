using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;
using SecureWallet.Domain.Enums;

namespace SecureWallet.Application.Features.Transactions.Commands.CreateTransfer;

public class CreateTransferHandler
{
    private const string UsernameRecipientType = "Username";
    private const string PhoneNumberRecipientType = "PhoneNumber";
    private const string IbanRecipientType = "Iban";

    private readonly IWalletRepository _walletRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITotpService _totpService;

    public CreateTransferHandler(
        IWalletRepository walletRepository,
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        ITotpService totpService)
    {
        _walletRepository = walletRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _totpService = totpService;
    }

    public async Task<TransferResultDto> Handle(CreateTransferCommand command, CancellationToken cancellationToken = default)
    {
        ValidateRecipient(command.RecipientType, command.RecipientValue);
        ValidateAmount(command.Amount);
        ValidateDescription(command.Description);
        ValidateTotpCode(command.TotpCode);

        User? senderUser = await _userRepository.GetByIdAsync(command.SenderUserId, cancellationToken);
        if (senderUser is null || !senderUser.IsActive)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (!senderUser.TwoFactorEnabled || string.IsNullOrWhiteSpace(senderUser.TotpSecret))
        {
            throw new InvalidOperationException("Първо трябва да включиш двуфакторната защита.");
        }

        bool isTotpCodeValid = _totpService.VerifyCode(senderUser.TotpSecret, command.TotpCode);
        if (!isTotpCodeValid)
        {
            throw new InvalidOperationException("Кодът от authenticator приложението е грешен.");
        }

        Wallet? senderWallet = await _walletRepository.GetByUserIdAsync(command.SenderUserId, cancellationToken);
        if (senderWallet is null)
        {
            throw new InvalidOperationException("Портфейлът на подателя не беше намерен.");
        }

        if (!senderWallet.IsActive)
        {
            throw new InvalidOperationException("Твоят портфейл не е активен.");
        }

        User receiverUser = await ResolveReceiverUserAsync(command, cancellationToken);

        if (!receiverUser.IsActive)
        {
            throw new InvalidOperationException("Получателят не е активен.");
        }

        if (receiverUser.Id == command.SenderUserId)
        {
            throw new InvalidOperationException("Не можеш да изпращаш пари към собствения си акаунт.");
        }

        Wallet? receiverWallet = await _walletRepository.GetByUserIdAsync(receiverUser.Id, cancellationToken);
        if (receiverWallet is null)
        {
            throw new InvalidOperationException("Портфейлът на получателя не беше намерен.");
        }

        if (!receiverWallet.IsActive)
        {
            throw new InvalidOperationException("Портфейлът на получателя не е активен.");
        }

        if (!string.Equals(senderWallet.Currency, receiverWallet.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Превод между портфейли с различна валута още не се поддържа.");
        }

        if (senderWallet.Balance < command.Amount)
        {
            throw new InvalidOperationException("Нямаш достатъчна наличност за този превод.");
        }

        DateTime now = DateTime.UtcNow;
        senderWallet.Balance -= command.Amount;
        senderWallet.UpdatedAtUtc = now;
        receiverWallet.Balance += command.Amount;
        receiverWallet.UpdatedAtUtc = now;

        Transaction transaction = new()
        {
            SenderWalletId = senderWallet.Id,
            ReceiverWalletId = receiverWallet.Id,
            Amount = command.Amount,
            Status = TransactionStatus.Completed,
            Reference = GenerateReference(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description,
            CreatedAtUtc = now
        };

        await _transactionRepository.CreateCompletedTransferAsync(
            transaction,
            senderWallet,
            receiverWallet,
            cancellationToken);

        return new TransferResultDto
        {
            Message = "Преводът беше изпратен успешно.",
            TransactionId = transaction.Id,
            ReceiverUsername = receiverUser.Username,
            Amount = transaction.Amount,
            Currency = senderWallet.Currency,
            UpdatedBalance = senderWallet.Balance,
            Reference = transaction.Reference,
            Status = FormatStatus(transaction.Status),
            Description = transaction.Description ?? string.Empty,
            CreatedAtUtc = transaction.CreatedAtUtc
        };
    }

    private async Task<User> ResolveReceiverUserAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        if (string.Equals(command.RecipientType, UsernameRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            User? userByUsername = await _userRepository.GetByUsernameAsync(command.RecipientValue, cancellationToken);
            return userByUsername ?? throw new InvalidOperationException("Не беше намерен потребител с това потребителско име.");
        }

        if (string.Equals(command.RecipientType, PhoneNumberRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            int usersWithThisPhone = await _userRepository.CountByPhoneNumberAsync(command.RecipientValue, cancellationToken);
            if (usersWithThisPhone == 0)
            {
                throw new InvalidOperationException("Не беше намерен потребител с този телефонен номер.");
            }

            if (usersWithThisPhone > 1)
            {
                throw new InvalidOperationException("Има повече от един акаунт с този телефонен номер. Използвай потребителско име или IBAN.");
            }

            User? userByPhone = await _userRepository.GetByPhoneNumberAsync(command.RecipientValue, cancellationToken);
            return userByPhone ?? throw new InvalidOperationException("Не беше намерен потребител с този телефонен номер.");
        }

        if (string.Equals(command.RecipientType, IbanRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            Wallet? walletByIban = await _walletRepository.GetByIbanAsync(command.RecipientValue, cancellationToken);
            if (walletByIban?.User is null)
            {
                throw new InvalidOperationException("Не беше намерен портфейл с този IBAN.");
            }

            return walletByIban.User;
        }

        throw new InvalidOperationException("Неподдържан тип получател.");
    }

    private static void ValidateRecipient(string recipientType, string recipientValue)
    {
        AuthInputValidator.ValidateRequiredField(recipientType, "Типът получател");
        AuthInputValidator.ValidateRequiredField(recipientValue, "Получателят");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(recipientType, "Типът получател");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(recipientValue, "Получателят");

        if (string.Equals(recipientType, UsernameRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            AuthInputValidator.ValidateUsername(recipientValue);
            return;
        }

        if (string.Equals(recipientType, PhoneNumberRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            AuthInputValidator.ValidatePhoneNumber(recipientValue);
            return;
        }

        if (string.Equals(recipientType, IbanRecipientType, StringComparison.OrdinalIgnoreCase))
        {
            string normalizedIban = recipientValue.Replace(" ", string.Empty);
            if (normalizedIban.Length != 22 || !normalizedIban.All(char.IsLetterOrDigit))
            {
                throw new InvalidOperationException("IBAN-ът трябва да е във валиден формат.");
            }

            return;
        }

        throw new InvalidOperationException("Неподдържан тип получател.");
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

    private static void ValidateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        if (description != description.Trim())
        {
            throw new InvalidOperationException("Коментарът не трябва да започва или завършва с интервали.");
        }

        if (description.Length > 500)
        {
            throw new InvalidOperationException("Коментарът може да е най-много 500 символа.");
        }
    }

    private static void ValidateTotpCode(string totpCode)
    {
        AuthInputValidator.ValidateRequiredField(totpCode, "Кодът от authenticator приложението");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(totpCode, "Кодът от authenticator приложението");
    }

    private static string GenerateReference()
    {
        string shortGuid = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{shortGuid}";
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
