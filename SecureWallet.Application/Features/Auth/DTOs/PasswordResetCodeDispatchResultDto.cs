namespace SecureWallet.Application.Features.Auth.DTOs;

public class PasswordResetCodeDispatchResultDto
{
    public string Message { get; set; } = string.Empty;

    public bool CanEnterCode { get; set; }

    public string? DevelopmentCodePreview { get; set; }
}
