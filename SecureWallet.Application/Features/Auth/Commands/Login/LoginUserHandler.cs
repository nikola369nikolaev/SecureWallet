using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Application.Features.Auth.Commands.Login;

public class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDto> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Normalize the email so login checks use the same format as registration.
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        // Login starts by finding the account that matches the supplied email.
        Domain.Entities.User? user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        // Inactive accounts are blocked before password or token processing continues.
        if (!user.IsActive)
        {
            throw new InvalidOperationException("The user account is inactive.");
        }

        // The raw password is checked against the stored hash, never against plain text.
        bool isPasswordValid = _passwordHasher.Verify(command.Password, user.Password);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        // Once identity is confirmed, create the signed JWT access token for the client.
        string accessToken = _jwtTokenService.GenerateAccessToken(user);

        // For now the response expiry mirrors the current JWT configuration of 60 minutes.
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
}
