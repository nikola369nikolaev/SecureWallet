using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Парола"));
    }
}