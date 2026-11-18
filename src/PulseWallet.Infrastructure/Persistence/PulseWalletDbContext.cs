using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Abstractions;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;
using PulseWallet.Infrastructure.Identity;

namespace PulseWallet.Infrastructure.Persistence;

// One context for the business tables and the Identity tables (AspNetUsers, ...): one database, one migration
// history, and a wallet's owner can be a real foreign key to AspNetUsers.
public class PulseWalletDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public PulseWalletDbContext(DbContextOptions<PulseWalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Maps the Identity tables; must run before our own configurations.
        base.OnModelCreating(builder);

        // Picks up every IEntityTypeConfiguration<T> in this project (Configurations/),
        // so adding a new entity never requires changing this method.
        builder.ApplyConfigurationsFromAssembly(typeof(PulseWalletDbContext).Assembly);
    }
}