using Microsoft.Extensions.Logging.Abstractions;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.WithdrawFromWallet;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.UnitTests.Wallets.WithdrawFromWallet;

public sealed class WithdrawFromWalletHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ILedgerEntryRepository _ledgerEntryRepository = Substitute.For<ILedgerEntryRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IWalletQueries _walletQueries = Substitute.For<IWalletQueries>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly WithdrawFromWalletHandler _handler;
    private readonly Wallet _wallet = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);

    public WithdrawFromWalletHandlerTests()
    {
        _wallet.TopUp(Money.Create(100m, Currency.EUR), Guid.NewGuid(), Now);

        _timeProvider.GetUtcNow().Returns(Now);
        _walletRepository.GetByIdAsync(_wallet.Id, Arg.Any<CancellationToken>()).Returns(_wallet);
        _walletQueries
            .GetDetailsAsync(_wallet.Id, OwnerId, WalletDetails.RecentEntriesCount, Arg.Any<CancellationToken>())
            .Returns(call => new WalletDetails(_wallet.Id, "alice", Currency.EUR, _wallet.Balance, Now, []));

        _handler = new WithdrawFromWalletHandler(
            _ledgerEntryRepository,
            _walletRepository,
            _unitOfWork,
            _walletQueries,
            _timeProvider,
            NullLogger<WithdrawFromWalletHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithNewKey_WithdrawsSavesAndReturnsDetails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        WithdrawFromWalletCommand command = new WithdrawFromWalletCommand(_wallet.Id, OwnerId, 40m, Currency.EUR, idempotencyKey);

        WalletDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(60m, _wallet.Balance);
        _ledgerEntryRepository.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.Type == LedgerEntryType.Withdrawal
            && entry.Amount == Money.Create(40m, Currency.EUR)
            && entry.IdempotencyKey == idempotencyKey));
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        Assert.Equal(60m, result.Balance);
    }

    // The first attempt already spent the balance; the retry must not fail on the funds it spent itself.
    [Fact]
    public async Task HandleAsync_ReplayOfWithdrawalThatEmptiedTheWallet_ReturnsDetailsInsteadOfInsufficientFunds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        LedgerEntry existing = _wallet.Withdraw(Money.Create(100m, Currency.EUR), idempotencyKey, Now);
        _ledgerEntryRepository.GetByIdempotencyKeyAsync(_wallet.Id, idempotencyKey, cancellationToken).Returns(existing);
        WithdrawFromWalletCommand command = new WithdrawFromWalletCommand(_wallet.Id, OwnerId, 100m, Currency.EUR, idempotencyKey);

        WalletDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(0m, result.Balance);
        _ledgerEntryRepository.DidNotReceive().Add(Arg.Any<LedgerEntry>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithKeyOfATopUp_ThrowsIdempotencyKeyReused()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        LedgerEntry topUp = _wallet.TopUp(Money.Create(40m, Currency.EUR), idempotencyKey, Now);
        _ledgerEntryRepository.GetByIdempotencyKeyAsync(_wallet.Id, idempotencyKey, cancellationToken).Returns(topUp);
        WithdrawFromWalletCommand command = new WithdrawFromWalletCommand(_wallet.Id, OwnerId, 40m, Currency.EUR, idempotencyKey);

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => _handler.HandleAsync(command, cancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MoreThanBalance_ThrowsInsufficientFundsAndSavesNothing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WithdrawFromWalletCommand command = new WithdrawFromWalletCommand(_wallet.Id, OwnerId, 100.01m, Currency.EUR, Guid.NewGuid());

        await Assert.ThrowsAsync<InsufficientFundsException>(() => _handler.HandleAsync(command, cancellationToken));

        Assert.Equal(100m, _wallet.Balance);
        _ledgerEntryRepository.DidNotReceive().Add(Arg.Any<LedgerEntry>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}