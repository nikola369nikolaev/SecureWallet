using Microsoft.AspNetCore.Mvc;
using SecureWallet.API.Requests.Auth;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Register;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Exceptions;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;
    private readonly RequestPasswordResetCodeHandler _requestPasswordResetCodeHandler;
    private readonly VerifyPasswordResetCodeHandler _verifyPasswordResetCodeHandler;
    private readonly ResetPasswordHandler _resetPasswordHandler;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginUserHandler loginUserHandler,
        RequestPasswordResetCodeHandler requestPasswordResetCodeHandler,
        VerifyPasswordResetCodeHandler verifyPasswordResetCodeHandler,
        ResetPasswordHandler resetPasswordHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
        _requestPasswordResetCodeHandler = requestPasswordResetCodeHandler;
        _verifyPasswordResetCodeHandler = verifyPasswordResetCodeHandler;
        _resetPasswordHandler = resetPasswordHandler;
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        LoginUserCommand command = new()
        {
            Email = request.Email,
            Password = request.Password,
            CaptchaToken = request.CaptchaToken
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
                captchaImageBase64 = exception.CaptchaImageBase64,
                lockoutSeconds = exception.LockoutSeconds
            });
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
            return Ok(new { message = "Password was reset successfully." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
