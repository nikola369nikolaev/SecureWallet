using System.ComponentModel.DataAnnotations;

namespace SecureWallet.Application.Validation;

public static class FluentValidationRuleExtensions
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static string RequiredMessage(string fieldName) => $"{fieldName} е задължително поле.";

    public static string NoWhitespaceMessage(string fieldName) => $"{fieldName} не трябва да започва или да завършва с интервали.";

    public static string EmailFormatMessage() => "Имейл адресът не е в правилен формат.";

    public static string UsernameRulesMessage() =>
        "Потребителското име може да съдържа само латински букви, цифри, долна черта и точка и трябва да е между 3 и 30 символа.";

    public static string PhoneRulesMessage() =>
        "Телефонният номер трябва да започва с '+359', да съдържа точно 9 цифри след +359 и първата цифра след +359 да не е 0.";

    public static string NameNoDigitsMessage(string fieldName) => $"{fieldName} не трябва да съдържа цифри.";

    public static string PasswordMinLengthMessage() => "Паролата трябва да е поне 8 символа.";

    public static string PasswordUppercaseMessage() => "Паролата трябва да съдържа поне една главна буква.";

    public static string PasswordDigitMessage() => "Паролата трябва да съдържа поне една цифра.";

    public static string PasswordsDoNotMatchMessage() => "Паролите не съвпадат.";

    public static string VerificationCodeFieldName() => "Кодът за потвърждение";

    public static string TemporaryCodeFieldName() => "Временният код";

    public static string AmountPositiveMessage() => "Сумата трябва да е по-голяма от 0.";

    public static string AmountPrecisionMessage() => "Сумата може да има най-много 2 знака след десетичната запетая.";

    public static string DescriptionLengthMessage() => "Коментарът може да е най-много 500 символа.";

    public static string RecipientTypeNotSupportedMessage() => "Неподдържан тип получател.";

    public static string IbanFormatMessage() => "IBAN-ът трябва да е във валиден формат.";

    public static bool IsValidEmail(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && EmailValidator.IsValid(value);
    }

    public static bool HasNoLeadingOrTrailingWhitespace(string? value)
    {
        return string.IsNullOrEmpty(value) || value == value.Trim();
    }

    public static bool IsValidUsername(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length < 3 || value.Length > 30)
        {
            return false;
        }

        return value.All(character =>
            char.IsLetterOrDigit(character) ||
            character == '_' ||
            character == '.');
    }

    public static bool IsValidPhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value != value.Trim())
        {
            return false;
        }

        if (!value.StartsWith("+359", StringComparison.Ordinal))
        {
            return false;
        }

        string subscriberNumber = value[4..];

        if (subscriberNumber.Length != 9)
        {
            return false;
        }

        if (!subscriberNumber.All(char.IsDigit))
        {
            return false;
        }

        return subscriberNumber[0] != '0';
    }

    public static bool HasNoDigits(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsDigit);
    }
}