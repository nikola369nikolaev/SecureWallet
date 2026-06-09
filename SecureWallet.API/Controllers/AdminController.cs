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

    private readonly ILogger<AdminController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly GetAdminUsersHandler _getAdminUsersHandler;
    private readonly GetAdminUserDetailsHandler _getAdminUserDetailsHandler;
    private readonly GetAdminUserTransactionsHandler _getAdminUserTransactionsHandler;
    private readonly CreateSupportAccountHandler _createSupportAccountHandler;

    public AdminController(
        ILogger<AdminController> logger,
        IWebHostEnvironment environment,
        GetAdminUsersHandler getAdminUsersHandler,
        GetAdminUserDetailsHandler getAdminUserDetailsHandler,
        GetAdminUserTransactionsHandler getAdminUserTransactionsHandler,
        CreateSupportAccountHandler createSupportAccountHandler)
    {
        _logger = logger;
        _environment = environment;
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

        _logger.LogInformation(
            "Администрация: {Actor} отвори списъка с потребители. Role={Role}, VisibleUsers={Count}.",
            GetCurrentActor(),
            GetCurrentRole(),
            result.Count);
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
                _logger.LogWarning("Администрация: support акаунт {Actor} опита да отвори admin потребител {UserId}.", GetCurrentActor(), userId);
                return NotFound(new { message = "Потребителят не беше намерен." });
            }

            _logger.LogInformation("Администрация: {Actor} отвори детайли за потребител {UserId}.", GetCurrentActor(), userId);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning("Администрация: неуспешно отваряне на детайли за потребител {UserId}: {Reason}", userId, exception.Message);
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
                _logger.LogWarning("Администрация: support акаунт {Actor} опита да отвори транзакциите на admin потребител {UserId}.", GetCurrentActor(), userId);
                return NotFound(new { message = "Потребителят не беше намерен." });
            }

            IReadOnlyCollection<TransactionHistoryItemDto> result = await _getAdminUserTransactionsHandler.Handle(userId, cancellationToken);
            _logger.LogInformation("Администрация: {Actor} отвори транзакциите на потребител {UserId}.", GetCurrentActor(), userId);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning("Администрация: неуспешно отваряне на транзакции за потребител {UserId}: {Reason}", userId, exception.Message);
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
            _logger.LogInformation(
                "Администрация: admin акаунт {Actor} създаде support акаунт {Username} с имейл {Email}.",
                GetCurrentActor(),
                result.Username,
                result.Email);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning("Администрация: неуспешно създаване на support акаунт от {Actor}: {Reason}", GetCurrentActor(), exception.Message);
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminLogsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminLogsResultDto>> GetLatestLogs(
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        ObjectResult? securityResult = EnsureCompletedSecuritySetup();
        if (securityResult is not null)
        {
            return securityResult;
        }

        string? latestLogFilePath = GetLatestLogFilePath();
        if (latestLogFilePath is null)
        {
            return NotFound(new { message = "Все още няма генериран log файл." });
        }

        string[] allLines = await ReadAllLinesWithSharedAccessAsync(latestLogFilePath, cancellationToken);
        int normalizedTake = Math.Clamp(take, 1, 500);
        string[] currentSessionLines = FilterCurrentSessionLines(allLines);
        string[] lastLines = currentSessionLines
            .Skip(Math.Max(0, currentSessionLines.Length - normalizedTake))
            .ToArray();

        _logger.LogInformation(
            "Администрация: {Actor} отвори екрана с логовете. Файл={FileName}, ВърнатиРедове={Count}.",
            GetCurrentActor(),
            Path.GetFileName(latestLogFilePath),
            lastLines.Length);

        return Ok(new AdminLogsResultDto
        {
            FileName = Path.GetFileName(latestLogFilePath),
            LogDirectory = GetLogsDirectoryPath(),
            Lines = lastLines,
            ReturnedLineCount = lastLines.Length
        });
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

    private string GetCurrentActor()
    {
        return User.FindFirstValue(ClaimTypes.Email)
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? "неизвестен";
    }

    private string GetCurrentRole()
    {
        if (User.IsInRole(AdminRoleName))
        {
            return AdminRoleName;
        }

        if (User.IsInRole(SupportRoleName))
        {
            return SupportRoleName;
        }

        return "Неизвестна";
    }

    private string GetLogsDirectoryPath()
    {
        return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "logs"));
    }

    private string? GetLatestLogFilePath()
    {
        string logsDirectoryPath = GetLogsDirectoryPath();
        if (!Directory.Exists(logsDirectoryPath))
        {
            return null;
        }

        return Directory.GetFiles(logsDirectoryPath, "securewallet-*.log")
            .OrderByDescending(path => System.IO.File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();
    }

    private static async Task<string[]> ReadAllLinesWithSharedAccessAsync(string filePath, CancellationToken cancellationToken)
    {
        using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using StreamReader reader = new(stream);
        string content = await reader.ReadToEndAsync(cancellationToken);

        return content.Split(["\r\n", "\n"], StringSplitOptions.None);
    }

    private static string[] FilterCurrentSessionLines(string[] allLines)
    {
        const string apiStartedMessage = "SecureWallet API стартира успешно.";

        int lastStartupLineIndex = Array.FindLastIndex(
            allLines,
            line => line.Contains(apiStartedMessage, StringComparison.Ordinal));

        if (lastStartupLineIndex < 0)
        {
            return allLines;
        }

        return allLines
            .Skip(lastStartupLineIndex)
            .ToArray();
    }
}

public class AdminLogsResultDto
{
    public string FileName { get; set; } = string.Empty;

    public string LogDirectory { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Lines { get; set; } = Array.Empty<string>();

    public int ReturnedLineCount { get; set; }
}
