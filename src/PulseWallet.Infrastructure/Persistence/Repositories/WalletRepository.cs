using PulseWallet.Domain.Wallets;

namespace PulseWallet.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly PulseWalletDbContext _dbContext;

    public WalletRepository(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Add(Wallet wallet)
    {
        throw new NotImplementedException();
    }
}