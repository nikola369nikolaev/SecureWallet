using FluentValidation;
using FluentValidation.Results;

namespace SecureWallet.Application.Validation;

public static class ValidationExecutionExtensions
{
    public static async Task ValidateAndThrowInvalidOperationAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default,
        bool combineAllMessages = false)
    {
        ValidationResult validationResult = await validator.ValidateAsync(instance, cancellationToken);

        if (validationResult.IsValid)
        {
            return;
        }

        string[] messages = validationResult.Errors
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToArray();

        if (messages.Length == 0)
        {
            throw new InvalidOperationException("Входните данни не са валидни.");
        }

        throw new InvalidOperationException(
            combineAllMessages
                ? string.Join(" ", messages)
                : messages[0]);
    }
}