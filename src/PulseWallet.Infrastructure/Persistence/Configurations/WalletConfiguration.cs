using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWallet.Domain.Wallets;
using PulseWallet.Infrastructure.Identity;

namespace PulseWallet.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        // A real foreign key to the Identity users table, configured here so the domain stays unaware of it.
        // Restrict: deleting a user who still owns wallets fails instead of deleting their money's history.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(w => w.FriendlyName)
            .IsRequired()
            .HasMaxLength(Wallet.FriendlyNameMaxLength);

        // Unique per owner: the real guarantee, even if two requests pass the "does it exist?" check at the same moment.
        // OwnerId first, so the same index also serves "all wallets of this user" (it replaces the FK's own index).
        builder.HasIndex(w => new { w.OwnerId, w.FriendlyName })
            .IsUnique();

        // Stored as text ("EUR"), not as the enum's number, so the data stays readable
        // and reordering the enum can't silently change existing wallets.
        builder.Property(w => w.Currency)
            .HasConversion<string>()
            .HasMaxLength(3);

        // Money: 18 digits in total, 2 after the decimal point. Without this, EF Core warns
        // and SQL Server would use its own default precision.
        builder.Property(w => w.Balance)
            .HasPrecision(18, 2);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        // Optimistic concurrency: SQL Server changes a rowversion on every update, and EF adds
        // "WHERE RowVersion = <value when loaded>" to the UPDATE. If another request saved the wallet in between,
        // no row matches and EF throws DbUpdateConcurrencyException, so two concurrent balance changes
        // can't both apply to the same starting balance (no overdraft, no lost top-up).
        // A shadow property: it exists only in the EF model, so the domain class stays free of persistence details.
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();
    }
}