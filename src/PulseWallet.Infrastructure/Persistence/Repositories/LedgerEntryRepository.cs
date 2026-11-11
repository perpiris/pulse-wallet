using Microsoft.EntityFrameworkCore;
using PulseWallet.Domain.Ledger;

namespace PulseWallet.Infrastructure.Persistence.Repositories;

// Append-only: no update or delete. History reads go through the query services.
public class LedgerEntryRepository : ILedgerEntryRepository
{
    private readonly PulseWalletDbContext _dbContext;

    public LedgerEntryRepository(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerEntry?> GetByIdempotencyKeyAsync(Guid walletId, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        return await _dbContext.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.WalletId == walletId && e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public void Add(LedgerEntry entry)
    {
        _dbContext.LedgerEntries.Add(entry);
    }
}