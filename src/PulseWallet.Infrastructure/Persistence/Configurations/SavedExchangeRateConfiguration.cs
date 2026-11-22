using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWallet.Domain.Shared;
using PulseWallet.Infrastructure.ExchangeRates;

namespace PulseWallet.Infrastructure.Persistence.Configurations;

internal sealed class SavedExchangeRateConfiguration : IEntityTypeConfiguration<SavedExchangeRate>
{
    public void Configure(EntityTypeBuilder<SavedExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        // The pair is the identity: one row per pair, so a fetch overwrites instead of piling up rows.
        builder.HasKey(r => new { r.From, r.To });

        builder.Property(r => r.From)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(r => r.To)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(r => r.Rate)
            .HasPrecision(18, ExchangeRate.MaxDecimals);
    }
}