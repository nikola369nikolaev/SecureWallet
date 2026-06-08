using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.API.Requests.Admin;
using SecureWallet.Application.Features.Admin.Commands.CreateSupportAccount;
using SecureWallet.Application.Features.Admin.DTOs;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUserDetails;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUsers;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUserTransactions;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Features.Transactions.DTOs;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Support")]
public class AdminController : ControllerBase
{
    private const string AdminRoleName = "Admin";
    private const string SupportRoleName = "Support";

    private readonly GetAdminUsersHandler _getAdminUsersHandler;
    private readonly GetAdminUserDetailsHandler _getAdminUserDetailsHandler;
    private readonly GetAdminUserTransactionsHandler _getAdminUserTransactionsHandler;
    private readonly CreateSupportAccountHandler _createSupportAccountHandler;

    public AdminController(
        GetAdminUsersHandler getAdminUsersHandler,
        GetAdminUserDetailsHandler getAdminUserDetailsHandler,
        GetAdminUserTransactionsHandler getAdminUserTransactionsHandler,
        CreateSupportAccountHandler createSupportAccountHandler)
    {
        _getAdminUsersHandler = getAdminUsersHandler;
        _getAdminUserDetailsHandler = getAdminUserDetailsHandler;
        _getAdminUserTransactionsHandler = getAdminUserTransactionsHandler;
        _createSupportAccountHandler = createSupportAccountHandler;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AdminUserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<AdminUserListItemDto>>> GetUsers(CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        IReadOnlyCollection<AdminUserListItemDto> result = await _getAdminUsersHandler.Handle(cancellationToken);

        if (IsCurrentUserSupport())
        {
            result = result
                .Where(user => !string.Equals(user.Role, AdminRoleName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminUserDetailsDto>> GetUserDetails(Guid userId, CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        try
        {
            AdminUserDetailsDto result = await _getAdminUserDetailsHandler.Handle(userId, cancellationToken);

            if (IsCurrentUserSupport() && IsAdminRole(result.Role))
            {
                return NotFound(new { message = "Потребителят не беше намерен." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("users/{userId:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TransactionHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<TransactionHistoryItemDto>>> GetUserTransactions(Guid userId, CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        try
        {
            AdminUserDetailsDto targetUser = await _getAdminUserDetailsHandler.Handle(userId, cancellationToken);

            if (IsCurrentUserSupport() && IsAdminRole(targetUser.Role))
            {
                return NotFound(new { message = "Потребителят не беше намерен." });
            }

            IReadOnlyCollection<TransactionHistoryItemDto> result = await _getAdminUserTransactionsHandler.Handle(userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("support-accounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupportAccountResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SupportAccountResultDto>> CreateSupportAccount(
        [FromBody] CreateSupportAccountRequest request,
        CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        CreateSupportAccountCommand command = new()
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        try
        {
            SupportAccountResultDto result = await _createSupportAccountHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private ObjectResult? EnsureCompletedSecuritySetup()
    {
        if (string.Equals(
                User.FindFirstValue(AuthClaimNames.SecuritySetupRequired),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Първо завърши настройката на двуфакторната защита, за да отвориш административната част."
            });
        }

        return null;
    }

    private bool IsCurrentUserSupport()
    {
        return User.IsInRole(SupportRoleName);
    }

    private static bool IsAdminRole(string role)
    {
        return string.Equals(role, AdminRoleName, StringComparison.OrdinalIgnoreCase);
    }
}
