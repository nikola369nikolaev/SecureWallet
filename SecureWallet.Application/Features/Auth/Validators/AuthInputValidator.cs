using System.ComponentModel.DataAnnotations;

namespace SecureWallet.Application.Features.Auth.Validators;

public static class AuthInputValidator
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static void ValidateRequiredField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }
    }

    public static void ValidateNoLeadingOrTrailingWhitespace(string value, string fieldName)
    {
        if (value != value.Trim())
        {
            throw new InvalidOperationException($"{fieldName} must not start or end with whitespace.");
        }
    }

    public static void ValidateOptionalNoLeadingOrTrailingWhitespace(string? value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (value != value.Trim())
        {
            throw new InvalidOperationException($"{fieldName} must not start or end with whitespace.");
        }
    }

    public static void ValidateEmail(string email)
    {
        ValidateRequiredField(email, "Email");
        ValidateNoLeadingOrTrailingWhitespace(email, "Email");

        if (!EmailValidator.IsValid(email))
        {
            throw new InvalidOperationException("Email format is invalid.");
        }
    }

    public static void ValidateUsername(string username)
    {
        ValidateRequiredField(username, "Username");
        ValidateNoLeadingOrTrailingWhitespace(username, "Username");

        if (username.Length < 3 || username.Length > 30)
        {
            throw new InvalidOperationException("Username must be between 3 and 30 characters long.");
        }

        bool hasInvalidCharacter = username.Any(character =>
            !char.IsLetterOrDigit(character) &&
            character != '_' &&
            character != '.');

        if (hasInvalidCharacter)
        {
            throw new InvalidOperationException(
                "Username may contain only Latin letters, digits, underscores, and dots.");
        }
    }

    public static void ValidatePhoneNumber(string? phoneNumber)
    {
        ValidateOptionalNoLeadingOrTrailingWhitespace(phoneNumber, "Phone number");

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return;
        }

        if (!phoneNumber.StartsWith('+'))
        {
            throw new InvalidOperationException("Phone number must start with '+'.");
        }

        string digits = phoneNumber[1..];

        if (digits.Length < 8 || digits.Length > 15)
        {
            throw new InvalidOperationException("Phone number must contain between 8 and 15 digits after '+'.");
        }

        if (!digits.All(char.IsDigit))
        {
            throw new InvalidOperationException("Phone number must start with '+' and contain only digits after it.");
        }
    }
}
