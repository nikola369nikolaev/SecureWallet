using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Features.Wallets;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Register;

public class RegisterUserHandler
{
    private static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationSender _emailVerificationSender;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IEmailVerificationSender emailVerificationSender)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
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
        bool retryingPendingRegistration = existingUserByEmail is not null && !existingUserByEmail.IsEmailVerified;

        if (existingUserByEmail is not null && existingUserByEmail.IsEmailVerified)
        {
            throw new InvalidOperationException("Вече съществува акаунт с този имейл.");
        }

        User? existingUserByUsername = await _userRepository.GetByUsernameAsync(command.Username, cancellationToken);
        if (existingUserByUsername is not null &&
            (!retryingPendingRegistration || existingUserByUsername.Id != existingUserByEmail!.Id))
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

        if (retryingPendingRegistration)
        {
            User user = existingUserByEmail!;
            user.Username = command.Username;
            user.Email = command.Email;
            user.Password = _passwordHasher.Hash(command.Password);
            user.PhoneNumber = command.PhoneNumber;
            user.FirstName = command.FirstName;
            user.LastName = command.LastName;
            user.RoleId = userRole.Id;
            user.IsEmailVerified = false;
            user.EmailVerificationCodeHash = _passwordHasher.Hash(emailVerificationCode);
            user.EmailVerificationCodeExpiresAtUtc = expiresAtUtc;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _emailVerificationSender.SendRegistrationVerificationCodeAsync(user.Email, emailVerificationCode, cancellationToken);
            await _userRepository.UpdateAsync(user, cancellationToken);

            return new RegisterResultDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Message = "Имаш незавършена регистрация. Изпратихме нов код за потвърждение на имейла.",
                RequiresEmailVerification = true,
                SecuritySetupRequired = true
            };
        }

        User newUser = new()
        {
            Username = command.Username,
            Email = command.Email,
            Password = _passwordHasher.Hash(command.Password),
            PhoneNumber = command.PhoneNumber,
            FirstName = command.FirstName,
            LastName = command.LastName,
            RoleId = userRole.Id,
            IsEmailVerified = false,
            EmailVerificationCodeHash = _passwordHasher.Hash(emailVerificationCode),
            EmailVerificationCodeExpiresAtUtc = expiresAtUtc
        };

        Wallet wallet = new()
        {
            UserId = newUser.Id,
            Balance = 0m
        };

        WalletCardGenerator.ApplyNewCardDetails(wallet);

        await _emailVerificationSender.SendRegistrationVerificationCodeAsync(newUser.Email, emailVerificationCode, cancellationToken);
        await _userRepository.AddWithWalletAsync(newUser, wallet, cancellationToken);

        return new RegisterResultDto
        {
            UserId = newUser.Id,
            Username = newUser.Username,
            Email = newUser.Email,
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
