using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Interfaces.Repositories;

namespace SecureWallet.Application.Features.Admin.Queries.GetAdminUsers;

public class GetAdminUsersHandler
{
    private readonly IUserRepository _userRepository;

    public GetAdminUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<AdminUserListItemDto>> Handle(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Domain.Entities.User> users = await _userRepository.GetAllAsync(cancellationToken);

        return users
            .Select(user => new AdminUserListItemDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role?.Name ?? string.Empty,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAtUtc = user.CreatedAtUtc
            })
            .ToList();
    }
}
