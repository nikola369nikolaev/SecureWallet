using Microsoft.AspNetCore.Mvc;
using SecureWallet.API.Requests.Auth;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Register;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Exceptions;

namespace SecureWallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;

    public AuthController(RegisterUserHandler registerUserHandler, LoginUserHandler loginUserHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
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
                captchaCode = exception.CaptchaCode,
                lockoutSeconds = exception.LockoutSeconds
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
