using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class TestSmsVerificationService : ISmsVerificationService
{
    private readonly IConfiguration _configuration;

    public TestSmsVerificationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<SmsVerificationDispatchResult> SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(_configuration["SMS:Username"]!));
        message.To.Add(MailboxAddress.Parse($"{phoneNumber.Replace("+","")}@sms.yettel.bg"));
        message.Subject = "Kod za podtvurjdenie";

        BodyBuilder builder = new()
        {
            TextBody = "Kod: " + code,
        };
        message.Body = builder.ToMessageBody();

        using SmtpClient client = new();
        await client.ConnectAsync(_configuration["SMS:SmtpClient"]!, int.Parse(_configuration["SMS:SmtpPort"]!), SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_configuration["SMS:Username"]!, _configuration["SMS:Password"]!);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        
        SmsVerificationDispatchResult result = new()
        {
            Message = $"СМС код беше генериран за {phoneNumber}."
        };

        return result;
    }
}
