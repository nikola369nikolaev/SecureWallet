using FluentValidation;
using SecureWallet.Application.Features.Admin.Commands.CreateSupportAccount;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Admin.Validators;

public class CreateSupportAccountCommandValidator : AbstractValidator<CreateSupportAccountCommand>
{
    public CreateSupportAccountCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Потребителското име"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Потребителското име"))
            .Must(FluentValidationRuleExtensions.IsValidUsername).WithMessage(FluentValidationRuleExtensions.UsernameRulesMessage());

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Имейл"))
            .Must(FluentValidationRuleExtensions.IsValidEmail).WithMessage(FluentValidationRuleExtensions.EmailFormatMessage());

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Парола"))
            .Must(password => password.Length >= 8).WithMessage(FluentValidationRuleExtensions.PasswordMinLengthMessage())
            .Must(password => password.Any(char.IsUpper)).WithMessage(FluentValidationRuleExtensions.PasswordUppercaseMessage())
            .Must(password => password.Any(char.IsDigit)).WithMessage(FluentValidationRuleExtensions.PasswordDigitMessage());

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Собственото име"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Собственото име"))
            .Must(FluentValidationRuleExtensions.HasNoDigits).WithMessage(FluentValidationRuleExtensions.NameNoDigitsMessage("Собственото име"));

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Фамилията"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Фамилията"))
            .Must(FluentValidationRuleExtensions.HasNoDigits).WithMessage(FluentValidationRuleExtensions.NameNoDigitsMessage("Фамилията"));

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Телефонният номер"))
            .Must(FluentValidationRuleExtensions.IsValidPhoneNumber).WithMessage(FluentValidationRuleExtensions.PhoneRulesMessage());
    }
}