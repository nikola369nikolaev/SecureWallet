using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class RequestPasswordResetCodeHandler
{
    private static readonly TimeSpan PasswordResetCodeLifetime = TimeSpan.FromMinutes(5);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISmsVerificationService _smsVerificationService;
    private readonly IValidator<RequestPasswordResetCodeCommand> _validator;

    public RequestPasswordResetCodeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISmsVerificationService smsVerificationService,
        IValidator<RequestPasswordResetCodeCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _smsVerificationService = smsVerificationService;
        _validator = validator;
    }

    public async Task<PasswordResetCodeDispatchResultDto> Handle(RequestPasswordResetCodeCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByEmailAndPhoneNumberAsync(command.Email, command.PhoneNumber, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Не беше намерен акаунт с този имейл и телефонен номер.");
        }

        if (user.PasswordResetCodeLockoutEndUtc is not null && user.PasswordResetCodeLockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Смяната на паролата е временно заключена. Опитай отново след 15 минути.");
        }

        string resetCode = GenerateResetCode();
        SmsVerificationDispatchResult dispatchResult = await _smsVerificationService
            .SendPasswordResetCodeAsync(user.PhoneNumber!, resetCode, cancellationToken);

        user.PasswordResetCodeHash = _passwordHasher.Hash(resetCode);
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.Add(PasswordResetCodeLifetime);
        user.FailedPasswordResetCodeAttempts = 0;
        user.PasswordResetCodeLockoutEndUtc = null;
        user.PasswordResetSessionToken = null;
        user.PasswordResetSessionExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new PasswordResetCodeDispatchResultDto
        {
            Message = dispatchResult.Message,
            CanEnterCode = true
        };
    }

    private static string GenerateResetCode()
    {
        int code = Random.Shared.Next(100000, 999999);
        return code.ToString();
    }
}