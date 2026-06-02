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

        builder.Property(wallet => wallet.CreatedAtUtc)
            .IsRequired();

        builder.Property(wallet => wallet.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(wallet => wallet.UserId)
            .IsUnique();

        builder.HasOne(wallet => wallet.User)
            .WithOne()
            .HasForeignKey<Wallet>(wallet => wallet.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
