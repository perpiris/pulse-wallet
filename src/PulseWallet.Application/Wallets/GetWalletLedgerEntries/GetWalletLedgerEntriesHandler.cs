using PulseWallet.Application.Ledger;

namespace PulseWallet.Application.Wallets.GetWalletLedgerEntries;

public sealed class GetWalletLedgerEntriesHandler
{
    private const int PageSize = 10;

    private readonly IWalletQueries _walletQueries;

    public GetWalletLedgerEntriesHandler(IWalletQueries walletQueries)
    {
        _walletQueries = walletQueries;
    }

    public async Task<LedgerEntriesPage> HandleAsync(GetWalletLedgerEntriesQuery query, CancellationToken cancellationToken)
    {
        // Calendar days become a half-open range of instants: [From 00:00, To + 1 day 00:00).
        // "<= To 00:00" would silently drop everything that happened during the last day.
        // Days are UTC for now: there's no user time zone yet.
        LedgerEntriesFilter filter = new LedgerEntriesFilter(
            From: StartOfDayUtc(query.From),
            ToExclusive: StartOfDayUtc(query.To?.AddDays(1)),
            Direction: query.Direction,
            Before: query.Before,
            PageSize: PageSize);

        LedgerEntriesPage? page = await _walletQueries.GetLedgerEntriesAsync(
            query.WalletId, query.UserId, filter, cancellationToken);
        if (page is null)
        {
            throw new WalletNotFoundException(query.WalletId);
        }

        return page;
    }

    private static DateTimeOffset? StartOfDayUtc(DateOnly? day)
    {
        if (day is null)
        {
            return null;
        }

        return new DateTimeOffset(day.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}