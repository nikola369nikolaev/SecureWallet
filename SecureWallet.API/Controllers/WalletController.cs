using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Application.Features.Wallets.DTOs;
using SecureWallet.Application.Features.Wallets.Queries.GetCurrentUserWallet;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly GetCurrentUserWalletHandler _getCurrentUserWalletHandler;

    public WalletController(GetCurrentUserWalletHandler getCurrentUserWalletHandler)
    {
        _getCurrentUserWalletHandler = getCurrentUserWalletHandler;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(WalletSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WalletSummaryDto>> GetCurrentUserWallet(CancellationToken cancellationToken)
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Unauthorized();
        }

        try
        {
            WalletSummaryDto result = await _getCurrentUserWalletHandler.Handle(userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
