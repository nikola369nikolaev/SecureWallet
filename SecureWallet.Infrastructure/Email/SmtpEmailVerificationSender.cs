using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Infrastructure.Options;

namespace SecureWallet.Infrastructure.Email;

public class SmtpEmailVerificationSender : IEmailVerificationSender
{
    private readonly EmailOptions _options;

    public SmtpEmailVerificationSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendRegistrationVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpClient) ||
            _options.SmtpPort <= 0 ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("Имейл верификацията не е конфигурирана. Попълни Имейл настройките в API appsettings.json.");
        }

        using MailMessage message = new();
        message.From = new MailAddress(_options.Username, "SecureWallet");
        message.To.Add(email);
        message.Subject = "Код за потвърждение на имейл";
        message.Body =
            "Здравей,\n\n" +
            $"Твоят код за потвърждение в SecureWallet е: {code}\n\n" +
            "Кодът е валиден 10 минути.\n\n" +
            "Ако не си поискал регистрация, можеш да игнорираш това съобщение.";
        message.IsBodyHtml = false;

        using SmtpClient client = new(_options.SmtpClient, _options.SmtpPort)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
    }
}
