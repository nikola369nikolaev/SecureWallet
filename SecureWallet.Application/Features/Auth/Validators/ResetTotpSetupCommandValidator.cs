using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.Totp;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class ResetTotpSetupCommandValidator : AbstractValidator<ResetTotpSetupCommand>
{
    public ResetTotpSetupCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()));
    }
}