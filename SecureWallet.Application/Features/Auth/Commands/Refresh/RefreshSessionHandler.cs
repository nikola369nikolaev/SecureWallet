using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Refresh;

public class RefreshSessionHandler
{
    private readonly IUserRepository _userRepository;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly IValidator<RefreshSessionCommand> _validator;

    public RefreshSessionHandler(
        IUserRepository userRepository,
        AuthSessionIssuer authSessionIssuer,
        IValidator<RefreshSessionCommand> validator)
    {
        _userRepository = userRepository;
        _authSessionIssuer = authSessionIssuer;
        _validator = validator;
    }

    public async Task<RefreshSessionResultDto> Handle(RefreshSessionCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Сесията изтече. Моля влез отново.");
        }

        if (!_authSessionIssuer.IsRefreshTokenValid(user, command.RefreshToken))
        {
            throw new InvalidOperationException("Сесията изтече. Моля влез отново.");
        }

        bool securitySetupRequired = user.IsEmailVerified && !user.TwoFactorEnabled;
        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, securitySetupRequired, cancellationToken);

        return new RefreshSessionResultDto
        {
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = securitySetupRequired
        };
    }
}