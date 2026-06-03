using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.Password)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.FirstName)
            .HasMaxLength(50);

        builder.Property(user => user.LastName)
            .HasMaxLength(50);

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.TwoFactorEnabled)
            .IsRequired();

        builder.Property(user => user.FailedLoginAttempts)
            .IsRequired();

        builder.Property(user => user.CurrentCaptchaCode)
            .HasMaxLength(4);

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(user => user.Username)
            .IsUnique();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
