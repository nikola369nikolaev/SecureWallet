using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(wallet => wallet.Id);

        builder.Property(wallet => wallet.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(wallet => wallet.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(wallet => wallet.IsActive)
            .IsRequired();

        builder.Property(wallet => wallet.Iban)
            .IsRequired()
            .HasMaxLength(22);

        builder.Property(wallet => wallet.CardNumber)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(wallet => wallet.CardCvv)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(wallet => wallet.CardCreatedAtUtc)
            .IsRequired();

        builder.Property(wallet => wallet.CardExpiresAtUtc)
            .IsRequired();

        builder.Property(wallet => wallet.CreatedAtUtc)
            .IsRequired();

        builder.Property(wallet => wallet.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(wallet => wallet.UserId)
            .IsUnique();

        builder.HasIndex(wallet => wallet.Iban)
            .IsUnique();

        builder.HasIndex(wallet => wallet.CardNumber)
            .IsUnique();

        builder.HasOne(wallet => wallet.User)
            .WithOne()
            .HasForeignKey<Wallet>(wallet => wallet.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
