using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Register;

public class RegisterUserHandler
{
    private static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationSender _emailVerificationSender;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IWalletRepository walletRepository,
        IPasswordHasher passwordHasher,
        IEmailVerificationSender emailVerificationSender)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _walletRepository = walletRepository;
        _passwordHasher = passwordHasher;
        _emailVerificationSender = emailVerificationSender;
    }

    public async Task<RegisterResultDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> validationErrors = PasswordValidator.Validate(command.Password);

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", validationErrors));
        }

        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidateUsername(command.Username);
        AuthInputValidator.ValidatePhoneNumber(command.PhoneNumber);
        AuthInputValidator.ValidateRequiredField(command.Password, "Парола");
        AuthInputValidator.ValidatePersonName(command.FirstName ?? string.Empty, "Собственото име");
        AuthInputValidator.ValidatePersonName(command.LastName ?? string.Empty, "Фамилията");

        User? existingUserByEmail = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUserByEmail is not null)
        {
            throw new InvalidOperationException("Вече съществува акаунт с този имейл.");
        }

        User? existingUserByUsername = await _userRepository.GetByUsernameAsync(command.Username, cancellationToken);
        if (existingUserByUsername is not null)
        {
            throw new InvalidOperationException("Вече съществува акаунт с това потребителско име.");
        }

        Role? userRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
        if (userRole is null)
        {
            throw new InvalidOperationException("Ролята User не беше намерена.");
        }

        string emailVerificationCode = GenerateVerificationCode();
        DateTime expiresAtUtc = DateTime.UtcNow.Add(EmailVerificationCodeLifetime);

        User user = new()
        {
            Username = command.Username,
            Email = command.Email,
            Password = _passwordHasher.Hash(command.Password),
            PhoneNumber = command.PhoneNumber,
            FirstName = command.FirstName,
            LastName = command.LastName,
            RoleId = userRole.Id,
            Role = userRole,
            IsEmailVerified = false,
            EmailVerificationCodeHash = _passwordHasher.Hash(emailVerificationCode),
            EmailVerificationCodeExpiresAtUtc = expiresAtUtc
        };

        await _userRepository.AddAsync(user, cancellationToken);

        Wallet wallet = new()
        {
            UserId = user.Id,
            Balance = 0m
        };

        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _emailVerificationSender.SendRegistrationVerificationCodeAsync(user.Email, emailVerificationCode, cancellationToken);

        return new RegisterResultDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Message = "Изпратихме код за потвърждение на имейла. Въведи го, за да продължиш към двуфакторната защита.",
            RequiresEmailVerification = true,
            SecuritySetupRequired = true
        };
    }

    private static string GenerateVerificationCode()
    {
        int code = Random.Shared.Next(100000, 999999);
        return code.ToString();
    }
}
