using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.API.Requests.Transactions;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;
using SecureWallet.Application.Features.Transactions.Commands.CreateTransfer;
using SecureWallet.Application.Features.Transactions.DTOs;
using SecureWallet.Application.Features.Transactions.Queries.GetCurrentUserTransactionHistory;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransferHandler _createTransferHandler;
    private readonly CreateDepositHandler _createDepositHandler;
    private readonly GetCurrentUserTransactionHistoryHandler _getCurrentUserTransactionHistoryHandler;

    public TransactionsController(
        CreateTransferHandler createTransferHandler,
        CreateDepositHandler createDepositHandler,
        GetCurrentUserTransactionHistoryHandler getCurrentUserTransactionHistoryHandler)
    {
        _createTransferHandler = createTransferHandler;
        _createDepositHandler = createDepositHandler;
        _getCurrentUserTransactionHistoryHandler = getCurrentUserTransactionHistoryHandler;
    }

    [HttpPost("deposit")]
    [ProducesResponseType(typeof(DepositResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DepositResultDto>> CreateDeposit(
        [FromBody] CreateDepositRequest request,
        CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        CreateDepositCommand command = new()
        {
            UserId = userId,
            Amount = request.Amount,
            TotpCode = request.TotpCode
        };

        try
        {
            DepositResultDto result = await _createDepositHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransferResultDto>> CreateTransfer(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        CreateTransferCommand command = new()
        {
            SenderUserId = userId,
            RecipientType = request.RecipientType,
            RecipientValue = request.RecipientValue,
            Amount = request.Amount,
            Description = request.Description
        };

        try
        {
            TransferResultDto result = await _createTransferHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(TransactionHistoryPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransactionHistoryPageDto>> GetCurrentUserTransactionHistory(
        CancellationToken cancellationToken,
        [FromQuery] string? type,
        [FromQuery] string? dateRange,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        try
        {
            TransactionHistoryQueryParametersDto queryParameters = new()
            {
                Type = type ?? "All",
                DateRange = dateRange ?? "All",
                SearchTerm = searchTerm ?? string.Empty,
                Page = page,
                PageSize = pageSize
            };

            TransactionHistoryPageDto result = await _getCurrentUserTransactionHistoryHandler.Handle(
                userId,
                queryParameters,
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
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
                message = "Първо завърши настройката на двуфакторната защита, за да използваш преводи и история."
            });
        }

        return null;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out userId);
    }
}
