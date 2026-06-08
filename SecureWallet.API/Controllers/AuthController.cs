using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.API.Requests.Auth;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Refresh;
using SecureWallet.Application.Features.Auth.Commands.Register;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Features.Auth.Commands.Totp;
using SecureWallet.Application.Features.Auth.Commands.VerifyEmail;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Exceptions;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly VerifyEmailCodeHandler _verifyEmailCodeHandler;
    private readonly ResendEmailVerificationCodeHandler _resendEmailVerificationCodeHandler;
    private readonly LoginUserHandler _loginUserHandler;
    private readonly RefreshSessionHandler _refreshSessionHandler;
    private readonly RequestPasswordResetCodeHandler _requestPasswordResetCodeHandler;
    private readonly VerifyPasswordResetCodeHandler _verifyPasswordResetCodeHandler;
    private readonly ResetPasswordHandler _resetPasswordHandler;
    private readonly BeginTotpSetupHandler _beginTotpSetupHandler;
    private readonly VerifyTotpSetupHandler _verifyTotpSetupHandler;
    private readonly DisableTotpHandler _disableTotpHandler;
    private readonly ResetTotpSetupHandler _resetTotpSetupHandler;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        VerifyEmailCodeHandler verifyEmailCodeHandler,
        ResendEmailVerificationCodeHandler resendEmailVerificationCodeHandler,
        LoginUserHandler loginUserHandler,
        RefreshSessionHandler refreshSessionHandler,
        RequestPasswordResetCodeHandler requestPasswordResetCodeHandler,
        VerifyPasswordResetCodeHandler verifyPasswordResetCodeHandler,
        ResetPasswordHandler resetPasswordHandler,
        BeginTotpSetupHandler beginTotpSetupHandler,
        VerifyTotpSetupHandler verifyTotpSetupHandler,
        DisableTotpHandler disableTotpHandler,
        ResetTotpSetupHandler resetTotpSetupHandler)
    {
        _registerUserHandler = registerUserHandler;
        _verifyEmailCodeHandler = verifyEmailCodeHandler;
        _resendEmailVerificationCodeHandler = resendEmailVerificationCodeHandler;
        _loginUserHandler = loginUserHandler;
        _refreshSessionHandler = refreshSessionHandler;
        _requestPasswordResetCodeHandler = requestPasswordResetCodeHandler;
        _verifyPasswordResetCodeHandler = verifyPasswordResetCodeHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _beginTotpSetupHandler = beginTotpSetupHandler;
        _verifyTotpSetupHandler = verifyTotpSetupHandler;
        _disableTotpHandler = disableTotpHandler;
        _resetTotpSetupHandler = resetTotpSetupHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResultDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        RegisterUserCommand command = new()
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        try
        {
            RegisterResultDto result = await _registerUserHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(EmailVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailVerificationResultDto>> VerifyEmail(
        [FromBody] VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        VerifyEmailCodeCommand command = new()
        {
            Email = request.Email,
            Code = request.Code
        };

        try
        {
            EmailVerificationResultDto result = await _verifyEmailCodeHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("verify-email/resend")]
    [ProducesResponseType(typeof(EmailVerificationCodeDispatchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailVerificationCodeDispatchResultDto>> ResendEmailVerificationCode(
        [FromBody] ResendEmailVerificationCodeRequest request,
        CancellationToken cancellationToken)
    {
        ResendEmailVerificationCodeCommand command = new()
        {
            Email = request.Email
        };

        try
        {
            EmailVerificationCodeDispatchResultDto result = await _resendEmailVerificationCodeHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        LoginUserCommand command = new()
        {
            Email = request.Email,
            Password = request.Password,
            CaptchaToken = request.CaptchaToken,
            TotpCode = request.TotpCode
        };

        try
        {
            LoginResultDto result = await _loginUserHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (LoginProtectionException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                requiresCaptcha = exception.RequiresCaptcha,
                requiresTotp = exception.RequiresTotp,
                requiresEmailVerification = exception.RequiresEmailVerification,
                email = exception.Email,
                captchaImageBase64 = exception.CaptchaImageBase64,
                lockoutSeconds = exception.LockoutSeconds
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshSessionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefreshSessionResultDto>> RefreshSession(
        [FromBody] RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        RefreshSessionCommand command = new()
        {
            UserId = request.UserId,
            RefreshToken = request.RefreshToken
        };

        try
        {
            RefreshSessionResultDto result = await _refreshSessionHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("reset-password/request-code")]
    [ProducesResponseType(typeof(PasswordResetCodeDispatchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasswordResetCodeDispatchResultDto>> RequestResetPasswordCode(
        [FromBody] RequestPasswordResetCodeRequest request,
        CancellationToken cancellationToken)
    {
        RequestPasswordResetCodeCommand command = new()
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        try
        {
            PasswordResetCodeDispatchResultDto result = await _requestPasswordResetCodeHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("reset-password/verify-code")]
    [ProducesResponseType(typeof(PasswordResetCodeVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasswordResetCodeVerificationResultDto>> VerifyResetPasswordCode(
        [FromBody] VerifyPasswordResetCodeRequest request,
        CancellationToken cancellationToken)
    {
        VerifyPasswordResetCodeCommand command = new()
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Code = request.Code
        };

        try
        {
            PasswordResetCodeVerificationResultDto result = await _verifyPasswordResetCodeHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("reset-password/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        ResetPasswordCommand command = new()
        {
            ResetSessionToken = request.ResetSessionToken,
            NewPassword = request.NewPassword
        };

        try
        {
            await _resetPasswordHandler.Handle(command, cancellationToken);
            return Ok(new { message = "Паролата беше сменена успешно." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpGet("totp/setup")]
    [ProducesResponseType(typeof(TotpSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TotpSetupDto>> BeginTotpSetup(CancellationToken cancellationToken)
    {
        try
        {
            Guid userId = GetCurrentUserId();
            TotpSetupDto result = await _beginTotpSetupHandler.Handle(userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("totp/verify-setup")]
    [ProducesResponseType(typeof(TotpVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TotpVerificationResultDto>> VerifyTotpSetup(
        [FromBody] VerifyTotpSetupRequest request,
        CancellationToken cancellationToken)
    {
        VerifyTotpSetupCommand command = new()
        {
            UserId = GetCurrentUserId(),
            Code = request.Code
        };

        try
        {
            TotpVerificationResultDto result = await _verifyTotpSetupHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("totp/disable")]
    [ProducesResponseType(typeof(TotpVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TotpVerificationResultDto>> DisableTotp(
        [FromBody] DisableTotpRequest request,
        CancellationToken cancellationToken)
    {
        DisableTotpCommand command = new()
        {
            UserId = GetCurrentUserId(),
            Code = request.Code
        };

        try
        {
            TotpVerificationResultDto result = await _disableTotpHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("totp/reset")]
    [ProducesResponseType(typeof(TotpSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TotpSetupDto>> ResetTotpSetup(
        [FromBody] ResetTotpSetupRequest request,
        CancellationToken cancellationToken)
    {
        ResetTotpSetupCommand command = new()
        {
            UserId = GetCurrentUserId(),
            Code = request.Code
        };

        try
        {
            TotpSetupDto result = await _resetTotpSetupHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              User.FindFirstValue(ClaimTypes.Name) ??
                              User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Текущият потребител не можа да бъде разпознат.");
        }

        return userId;
    }
}
