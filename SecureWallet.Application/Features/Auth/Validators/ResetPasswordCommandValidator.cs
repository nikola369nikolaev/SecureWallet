using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.ResetSessionToken)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Токенът за смяна на паролата"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Токенът за смяна на паролата"));

        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Парола"))
            .Must(password => password.Length >= 8).WithMessage(FluentValidationRuleExtensions.PasswordMinLengthMessage())
            .Must(password => password.Any(char.IsUpper)).WithMessage(FluentValidationRuleExtensions.PasswordUppercaseMessage())
            .Must(password => password.Any(char.IsDigit)).WithMessage(FluentValidationRuleExtensions.PasswordDigitMessage());
    }
}