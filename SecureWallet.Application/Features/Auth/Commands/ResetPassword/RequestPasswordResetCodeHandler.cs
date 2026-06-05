using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class RequestPasswordResetCodeHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISmsVerificationService _smsVerificationService;

    public RequestPasswordResetCodeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISmsVerificationService smsVerificationService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _smsVerificationService = smsVerificationService;
    }

    public async Task<PasswordResetCodeDispatchResultDto> Handle(RequestPasswordResetCodeCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidatePhoneNumber(command.PhoneNumber);

        User? user = await _userRepository.GetByEmailAndPhoneNumberAsync(command.Email, command.PhoneNumber, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("No account was found for this email and phone number.");
        }

        string resetCode = GenerateResetCode();
        SmsVerificationDispatchResult dispatchResult = await _smsVerificationService
                .SendPasswordResetCodeAsync(user.PhoneNumber!, resetCode, cancellationToken);

        user.PasswordResetCodeHash = _passwordHasher.Hash(resetCode);
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        user.PasswordResetSessionToken = null;
        user.PasswordResetSessionExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new PasswordResetCodeDispatchResultDto
        {
            Message = dispatchResult.Message,
            CanEnterCode = true,
            DevelopmentCodePreview = dispatchResult.DevelopmentCodePreview
        };
    }

    private static string GenerateResetCode()
    {
        int code = Random.Shared.Next(100000, 999999);
        return code.ToString();
    }
}
