using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.SourceWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.ComplexProperty(t => t.ReceivedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ReceivedAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("ReceivedCurrency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Rates need more precision than money: 7.4744 DKK per EUR, 0.00006 for weak currencies.
        builder.Property(t => t.Rate)
            .HasPrecision(18, ExchangeRate.MaxDecimals);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Idempotency per sender: two requests with the same key can't both create a transfer.
        // Starts with SourceWalletId, so it also serves as the index for that foreign key.
        builder.HasIndex(t => new { t.SourceWalletId, t.IdempotencyKey })
            .IsUnique();
    }
}