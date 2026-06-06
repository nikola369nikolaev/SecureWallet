using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCodeHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public VerifyEmailCodeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<EmailVerificationResultDto> Handle(VerifyEmailCodeCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidateRequiredField(command.Code, "Кодът за потвърждение");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.Code, "Кодът за потвърждение");

        User? user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Не беше намерен акаунт с този имейл.");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Имейлът вече е потвърден.");
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) || user.EmailVerificationCodeExpiresAtUtc is null)
        {
            throw new InvalidOperationException("Няма активен код за потвърждение на имейла.");
        }

        if (user.EmailVerificationCodeExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Кодът за потвърждение е изтекъл. Поискай нов код и опитай отново.");
        }

        if (!_passwordHasher.Verify(command.Code, user.EmailVerificationCodeHash))
        {
            throw new InvalidOperationException("Кодът за потвърждение е грешен.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        string accessToken = _jwtTokenService.GenerateAccessToken(user, true);

        return new EmailVerificationResultDto
        {
            Message = "Имейлът беше потвърден успешно. Продължи с настройката на двуфакторната защита.",
            AccessToken = accessToken,
            ExpiresAtUtc = _jwtTokenService.GetAccessTokenExpiresAtUtc(),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsEmailVerified = true,
            SecuritySetupRequired = true
        };
    }
}
