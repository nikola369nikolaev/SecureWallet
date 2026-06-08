using System.Text;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Wallets;

public static class WalletCardGenerator
{
    private static readonly string[] BulgarianBankCodes =
    {
        "BNBG",
        "STSA",
        "UNCR",
        "RZBB",
        "BPBI"
    };

    public static void ApplyNewCardDetails(Wallet wallet)
    {
        DateTime nowUtc = DateTime.UtcNow;

        wallet.Iban = GenerateBulgarianIban();
        wallet.CardNumber = GenerateVisaCardNumber();
        wallet.CardCvv = Random.Shared.Next(100, 1000).ToString();
        wallet.CardCreatedAtUtc = nowUtc;
        wallet.CardExpiresAtUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddYears(Random.Shared.Next(3, 6))
            .AddMonths(Random.Shared.Next(0, 12));
    }

    public static void EnsureCardDetails(Wallet wallet)
    {
        if (string.IsNullOrWhiteSpace(wallet.Iban) ||
            string.IsNullOrWhiteSpace(wallet.CardNumber) ||
            string.IsNullOrWhiteSpace(wallet.CardCvv) ||
            wallet.CardCreatedAtUtc == default ||
            wallet.CardExpiresAtUtc == default)
        {
            ApplyNewCardDetails(wallet);
        }
    }

    private static string GenerateBulgarianIban()
    {
        string bankCode = BulgarianBankCodes[Random.Shared.Next(BulgarianBankCodes.Length)];
        string branchCode = Random.Shared.Next(0, 10000).ToString("0000");
        string accountNumber = Random.Shared.NextInt64(0, 10_000_000_000).ToString("0000000000");

        string ibanBody = $"{bankCode}{branchCode}{accountNumber}";
        string checkDigits = CalculateIbanCheckDigits("BG", ibanBody);
        return $"BG{checkDigits}{ibanBody}";
    }

    private static string GenerateVisaCardNumber()
    {
        StringBuilder builder = new();
        builder.Append('4');

        for (int index = 0; index < 14; index++)
        {
            builder.Append(Random.Shared.Next(0, 10));
        }

        int checkDigit = CalculateLuhnCheckDigit(builder.ToString());
        builder.Append(checkDigit);
        return builder.ToString();
    }

    private static string CalculateIbanCheckDigits(string countryCode, string ibanBody)
    {
        string rearranged = $"{ibanBody}{countryCode}00";
        int remainder = 0;

        foreach (char character in rearranged)
        {
            string value = char.IsLetter(character)
                ? (character - 'A' + 10).ToString()
                : character.ToString();

            foreach (char digit in value)
            {
                remainder = ((remainder * 10) + (digit - '0')) % 97;
            }
        }

        int checkDigits = 98 - remainder;
        return checkDigits.ToString("00");
    }

    private static int CalculateLuhnCheckDigit(string partialNumber)
    {
        int sum = 0;
        bool shouldDouble = true;

        for (int index = partialNumber.Length - 1; index >= 0; index--)
        {
            int digit = partialNumber[index] - '0';

            if (shouldDouble)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            shouldDouble = !shouldDouble;
        }

        return (10 - (sum % 10)) % 10;
    }
}
