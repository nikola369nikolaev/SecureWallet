using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.Refresh;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Refresh token"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Refresh token"));
    }
}