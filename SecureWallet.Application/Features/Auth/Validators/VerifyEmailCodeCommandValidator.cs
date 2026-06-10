using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.VerifyEmail;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class VerifyEmailCodeCommandValidator : AbstractValidator<VerifyEmailCodeCommand>
{
    public VerifyEmailCodeCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage(FluentValidationRuleExtensions.VerificationCodeFieldName()))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage(FluentValidationRuleExtensions.VerificationCodeFieldName()));
    }
}