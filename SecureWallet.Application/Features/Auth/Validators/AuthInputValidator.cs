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

        if (!phoneNumber.StartsWith("+359"))
        {
            throw new InvalidOperationException("Телефонният номер трябва да започва с '+359'.");
        }

        string subscriberNumber = phoneNumber[4..];

        if (subscriberNumber.Length != 9)
        {
            throw new InvalidOperationException("Телефонният номер трябва да съдържа точно 9 цифри след +359.");
        }

        if (!subscriberNumber.All(char.IsDigit))
        {
            throw new InvalidOperationException("Телефонният номер трябва да започва с '+359' и след него да има само цифри.");
        }

        if (subscriberNumber[0] == '0')
        {
            throw new InvalidOperationException("Първата цифра след +359 не може да бъде 0.");
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
