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

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Wallets.AnyAsync(x => x.Username == username, cancellationToken);
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Wallets.FindAsync([id], cancellationToken);
    }

    public void Add(Wallet wallet)
    {
        _dbContext.Wallets.Add(wallet);
    }
}