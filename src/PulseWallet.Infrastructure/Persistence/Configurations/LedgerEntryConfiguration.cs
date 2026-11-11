using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stored as text ("TopUp"), like Wallet.Currency: readable, and safe if the enum is reordered.
        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Money is a value object: no table or Id of its own, its values become columns of LedgerEntries.
        // Without HasColumnName, EF would name them Amount_Amount and Amount_Currency.
        builder.ComplexProperty(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // A wallet's history: filter by WalletId, read in CreatedAt order. Also serves WalletId-only queries,
        // so EF doesn't add a separate index for the foreign key.
        builder.HasIndex(e => new { e.WalletId, e.CreatedAt });

        // The guarantee behind idempotency: a key can create at most one entry per wallet, even if two
        // requests with the same key both pass the "seen it before?" check. Filtered on NOT NULL, so the
        // entries without a key (transfers) don't collide with each other.
        builder.HasIndex(e => new { e.WalletId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}