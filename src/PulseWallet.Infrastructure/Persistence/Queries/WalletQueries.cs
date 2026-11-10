using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;

namespace PulseWallet.Infrastructure.Persistence.Queries;

internal sealed class WalletQueries : IWalletQueries
{
    private readonly PulseWalletDbContext _dbContext;

    public WalletQueries(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // One SQL statement. A projection selects only the needed columns and never tracks anything,
    // so no AsNoTracking() is needed.
    public async Task<WalletDetails?> GetDetailsAsync(Guid walletId, int recentEntries, CancellationToken cancellationToken)
    {
        return await _dbContext.Wallets
            .Where(w => w.Id == walletId)
            .Select(w => new WalletDetails(
                w.Id,
                w.Username,
                w.Currency,
                w.Balance,
                w.CreatedAt,
                _dbContext.LedgerEntries
                    // Filter on the parameter, not on w.Id: with w.Id, EF joins to a ROW_NUMBER() over the
                    // whole table; with the parameter, SQL Server seeks the (WalletId, CreatedAt) index.
                    .Where(e => e.WalletId == walletId)
                    // CreatedAt gives the order; Id only breaks ties so the order is deterministic.
                    // (SQL Server doesn't sort uniqueidentifier by the v7 timestamp.)
                    .OrderByDescending(e => e.CreatedAt)
                    .ThenByDescending(e => e.Id)
                    .Take(recentEntries)
                    .Select(e => new LedgerEntryDetails(e.Id, e.Type, e.Amount.Amount, e.Amount.Currency, e.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}