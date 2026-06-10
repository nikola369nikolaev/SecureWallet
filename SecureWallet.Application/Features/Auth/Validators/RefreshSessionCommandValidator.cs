using FluentValidation;
using SecureWallet.Application.Features.Auth.Commands.Refresh;
using SecureWallet.Application.Validation;

namespace SecureWallet.Application.Features.Auth.Validators;

public class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.ExpiredAccessToken)
            .NotEmpty().WithMessage("Изтеклият access token е задължително поле.")
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage("Изтеклият access token не трябва да започва или завършва с интервал.");

        RuleFor(command => command.TotpCode)
            .NotEmpty().WithMessage("Временният код е задължително поле.")
            .Must(FluentValidationRuleExtensions.HasNoLeadingOrTrailingWhitespace).WithMessage("Временният код не трябва да започва или завършва с интервал.");
    }
}
