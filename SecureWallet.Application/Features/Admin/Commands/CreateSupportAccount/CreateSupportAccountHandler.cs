using FluentValidation;
using Microsoft.Extensions.Logging;
using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Features.Wallets;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Admin.Commands.CreateSupportAccount;

public class CreateSupportAccountHandler
{
    private const decimal InitialSupportOperationalBalance = 1000m;

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateSupportAccountCommand> _validator;
    private readonly ILogger<CreateSupportAccountHandler> _logger;

    public CreateSupportAccountHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IValidator<CreateSupportAccountCommand> validator,
        ILogger<CreateSupportAccountHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<SupportAccountResultDto> Handle(CreateSupportAccountCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken, combineAllMessages: true);

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
            _logger.LogError("Създаването на support акаунт се провали, защото не беше намерена ролята 'Support'.");
            throw new InvalidOperationException("Възникна проблем. Опитай по-късно.");
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
            Message = "Support акаунтът е създаден. При първи вход ще се изиска настройка на временния код."
        };
    }
}
