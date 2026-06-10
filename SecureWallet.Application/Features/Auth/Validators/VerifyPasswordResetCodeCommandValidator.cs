using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class VerifyPasswordResetCodeCommandValidator : AbstractValidator<VerifyPasswordResetCodeCommand>
{
    public VerifyPasswordResetCodeCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Телефонният номер"))
            .Must(FluentValidationRuleExtensions.IsValidPhoneNumber).WithMessage(FluentValidationRuleExtensions.PhoneRulesMessage());

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage(FluentValidationRuleExtensions.VerificationCodeFieldName()))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage(FluentValidationRuleExtensions.VerificationCodeFieldName()));
    }
}