using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class RequestPasswordResetCodeCommandValidator : AbstractValidator<RequestPasswordResetCodeCommand>
{
    public RequestPasswordResetCodeCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Телефонният номер"))
            .Must(FluentValidationRuleExtensions.IsValidPhoneNumber).WithMessage(FluentValidationRuleExtensions.PhoneRulesMessage());
    }
}