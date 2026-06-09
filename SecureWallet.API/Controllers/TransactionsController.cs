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
    private readonly ILogger<TransactionsController> _logger;
    private readonly CreateTransferHandler _createTransferHandler;
    private readonly CreateDepositHandler _createDepositHandler;
    private readonly GetCurrentUserTransactionHistoryHandler _getCurrentUserTransactionHistoryHandler;

    public TransactionsController(
        ILogger<TransactionsController> logger,
        CreateTransferHandler createTransferHandler,
        CreateDepositHandler createDepositHandler,
        GetCurrentUserTransactionHistoryHandler getCurrentUserTransactionHistoryHandler)
    {
        _logger = logger;
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
            _logger.LogInformation(
                "Транзакции: потребител {UserId} направи депозит за {Amount}.",
                userId,
                request.Amount);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                "Транзакции: неуспешен депозит за потребител {UserId} за {Amount}: {Reason}",
                userId,
                request.Amount,
                exception.Message);
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
            TotpCode = request.TotpCode,
            Amount = request.Amount,
            Description = request.Description
        };

        try
        {
            TransferResultDto result = await _createTransferHandler.Handle(command, cancellationToken);
            _logger.LogInformation(
                "Транзакции: потребител {UserId} изпрати {Amount} към {RecipientType}={RecipientValue}.",
                userId,
                request.Amount,
                request.RecipientType,
                MaskRecipientValue(request.RecipientValue));
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                "Транзакции: неуспешен превод от потребител {UserId} към {RecipientType}={RecipientValue} за {Amount}: {Reason}",
                userId,
                request.RecipientType,
                MaskRecipientValue(request.RecipientValue),
                request.Amount,
                exception.Message);
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
        [FromQuery] int? month,
        [FromQuery] int? year,
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
                Month = month,
                Year = year,
                SearchTerm = searchTerm ?? string.Empty,
                Page = page,
                PageSize = pageSize
            };

            TransactionHistoryPageDto result = await _getCurrentUserTransactionHistoryHandler.Handle(
                userId,
                queryParameters,
                cancellationToken);
            _logger.LogInformation(
                "Транзакции: заредена е история за потребител {UserId}. Type={Type}, DateRange={DateRange}, Month={Month}, Year={Year}, Page={Page}, PageSize={PageSize}.",
                userId,
                queryParameters.Type,
                queryParameters.DateRange,
                queryParameters.Month,
                queryParameters.Year,
                queryParameters.Page,
                queryParameters.PageSize);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning("Транзакции: историята не беше заредена за потребител {UserId}: {Reason}", userId, exception.Message);
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

    private static string MaskRecipientValue(string recipientValue)
    {
        if (string.IsNullOrWhiteSpace(recipientValue))
        {
            return string.Empty;
        }

        if (recipientValue.Length <= 4)
        {
            return recipientValue;
        }

        return $"{recipientValue[..2]}***{recipientValue[^2..]}";
    }
}
