using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.VerifyEmail;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class ResendEmailVerificationCodeCommandValidator : AbstractValidator<ResendEmailVerificationCodeCommand>
{
    public ResendEmailVerificationCodeCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());
    }
}