namespace SecureWallet.Application.Interfaces.Security;

public interface IQrCodeService
{
    string GenerateSvgDataUri(string content);
}
