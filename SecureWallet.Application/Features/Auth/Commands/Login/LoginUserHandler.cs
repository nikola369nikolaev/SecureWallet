using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Exceptions;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Login;

public class LoginUserHandler
{
    private const int CaptchaRequiredAttempts = 3;
    private const int FirstLockoutAttempts = 5;
    private const int FirstLockoutSeconds = 30;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly ICaptchaVerificationService _captchaVerificationService;
    private readonly ITotpService _totpService;
    private readonly IValidator<LoginUserCommand> _validator;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AuthSessionIssuer authSessionIssuer,
        ICaptchaVerificationService captchaVerificationService,
        ITotpService totpService,
        IValidator<LoginUserCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authSessionIssuer = authSessionIssuer;
        _captchaVerificationService = captchaVerificationService;
        _totpService = totpService;
        _validator = validator;
    }

    public async Task<LoginResultDto> Handle(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        DateTime now = DateTime.UtcNow;
        bool solvedCaptchaInCurrentAttempt = false;
        User? user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Грешен имейл или парола.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > now)
        {
            int remainingSeconds = (int)Math.Ceiling((user.LockoutEndUtc.Value - now).TotalSeconds);
            throw new LoginProtectionException(
                $"Твърде много неуспешни опити за вход. Опитай отново след {remainingSeconds} секунди.",
                requiresCaptcha: user.FailedLoginAttempts >= CaptchaRequiredAttempts,
                requiresTotp: user.TwoFactorEnabled,
                captchaImageBase64: CreateCaptchaImageBase64(user.CurrentCaptchaCode),
                lockoutSeconds: remainingSeconds,
                email: user.Email,
                failureStage: "LockoutActive",
                failedAttemptCount: user.FailedLoginAttempts);
        }

        if (user.FailedLoginAttempts >= CaptchaRequiredAttempts)
        {
            if (string.IsNullOrEmpty(user.CurrentCaptchaCode))
            {
                user.CurrentCaptchaCode = _captchaVerificationService.GenerateCaptchaCode();
                user.UpdatedAtUtc = now;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(command.CaptchaToken))
            {
                LoginProtectionException missingCaptchaException = await RegisterFailedAttemptAsync(
                    user,
                    now,
                    "Задължително е да въведеш кода от картинката.",
                    cancellationToken,
                    "MissingCaptcha");

                throw missingCaptchaException;
            }

            bool isCaptchaValid = _captchaVerificationService.IsValid(command.CaptchaToken, user.CurrentCaptchaCode);
            if (!isCaptchaValid)
            {
                LoginProtectionException captchaException = await RegisterFailedAttemptAsync(
                    user,
                    now,
                    "Кодът от картинката е грешен.",
                    cancellationToken,
                    "InvalidCaptcha");

                throw captchaException;
            }

            solvedCaptchaInCurrentAttempt = true;
        }

        if (!user.IsActive)
        {
            LoginProtectionException inactiveUserException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Грешен имейл или парола.",
                cancellationToken,
                "InactiveUser");

            throw inactiveUserException;
        }

        bool isPasswordValid = _passwordHasher.Verify(command.Password, user.Password);
        if (!isPasswordValid)
        {
            LoginProtectionException invalidPasswordException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Грешен имейл или парола.",
                cancellationToken,
                "InvalidPassword");

            throw invalidPasswordException;
        }

        if (solvedCaptchaInCurrentAttempt)
        {
            await ClearLoginProtectionStateAsync(user, now, cancellationToken);
        }

        if (!user.IsEmailVerified)
        {
            await ClearLoginProtectionStateAsync(user, now, cancellationToken);

            throw new LoginProtectionException(
                "Имейлът още не е потвърден. Въведи кода от имейла, за да продължиш.",
                email: user.Email,
                requiresEmailVerification: true,
                failureStage: "EmailVerificationRequired",
                failedAttemptCount: user.FailedLoginAttempts);
        }

        if (!user.TwoFactorEnabled)
        {
            await ClearLoginProtectionStateAsync(user, now, cancellationToken);
            return await CreateSessionResultAsync(user, true, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(command.TotpCode))
        {
            throw new LoginProtectionException(
                "Нужен е код от authenticator приложението.",
                requiresCaptcha: false,
                requiresTotp: true,
                email: user.Email,
                failureStage: "MissingTotp",
                failedAttemptCount: user.FailedLoginAttempts);
        }

        bool isTotpCodeValid = !string.IsNullOrWhiteSpace(user.TotpSecret) &&
                               _totpService.VerifyCode(user.TotpSecret, command.TotpCode);

        if (!isTotpCodeValid)
        {
            LoginProtectionException invalidTotpException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Кодът от authenticator приложението е грешен.",
                cancellationToken,
                "InvalidTotp",
                true);

            throw invalidTotpException;
        }

        await ClearLoginProtectionStateAsync(user, now, cancellationToken);
        return await CreateSessionResultAsync(user, false, cancellationToken);
    }

    private async Task<LoginResultDto> CreateSessionResultAsync(
        User user,
        bool securitySetupRequired,
        CancellationToken cancellationToken)
    {
        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, securitySetupRequired, cancellationToken);

        return new LoginResultDto
        {
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = securitySetupRequired
        };
    }

    private async Task ClearLoginProtectionStateAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        if (user.FailedLoginAttempts == 0 &&
            !user.LockoutEndUtc.HasValue &&
            string.IsNullOrEmpty(user.CurrentCaptchaCode))
        {
            return;
        }

        user.FailedLoginAttempts = 0;
        user.CurrentCaptchaCode = null;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = now;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    private async Task<LoginProtectionException> RegisterFailedAttemptAsync(
        User user,
        DateTime now,
        string defaultMessage,
        CancellationToken cancellationToken,
        string failureStage,
        bool requiresTotp = false)
    {
        user.FailedLoginAttempts += 1;
        user.UpdatedAtUtc = now;

        bool requiresCaptcha = user.FailedLoginAttempts >= CaptchaRequiredAttempts;
        if (requiresCaptcha)
        {
            user.CurrentCaptchaCode = _captchaVerificationService.GenerateCaptchaCode();
        }
        else
        {
            user.CurrentCaptchaCode = null;
        }

        int? lockoutSeconds = null;
        if (user.FailedLoginAttempts >= FirstLockoutAttempts)
        {
            lockoutSeconds = CalculateLockoutSeconds(user.FailedLoginAttempts);
            user.LockoutEndUtc = now.AddSeconds(lockoutSeconds.Value);
        }
        else
        {
            user.LockoutEndUtc = null;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        if (lockoutSeconds.HasValue)
        {
            return new LoginProtectionException(
                $"Твърде много неуспешни опити за вход. Входът е заключен за {lockoutSeconds.Value} секунди.",
                requiresCaptcha: requiresCaptcha,
                requiresTotp: requiresTotp,
                captchaImageBase64: CreateCaptchaImageBase64(user.CurrentCaptchaCode),
                lockoutSeconds: lockoutSeconds,
                email: user.Email,
                failureStage: failureStage,
                failedAttemptCount: user.FailedLoginAttempts);
        }

        if (requiresCaptcha)
        {
            string message = defaultMessage.Contains("картинката", StringComparison.OrdinalIgnoreCase)
                ? defaultMessage
                : $"{defaultMessage} Задължително е да въведеш и кода от картинката.";

            return new LoginProtectionException(
                message,
                requiresCaptcha: true,
                requiresTotp: requiresTotp,
                captchaImageBase64: CreateCaptchaImageBase64(user.CurrentCaptchaCode),
                email: user.Email,
                failureStage: failureStage,
                failedAttemptCount: user.FailedLoginAttempts);
        }

        return new LoginProtectionException(
            defaultMessage,
            requiresCaptcha: false,
            requiresTotp: requiresTotp,
            email: user.Email,
            failureStage: failureStage,
            failedAttemptCount: user.FailedLoginAttempts);
    }

    private string? CreateCaptchaImageBase64(string? captchaCode)
    {
        if (string.IsNullOrEmpty(captchaCode))
        {
            return null;
        }

        return _captchaVerificationService.GenerateCaptchaImageBase64(captchaCode);
    }

    private static int CalculateLockoutSeconds(int failedLoginAttempts)
    {
        if (failedLoginAttempts < FirstLockoutAttempts)
        {
            return 0;
        }

        int exponent = failedLoginAttempts - FirstLockoutAttempts;
        return FirstLockoutSeconds * (int)Math.Pow(2, exponent);
    }
}