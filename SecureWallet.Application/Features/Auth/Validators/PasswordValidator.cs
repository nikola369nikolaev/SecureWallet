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
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number.");
        }

        return errors;
    }
}
