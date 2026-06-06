using SecureWallet.Application.Interfaces.Security;
using System.Security.Cryptography;
using System.Text;

namespace SecureWallet.Infrastructure.Security;

public class TestCaptchaVerificationService : ICaptchaVerificationService
{
    private const string CaptchaCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CaptchaLength = 4;
    private const int CaptchaWidth = 160;
    private const int CaptchaHeight = 60;

    public string GenerateCaptchaCode()
    {
        char[] captchaCharacters = new char[CaptchaLength];

        for (int index = 0; index < CaptchaLength; index++)
        {
            int randomCharacterIndex = RandomNumberGenerator.GetInt32(CaptchaCharacters.Length);
            captchaCharacters[index] = CaptchaCharacters[randomCharacterIndex];
        }

        return new string(captchaCharacters);
    }

    public string GenerateCaptchaImageBase64(string captchaCode)
    {
        StringBuilder svgBuilder = new();

        svgBuilder.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{CaptchaWidth}"" height=""{CaptchaHeight}"" viewBox=""0 0 {CaptchaWidth} {CaptchaHeight}"">");
        svgBuilder.AppendLine(@"  <rect width=""100%"" height=""100%"" fill=""#f4f7fb"" rx=""8"" ry=""8"" />");

        for (int index = 0; index < 6; index++)
        {
            int x1 = RandomNumberGenerator.GetInt32(CaptchaWidth);
            int y1 = RandomNumberGenerator.GetInt32(CaptchaHeight);
            int x2 = RandomNumberGenerator.GetInt32(CaptchaWidth);
            int y2 = RandomNumberGenerator.GetInt32(CaptchaHeight);

            svgBuilder.AppendLine($@"  <line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" stroke=""#cbd5e1"" stroke-width=""1"" />");
        }

        for (int index = 0; index < CaptchaLength; index++)
        {
            char character = captchaCode[index];
            int x = 20 + (index * 32);
            int y = 38 + RandomNumberGenerator.GetInt32(-6, 7);
            int rotation = RandomNumberGenerator.GetInt32(-20, 21);
            string fillColor = index % 2 == 0 ? "#1e293b" : "#334155";

            svgBuilder.AppendLine($@"  <text x=""{x}"" y=""{y}"" font-family=""Arial, sans-serif"" font-size=""28"" font-weight=""700"" fill=""{fillColor}"" transform=""rotate({rotation} {x} {y})"">{character}</text>");
        }

        svgBuilder.AppendLine(@"</svg>");

        byte[] svgBytes = Encoding.UTF8.GetBytes(svgBuilder.ToString());
        return Convert.ToBase64String(svgBytes);
    }

    public bool IsValid(string? providedCaptchaToken, string expectedCaptchaCode)
    {
        return !string.IsNullOrWhiteSpace(providedCaptchaToken) &&
               providedCaptchaToken == expectedCaptchaCode;
    }
}
