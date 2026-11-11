using PulseWallet.Domain.Ledger;

namespace PulseWallet.Infrastructure.Persistence.Repositories;

// Writes only, and append-only: no update or delete. Reads go through the query services.
public class LedgerEntryRepository : ILedgerEntryRepository
{
    private readonly PulseWalletDbContext _dbContext;

    public LedgerEntryRepository(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(LedgerEntry entry)
    {
        _dbContext.LedgerEntries.Add(entry);
    }
}