using System.Text;
using Microsoft.Extensions.Logging;
using QRCoder;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public string GenerateSvgDataUri(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogError("Генерирането на QR код се провали, защото входният текст е празен.");
            throw new InvalidOperationException("Възникна проблем. Опитай по-късно.");
        }

        using QRCodeGenerator generator = new();
        using QRCodeData qrCodeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

        SvgQRCode svgQrCode = new(qrCodeData);
        string svgMarkup = svgQrCode.GetGraphic(20);

        string base64Svg = Convert.ToBase64String(Encoding.UTF8.GetBytes(svgMarkup));
        return $"data:image/svg+xml;base64,{base64Svg}";
    }
}
