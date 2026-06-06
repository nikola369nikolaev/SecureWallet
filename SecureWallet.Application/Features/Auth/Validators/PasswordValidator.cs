namespace SecureWallet.Application.Features.Auth.Validators;

public static class PasswordValidator
{
    public const int MinimumLength = 8;

    public static bool IsValid(string? password)
    {
        return Validate(password).Count == 0;
    }

    public static IReadOnlyCollection<string> Validate(string? password)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Паролата е задължително поле.");
            return errors;
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Паролата трябва да е поне {MinimumLength} символа.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Паролата трябва да съдържа поне една главна буква.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Паролата трябва да съдържа поне една цифра.");
        }

        return errors;
    }
}
