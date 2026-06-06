using System.ComponentModel.DataAnnotations;

namespace SecureWallet.Application.Features.Auth.Validators;

public static class AuthInputValidator
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static void ValidateRequiredField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} е задължително поле.");
        }
    }

    public static void ValidateNoLeadingOrTrailingWhitespace(string value, string fieldName)
    {
        if (value != value.Trim())
        {
            throw new InvalidOperationException($"{fieldName} не трябва да започва или да завършва с интервали.");
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
            throw new InvalidOperationException($"{fieldName} не трябва да започва или да завършва с интервали.");
        }
    }

    public static void ValidateEmail(string email)
    {
        ValidateRequiredField(email, "Имейл");
        ValidateNoLeadingOrTrailingWhitespace(email, "Имейл");

        if (!EmailValidator.IsValid(email))
        {
            throw new InvalidOperationException("Имейл адресът не е в правилен формат.");
        }
    }

    public static void ValidateUsername(string username)
    {
        ValidateRequiredField(username, "Потребителското име");
        ValidateNoLeadingOrTrailingWhitespace(username, "Потребителското име");

        if (username.Length < 3 || username.Length > 30)
        {
            throw new InvalidOperationException("Потребителското име трябва да е между 3 и 30 символа.");
        }

        bool hasInvalidCharacter = username.Any(character =>
            !char.IsLetterOrDigit(character) &&
            character != '_' &&
            character != '.');

        if (hasInvalidCharacter)
        {
            throw new InvalidOperationException(
                "Потребителското име може да съдържа само латински букви, цифри, долна черта и точка.");
        }
    }

    public static void ValidatePhoneNumber(string? phoneNumber)
    {
        ValidateOptionalNoLeadingOrTrailingWhitespace(phoneNumber, "Телефонният номер");

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return;
        }

        if (!phoneNumber.StartsWith('+'))
        {
            throw new InvalidOperationException("Телефонният номер трябва да започва с '+'.");
        }

        string digits = phoneNumber[1..];

        if (digits.Length < 8 || digits.Length > 15)
        {
            throw new InvalidOperationException("Телефонният номер трябва да съдържа между 8 и 15 цифри след '+'.");
        }

        if (!digits.All(char.IsDigit))
        {
            throw new InvalidOperationException("Телефонният номер трябва да започва с '+' и след него да има само цифри.");
        }
    }

    public static void ValidatePersonName(string value, string fieldName)
    {
        ValidateRequiredField(value, fieldName);
        ValidateNoLeadingOrTrailingWhitespace(value, fieldName);

        if (value.Any(char.IsDigit))
        {
            throw new InvalidOperationException($"{fieldName} не трябва да съдържа цифри.");
        }
    }
}
