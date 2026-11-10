using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Abstractions;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Infrastructure.Persistence;

public class PulseWalletDbContext : DbContext, IUnitOfWork
{
    public PulseWalletDbContext(DbContextOptions<PulseWalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this project (Configurations/),
        // so adding a new entity never requires changing this method.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseWalletDbContext).Assembly);
    }
}