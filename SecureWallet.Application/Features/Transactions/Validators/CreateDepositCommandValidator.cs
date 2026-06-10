using FluentValidation;
using SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Transactions.Validators;

public class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(command => command.Amount)
            .GreaterThan(0m).WithMessage(FluentValidationRuleExtensions.AmountPositiveMessage())
            .Must(amount => decimal.Round(amount, 2) == amount).WithMessage(FluentValidationRuleExtensions.AmountPrecisionMessage());

        RuleFor(command => command.TotpCode)
            .NotEmpty().WithMessage(FluentValidationRuleExtensions.RequiredMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()))
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage(FluentValidationRuleExtensions.NoWhitespaceMessage(FluentValidationRuleExtensions.TemporaryCodeFieldName()));
    }
}