using System.Security.Claims;
using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Refresh;

public class RefreshSessionHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITotpService _totpService;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly IValidator<RefreshSessionCommand> _validator;

    public RefreshSessionHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITotpService totpService,
        AuthSessionIssuer authSessionIssuer,
        IValidator<RefreshSessionCommand> validator)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _totpService = totpService;
        _authSessionIssuer = authSessionIssuer;
        _validator = validator;
    }

    public async Task<RefreshSessionResultDto> Handle(RefreshSessionCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        ClaimsPrincipal? principal = _jwtTokenService.GetPrincipalFromExpiredAccessToken(command.ExpiredAccessToken);
        Guid userId = GetUserId(principal);

        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Сесията изтече. Моля влез отново.");
        }

        if (!user.IsEmailVerified || !user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new InvalidOperationException("Сесията не може да бъде подновена. Моля влез отново.");
        }

        bool isTotpCodeValid = _totpService.VerifyCode(user.TotpSecret, command.TotpCode);
        if (!isTotpCodeValid)
        {
            throw new InvalidOperationException("Временният код е грешен.");
        }

        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, false, cancellationToken);

        return new RefreshSessionResultDto
        {
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = false
        };
    }

    private static Guid GetUserId(ClaimsPrincipal? principal)
    {
        string? userIdValue = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? principal?.FindFirst("sub")?.Value
                              ?? principal?.FindFirst(ClaimTypes.Name)?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Сесията изтече. Моля влез отново.");
        }

        return userId;
    }
}

