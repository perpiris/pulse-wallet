using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.GetWalletLedgerEntries;
using PulseWallet.Domain.Ledger;

namespace PulseWallet.Application.UnitTests.Wallets.GetWalletLedgerEntries;

public sealed class GetWalletLedgerEntriesHandlerTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();
    private static readonly Guid WalletId = Guid.CreateVersion7();

    private readonly IWalletQueries _walletQueries = Substitute.For<IWalletQueries>();
    private readonly GetWalletLedgerEntriesHandler _handler;

    public GetWalletLedgerEntriesHandlerTests()
    {
        _handler = new GetWalletLedgerEntriesHandler(_walletQueries);
    }

    [Fact]
    public async Task HandleAsync_WithDateRange_QueriesHalfOpenUtcRangeIncludingTheWholeLastDay()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LedgerEntriesCursor before = new LedgerEntriesCursor(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero), Guid.CreateVersion7());
        GetWalletLedgerEntriesQuery query = new GetWalletLedgerEntriesQuery(
            WalletId, OwnerId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 20), LedgerEntryDirection.Outgoing, before);
        LedgerEntriesPage page = new LedgerEntriesPage([], null);

        LedgerEntriesFilter? filter = null;
        _walletQueries
            .GetLedgerEntriesAsync(WalletId, OwnerId, Arg.Do<LedgerEntriesFilter>(value => filter = value), cancellationToken)
            .Returns(page);

        LedgerEntriesPage result = await _handler.HandleAsync(query, cancellationToken);

        Assert.Same(page, result);
        Assert.NotNull(filter);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), filter.From);
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 0, 0, 0, TimeSpan.Zero), filter.ToExclusive);
        Assert.Equal(LedgerEntryDirection.Outgoing, filter.Direction);
        Assert.Same(before, filter.Before);
        Assert.True(filter.PageSize > 0);
    }

    [Fact]
    public async Task HandleAsync_WithoutDates_QueriesWithoutBounds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GetWalletLedgerEntriesQuery query = new GetWalletLedgerEntriesQuery(WalletId, OwnerId, null, null, null, null);

        LedgerEntriesFilter? filter = null;
        _walletQueries
            .GetLedgerEntriesAsync(WalletId, OwnerId, Arg.Do<LedgerEntriesFilter>(value => filter = value), cancellationToken)
            .Returns(new LedgerEntriesPage([], null));

        await _handler.HandleAsync(query, cancellationToken);

        Assert.NotNull(filter);
        Assert.Null(filter.From);
        Assert.Null(filter.ToExclusive);
    }

    [Fact]
    public async Task HandleAsync_WalletMissingOrNotOwned_ThrowsNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GetWalletLedgerEntriesQuery query = new GetWalletLedgerEntriesQuery(WalletId, OwnerId, null, null, null, null);
        _walletQueries
            .GetLedgerEntriesAsync(WalletId, OwnerId, Arg.Any<LedgerEntriesFilter>(), cancellationToken)
            .Returns((LedgerEntriesPage?)null);

        await Assert.ThrowsAsync<WalletNotFoundException>(() => _handler.HandleAsync(query, cancellationToken));
    }
}