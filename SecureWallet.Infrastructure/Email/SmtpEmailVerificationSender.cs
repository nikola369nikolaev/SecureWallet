using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Infrastructure.Options;

namespace SecureWallet.Infrastructure.Email;

public class SmtpEmailVerificationSender : IEmailVerificationSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailVerificationSender> _logger;

    public SmtpEmailVerificationSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailVerificationSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendRegistrationVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpClient) ||
            _options.SmtpPort <= 0 ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError(
                "Имейл верификацията не е конфигурирана. Хост: {HasHost}, Порт: {Port}, Потребител: {HasUsername}, Парола: {HasPassword}, FromEmail: {HasFromEmail}.",
                !string.IsNullOrWhiteSpace(_options.SmtpClient),
                _options.SmtpPort,
                !string.IsNullOrWhiteSpace(_options.Username),
                !string.IsNullOrWhiteSpace(_options.Password),
                !string.IsNullOrWhiteSpace(_options.FromEmail));

            throw new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");
        }

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.Sender = MailboxAddress.Parse(_options.FromEmail);
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Код за потвърждение на имейл";
        message.Body = new TextPart("plain")
        {
            Text =
                "Здравей,\n\n" +
                $"Твоят код за потвърждение в SecureWallet е: {code}\n\n" +
                "Кодът е валиден 10 минути.\n\n" +
                "Ако не си поискал регистрация, можеш да игнорираш това съобщение."
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using SmtpClient client = new();
            SecureSocketOptions socketOptions = _options.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_options.SmtpClient, _options.SmtpPort, socketOptions, cancellationToken);
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Изпращането на имейл за потвърждение се провали за {RecipientEmail}. SMTP хост: {SmtpHost}, порт: {SmtpPort}.",
                email,
                _options.SmtpClient,
                _options.SmtpPort);

            throw new InvalidOperationException("Възникна проблем. Опитай по-късно.");
        }
    }
}
