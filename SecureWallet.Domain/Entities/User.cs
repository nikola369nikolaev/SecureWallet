namespace SecureWallet.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public bool TwoFactorEnabled { get; set; }

    public int FailedLoginAttempts { get; set; }

    public string? CurrentCaptchaCode { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public string? PasswordResetCodeHash { get; set; }

    public DateTime? PasswordResetCodeExpiresAtUtc { get; set; }

    public int FailedPasswordResetCodeAttempts { get; set; }

    public DateTime? PasswordResetCodeLockoutEndUtc { get; set; }

    public string? PasswordResetSessionToken { get; set; }

    public DateTime? PasswordResetSessionExpiresAtUtc { get; set; }

    public string? TotpSecret { get; set; }

    public string? PendingTotpSecret { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }
}
