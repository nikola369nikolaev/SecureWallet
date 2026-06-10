using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.VerifyEmail;

public class ResendEmailVerificationCodeHandler
{
    private static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(15);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationSender _emailVerificationSender;
    private readonly IValidator<ResendEmailVerificationCodeCommand> _validator;

    public ResendEmailVerificationCodeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailVerificationSender emailVerificationSender,
        IValidator<ResendEmailVerificationCodeCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailVerificationSender = emailVerificationSender;
        _validator = validator;
    }

    public async Task<EmailVerificationCodeDispatchResultDto> Handle(
        ResendEmailVerificationCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Не беше намерен акаунт с този имейл.");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Имейлът вече е потвърден.");
        }

        if (user.EmailVerificationCodeExpiresAtUtc.HasValue)
        {
            DateTime lastCodeSentAtUtc = user.EmailVerificationCodeExpiresAtUtc.Value - EmailVerificationCodeLifetime;
            DateTime nextAllowedResendAtUtc = lastCodeSentAtUtc.Add(ResendCooldown);

            if (nextAllowedResendAtUtc > DateTime.UtcNow)
            {
                int remainingSeconds = (int)Math.Ceiling((nextAllowedResendAtUtc - DateTime.UtcNow).TotalSeconds);
                throw new InvalidOperationException($"Можеш да поискаш нов код след {remainingSeconds} секунди.");
            }
        }

        string verificationCode = GenerateVerificationCode();
        DateTime expiresAtUtc = DateTime.UtcNow.Add(EmailVerificationCodeLifetime);

        user.EmailVerificationCodeHash = _passwordHasher.Hash(verificationCode);
        user.EmailVerificationCodeExpiresAtUtc = expiresAtUtc;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _emailVerificationSender.SendRegistrationVerificationCodeAsync(user.Email, verificationCode, cancellationToken);

        return new EmailVerificationCodeDispatchResultDto
        {
            Message = "Изпратихме нов код за потвърждение на имейла.",
            Email = user.Email,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private static string GenerateVerificationCode()
    {
        int code = Random.Shared.Next(100000, 999999);
        return code.ToString();
    }
}