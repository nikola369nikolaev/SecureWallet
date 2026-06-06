using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Exceptions;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Login;

public class LoginUserHandler
{
    private const int CaptchaRequiredAttempts = 3;
    private const int FirstLockoutAttempts = 5;
    private const int FirstLockoutSeconds = 30;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICaptchaVerificationService _captchaVerificationService;
    private readonly ITotpService _totpService;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICaptchaVerificationService captchaVerificationService,
        ITotpService totpService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _captchaVerificationService = captchaVerificationService;
        _totpService = totpService;
    }

    public async Task<LoginResultDto> Handle(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidateRequiredField(command.Password, "Парола");

        DateTime now = DateTime.UtcNow;
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
                user.FailedLoginAttempts >= CaptchaRequiredAttempts,
                user.TwoFactorEnabled,
                CreateCaptchaImageBase64(user.CurrentCaptchaCode),
                remainingSeconds);
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
                    cancellationToken);

                throw missingCaptchaException;
            }

            bool isCaptchaValid = _captchaVerificationService.IsValid(command.CaptchaToken, user.CurrentCaptchaCode);
            if (!isCaptchaValid)
            {
                LoginProtectionException captchaException = await RegisterFailedAttemptAsync(
                    user,
                    now,
                    "Кодът от картинката е грешен.",
                    cancellationToken);

                throw captchaException;
            }
        }

        if (!user.IsActive)
        {
            LoginProtectionException inactiveUserException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Грешен имейл или парола.",
                cancellationToken);

            throw inactiveUserException;
        }

        bool isPasswordValid = _passwordHasher.Verify(command.Password, user.Password);
        if (!isPasswordValid)
        {
            LoginProtectionException invalidPasswordException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Грешен имейл или парола.",
                cancellationToken);

            throw invalidPasswordException;
        }

        if (user.TwoFactorEnabled)
        {
            bool requiresCaptcha = user.FailedLoginAttempts >= CaptchaRequiredAttempts;
            string? captchaImageBase64 = CreateCaptchaImageBase64(user.CurrentCaptchaCode);

            if (string.IsNullOrWhiteSpace(command.TotpCode))
            {
                throw new LoginProtectionException(
                    "Нужен е код от authenticator приложението.",
                    requiresCaptcha,
                    true,
                    captchaImageBase64,
                    null);
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
                    true);

                throw invalidTotpException;
            }
        }

        if (user.FailedLoginAttempts > 0 ||
            user.LockoutEndUtc.HasValue ||
            !string.IsNullOrEmpty(user.CurrentCaptchaCode))
        {
            user.FailedLoginAttempts = 0;
            user.CurrentCaptchaCode = null;
            user.LockoutEndUtc = null;
            user.UpdatedAtUtc = now;

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        string accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new LoginResultDto
        {
            AccessToken = accessToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    private async Task<LoginProtectionException> RegisterFailedAttemptAsync(
        User user,
        DateTime now,
        string defaultMessage,
        CancellationToken cancellationToken,
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
                requiresCaptcha,
                requiresTotp,
                CreateCaptchaImageBase64(user.CurrentCaptchaCode),
                lockoutSeconds);
        }

        if (requiresCaptcha)
        {
            string message = defaultMessage.Contains("картинката", StringComparison.OrdinalIgnoreCase)
                ? defaultMessage
                : $"{defaultMessage} Задължително е да въведеш и кода от картинката.";

            return new LoginProtectionException(
                message,
                true,
                requiresTotp,
                CreateCaptchaImageBase64(user.CurrentCaptchaCode));
        }

        return new LoginProtectionException(defaultMessage, false, requiresTotp);
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
