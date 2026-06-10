using FluentValidation;
using SecureWallet.Application.Features.Transactions.Commands.CreateTransfer;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Transactions.Validators;

public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(command => command.RecipientType)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Типът получател"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Типът получател"))
            .Must(type => type is "Username" or "PhoneNumber" or "Iban").WithMessage(FluentValidationRuleExtensions.RecipientTypeNotSupportedMessage());

        RuleFor(command => command.RecipientValue)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage("Получателят"))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Получателят"));

        When(command => command.RecipientType == "Username", () =>
        {
            RuleFor(command => command.RecipientValue)
                .Must(FluentValidationRuleExtensions.IsValidUsername)
                .WithMessage(FluentValidationRuleExtensions.UsernameRulesMessage());
        });

        When(command => command.RecipientType == "PhoneNumber", () =>
        {
            RuleFor(command => command.RecipientValue)
                .Must(FluentValidationRuleExtensions.IsValidPhoneNumber)
                .WithMessage(FluentValidationRuleExtensions.PhoneRulesMessage());
        });

        When(command => command.RecipientType == "Iban", () =>
        {
            RuleFor(command => command.RecipientValue)
                .Must(value =>
                {
                    string normalizedIban = value.Replace(" ", string.Empty);
                    return normalizedIban.Length == 22 && normalizedIban.All(char.IsLetterOrDigit);
                })
                .WithMessage(FluentValidationRuleExtensions.IbanFormatMessage());
        });

        RuleFor(command => command.Amount)
            .GreaterThan(0m).WithMessage(FluentValidationRuleExtensions.AmountPositiveMessage())
            .Must(amount => decimal.Round(amount, 2) == amount).WithMessage(FluentValidationRuleExtensions.AmountPrecisionMessage());

        RuleFor(command => command.Description)
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage("Коментарът"))
            .Must(description => string.IsNullOrEmpty(description) || description.Length <= 500).WithMessage(FluentValidationRuleExtensions.DescriptionLengthMessage());

        RuleFor(command => command.TotpCode)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()));
    }
}