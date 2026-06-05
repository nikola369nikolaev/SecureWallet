using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class VerifyPasswordResetCodeHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public VerifyPasswordResetCodeHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<PasswordResetCodeVerificationResultDto> Handle(VerifyPasswordResetCodeCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidatePhoneNumber(command.PhoneNumber);
        AuthInputValidator.ValidateRequiredField(command.Code, "Verification code");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.Code, "Verification code");

        User? user = await _userRepository.GetByEmailAndPhoneNumberAsync(command.Email, command.PhoneNumber, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("No account was found for this email and phone number.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) || user.PasswordResetCodeExpiresAtUtc is null)
        {
            throw new InvalidOperationException("There is no active password reset code for this account.");
        }

        if (user.PasswordResetCodeExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("The SMS code has expired. Request a new code.");
        }

        if (!_passwordHasher.Verify(command.Code, user.PasswordResetCodeHash))
        {
            throw new InvalidOperationException("The SMS code is invalid.");
        }

        string resetSessionToken = Guid.NewGuid().ToString("N");

        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.PasswordResetSessionToken = resetSessionToken;
        user.PasswordResetSessionExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new PasswordResetCodeVerificationResultDto
        {
            Message = "SMS verification was successful.",
            ResetSessionToken = resetSessionToken,
            Email = user.Email
        };
    }
}
