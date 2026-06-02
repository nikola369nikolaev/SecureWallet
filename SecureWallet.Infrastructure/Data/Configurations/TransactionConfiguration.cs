using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .IsRequired();

        builder.Property(transaction => transaction.Reference)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(500);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(transaction => transaction.Reference)
            .IsUnique();

        builder.HasOne(transaction => transaction.SenderWallet)
            .WithMany(wallet => wallet.SentTransactions)
            .HasForeignKey(transaction => transaction.SenderWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.ReceiverWallet)
            .WithMany(wallet => wallet.ReceivedTransactions)
            .HasForeignKey(transaction => transaction.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
