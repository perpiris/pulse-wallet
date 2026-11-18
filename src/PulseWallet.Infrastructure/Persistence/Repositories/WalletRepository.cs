using Microsoft.EntityFrameworkCore;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly PulseWalletDbContext _dbContext;

    public WalletRepository(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Compared in SQL, so with the database's case-insensitive collation: "Savings" and "savings" count as the same,
    // exactly like the unique index that backs this check.
    public async Task<bool> FriendlyNameExistsAsync(Guid ownerId, string friendlyName, CancellationToken cancellationToken)
    {
        return await _dbContext.Wallets.AnyAsync(
            w => w.OwnerId == ownerId && w.FriendlyName == friendlyName,
            cancellationToken);
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Wallets.FindAsync([id], cancellationToken);
    }

    public void Add(Wallet wallet)
    {
        _dbContext.Wallets.Add(wallet);
    }
}