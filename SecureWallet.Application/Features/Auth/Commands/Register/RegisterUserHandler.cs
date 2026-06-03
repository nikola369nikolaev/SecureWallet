using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Register;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IWalletRepository walletRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _walletRepository = walletRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResultDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        // Reject weak passwords before we create any user records.
        IReadOnlyCollection<string> validationErrors = PasswordValidator.Validate(command.Password);

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", validationErrors));
        }

        // Normalize user input so uniqueness checks and persistence stay consistent.
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();
        string normalizedUsername = command.Username.Trim();
        string? normalizedPhoneNumber = command.PhoneNumber?.Trim();

        // Block duplicate accounts by email or username before creating the user.
        User? existingUserByEmail = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUserByEmail is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        User? existingUserByUsername = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (existingUserByUsername is not null)
        {
            throw new InvalidOperationException("A user with this username already exists.");
        }

        Role? userRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
        if (userRole is null)
        {
            throw new InvalidOperationException("Default user role was not found.");
        }

        // Hash the password before storing it so we never persist the raw secret.
        User user = new()
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            Password = _passwordHasher.Hash(command.Password),
            PhoneNumber = normalizedPhoneNumber,
            FirstName = command.FirstName?.Trim(),
            LastName = command.LastName?.Trim(),
            RoleId = userRole.Id,
            Role = userRole
        };

        // First persist the user, then create the initial wallet linked to that user.
        await _userRepository.AddAsync(user, cancellationToken);

        Wallet wallet = new()
        {
            UserId = user.Id,
            User = user,
            Balance = 0m
        };

        await _walletRepository.AddAsync(wallet, cancellationToken);

        // Return only the public data the caller needs after successful registration.
        return new RegisterResultDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}
