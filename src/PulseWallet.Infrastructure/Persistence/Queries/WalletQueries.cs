using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Ledger;

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
                    .Select(e => new LedgerEntryDetails(e.Id, e.Type, e.Amount.Amount, e.Amount.Currency, e.CreatedAt, e.TransferId))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LedgerEntriesPage?> GetLedgerEntriesAsync(
        Guid walletId,
        LedgerEntriesFilter filter,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Wallets.AnyAsync(w => w.Id == walletId, cancellationToken))
        {
            return null;
        }

        IQueryable<LedgerEntry> entries = _dbContext.LedgerEntries.Where(e => e.WalletId == walletId);

        if (filter.From is not null)
        {
            DateTimeOffset from = filter.From.Value;
            entries = entries.Where(e => e.CreatedAt >= from);
        }

        if (filter.ToExclusive is not null)
        {
            DateTimeOffset toExclusive = filter.ToExclusive.Value;
            entries = entries.Where(e => e.CreatedAt < toExclusive);
        }

        if (filter.Direction is not null)
        {
            // Not an array: on an array, C# 14 binds Contains to the span overload (MemoryExtensions.Contains),
            // which EF Core 10 translates but tools and older providers don't. IReadOnlyCollection binds to
            // Enumerable.Contains, the LINQ method every provider knows → "WHERE Type IN (...)".
            IReadOnlyCollection<LedgerEntryType> types = LedgerEntryTypes.For(filter.Direction.Value);
            entries = entries.Where(e => types.Contains(e.Type));
        }

        if (filter.Before is not null)
        {
            // Keyset: everything after the cursor in the (CreatedAt DESC, Id DESC) order.
            DateTimeOffset createdAt = filter.Before.CreatedAt;
            Guid id = filter.Before.Id;
            entries = entries.Where(e => e.CreatedAt < createdAt || (e.CreatedAt == createdAt && e.Id < id));
        }

        // One more than a page: if it comes back, there is a next page (no COUNT query needed).
        List<LedgerEntryDetails> items = await entries
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(filter.PageSize + 1)
            .Select(e => new LedgerEntryDetails(e.Id, e.Type, e.Amount.Amount, e.Amount.Currency, e.CreatedAt, e.TransferId))
            .ToListAsync(cancellationToken);

        if (items.Count <= filter.PageSize)
        {
            return new LedgerEntriesPage(items, NextCursor: null);
        }

        items.RemoveAt(items.Count - 1);
        LedgerEntryDetails last = items[^1];
        return new LedgerEntriesPage(items, new LedgerEntriesCursor(last.CreatedAt, last.Id).Encode());
    }
}