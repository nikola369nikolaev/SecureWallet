using System.Text;
using QRCoder;
using SecureWallet.Application.Interfaces.Security;

namespace SecureWallet.Infrastructure.Security;

public class QrCodeService : IQrCodeService
{
    public string GenerateSvgDataUri(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("QR code content cannot be empty.");
        }

        using QRCodeGenerator generator = new();
        using QRCodeData qrCodeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

        SvgQRCode svgQrCode = new(qrCodeData);
        string svgMarkup = svgQrCode.GetGraphic(20);

        string base64Svg = Convert.ToBase64String(Encoding.UTF8.GetBytes(svgMarkup));
        return $"data:image/svg+xml;base64,{base64Svg}";
    }
}
