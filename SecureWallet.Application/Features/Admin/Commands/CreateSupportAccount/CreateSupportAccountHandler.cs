using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Features.Wallets;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Admin.Commands.CreateSupportAccount;

public class CreateSupportAccountHandler
{
    private const decimal InitialSupportOperationalBalance = 1000m;

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateSupportAccountHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<SupportAccountResultDto> Handle(CreateSupportAccountCommand command, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> passwordErrors = PasswordValidator.Validate(command.Password);
        if (passwordErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", passwordErrors));
        }

        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidateUsername(command.Username);
        AuthInputValidator.ValidatePersonName(command.FirstName, "Собственото име");
        AuthInputValidator.ValidatePersonName(command.LastName, "Фамилията");
        AuthInputValidator.ValidatePhoneNumber(command.PhoneNumber);

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

        Role? supportRole = await _roleRepository.GetByNameAsync("Support", cancellationToken);
        if (supportRole is null)
        {
            throw new InvalidOperationException("Ролята Support не беше намерена.");
        }

        User supportUser = new()
        {
            Username = command.Username,
            Email = command.Email,
            Password = _passwordHasher.Hash(command.Password),
            FirstName = command.FirstName,
            LastName = command.LastName,
            PhoneNumber = command.PhoneNumber,
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            RoleId = supportRole.Id
        };

        Wallet wallet = new()
        {
            UserId = supportUser.Id,
            Balance = InitialSupportOperationalBalance,
            Currency = "EUR",
            IsActive = true
        };

        WalletCardGenerator.ApplyNewCardDetails(wallet);

        await _userRepository.AddWithWalletAsync(supportUser, wallet, cancellationToken);

        return new SupportAccountResultDto
        {
            UserId = supportUser.Id,
            Username = supportUser.Username,
            Email = supportUser.Email,
            Role = supportRole.Name,
            Message = "Support акаунтът беше създаден успешно. При първи login ще се изисква TOTP настройка."
        };
    }
}
