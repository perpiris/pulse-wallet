using Microsoft.Extensions.Logging.Abstractions;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.UnitTests.Wallets.TopUpWallet;

public sealed class TopUpWalletHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ILedgerEntryRepository _ledgerEntryRepository = Substitute.For<ILedgerEntryRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IWalletQueries _walletQueries = Substitute.For<IWalletQueries>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly TopUpWalletHandler _handler;
    private readonly Wallet _wallet = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);

    public TopUpWalletHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
        _walletRepository.GetByIdAsync(_wallet.Id, Arg.Any<CancellationToken>()).Returns(_wallet);
        _walletQueries
            .GetDetailsAsync(_wallet.Id, OwnerId, WalletDetails.RecentEntriesCount, Arg.Any<CancellationToken>())
            .Returns(call => new WalletDetails(_wallet.Id, "alice", Currency.EUR, _wallet.Balance, Now, []));

        _handler = new TopUpWalletHandler(
            _ledgerEntryRepository,
            _walletRepository,
            _unitOfWork,
            _walletQueries,
            _timeProvider,
            NullLogger<TopUpWalletHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithNewKey_TopsUpSavesAndReturnsDetails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        TopUpWalletCommand command = new TopUpWalletCommand(_wallet.Id, OwnerId, 25m, Currency.EUR, idempotencyKey);

        WalletDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(25m, _wallet.Balance);
        _ledgerEntryRepository.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.Type == LedgerEntryType.TopUp
            && entry.Amount == Money.Create(25m, Currency.EUR)
            && entry.IdempotencyKey == idempotencyKey
            && entry.CreatedAt == Now));
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        Assert.Equal(25m, result.Balance);
    }

    [Fact]
    public async Task HandleAsync_WithKeyOfSameTopUp_ReplaysWithoutMovingMoney()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        LedgerEntry existing = _wallet.TopUp(Money.Create(25m, Currency.EUR), idempotencyKey, Now);
        _ledgerEntryRepository.GetByIdempotencyKeyAsync(_wallet.Id, idempotencyKey, cancellationToken).Returns(existing);
        TopUpWalletCommand command = new TopUpWalletCommand(_wallet.Id, OwnerId, 25m, Currency.EUR, idempotencyKey);

        WalletDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(25m, _wallet.Balance);
        Assert.Equal(25m, result.Balance);
        _ledgerEntryRepository.DidNotReceive().Add(Arg.Any<LedgerEntry>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithKeyOfDifferentTopUp_ThrowsIdempotencyKeyReused()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        LedgerEntry existing = _wallet.TopUp(Money.Create(25m, Currency.EUR), idempotencyKey, Now);
        _ledgerEntryRepository.GetByIdempotencyKeyAsync(_wallet.Id, idempotencyKey, cancellationToken).Returns(existing);
        TopUpWalletCommand command = new TopUpWalletCommand(_wallet.Id, OwnerId, 30m, Currency.EUR, idempotencyKey);

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => _handler.HandleAsync(command, cancellationToken));

        Assert.Equal(25m, _wallet.Balance);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForAnotherUsersWallet_ThrowsNotFoundBeforeLookingUpTheKey()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TopUpWalletCommand command = new TopUpWalletCommand(_wallet.Id, Guid.CreateVersion7(), 25m, Currency.EUR, Guid.NewGuid());

        await Assert.ThrowsAsync<WalletNotFoundException>(() => _handler.HandleAsync(command, cancellationToken));

        await _ledgerEntryRepository.DidNotReceive()
            .GetByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}