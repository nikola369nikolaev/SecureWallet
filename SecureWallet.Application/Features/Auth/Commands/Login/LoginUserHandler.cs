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
    private static readonly TimeSpan FirstLockoutDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RepeatedLockoutDuration = TimeSpan.FromSeconds(35);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICaptchaVerificationService _captchaVerificationService;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICaptchaVerificationService captchaVerificationService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _captchaVerificationService = captchaVerificationService;
    }

    public async Task<LoginResultDto> Handle(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidateRequiredField(command.Password, "Password");

        DateTime now = DateTime.UtcNow;
        User? user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > now)
        {
            int remainingSeconds = (int)Math.Ceiling((user.LockoutEndUtc.Value - now).TotalSeconds);
            throw new LoginProtectionException(
                $"Too many failed login attempts. Try again in {remainingSeconds} seconds.",
                user.FailedLoginAttempts >= CaptchaRequiredAttempts,
                user.CurrentCaptchaCode,
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
                    "Captcha is required.",
                    cancellationToken);

                throw missingCaptchaException;
            }

            bool isCaptchaValid = _captchaVerificationService.IsValid(command.CaptchaToken, user.CurrentCaptchaCode);
            if (!isCaptchaValid)
            {
                LoginProtectionException captchaException = await RegisterFailedAttemptAsync(
                    user,
                    now,
                    "Captcha is invalid.",
                    cancellationToken);

                throw captchaException;
            }
        }

        if (!user.IsActive)
        {
            LoginProtectionException inactiveUserException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Invalid email or password.",
                cancellationToken);

            throw inactiveUserException;
        }

        bool isPasswordValid = _passwordHasher.Verify(command.Password, user.Password);
        if (!isPasswordValid)
        {
            LoginProtectionException invalidPasswordException = await RegisterFailedAttemptAsync(
                user,
                now,
                "Invalid email or password.",
                cancellationToken);

            throw invalidPasswordException;
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
            Role = user.Role?.Name ?? string.Empty
        };
    }

    private async Task<LoginProtectionException> RegisterFailedAttemptAsync(
        User user,
        DateTime now,
        string defaultMessage,
        CancellationToken cancellationToken)
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
        if (user.FailedLoginAttempts == FirstLockoutAttempts)
        {
            user.LockoutEndUtc = now.Add(FirstLockoutDuration);
            lockoutSeconds = (int)FirstLockoutDuration.TotalSeconds;
        }
        else if (user.FailedLoginAttempts >= FirstLockoutAttempts + 1)
        {
            user.LockoutEndUtc = now.Add(RepeatedLockoutDuration);
            lockoutSeconds = (int)RepeatedLockoutDuration.TotalSeconds;
        }
        else
        {
            user.LockoutEndUtc = null;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        if (lockoutSeconds.HasValue)
        {
            return new LoginProtectionException(
                $"Too many failed login attempts. The login is locked for {lockoutSeconds.Value} seconds.",
                requiresCaptcha,
                user.CurrentCaptchaCode,
                lockoutSeconds);
        }

        if (requiresCaptcha)
        {
            string message = defaultMessage.Contains("Captcha", StringComparison.OrdinalIgnoreCase)
                ? defaultMessage
                : $"{defaultMessage} Captcha is required.";

            return new LoginProtectionException(
                message,
                true,
                user.CurrentCaptchaCode);
        }

        return new LoginProtectionException(defaultMessage);
    }
}
