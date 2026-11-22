using Microsoft.Extensions.Logging.Abstractions;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Transfers;
using PulseWallet.Application.Transfers.CreateTransfer;
using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.UnitTests.Transfers.CreateTransfer;

public sealed class CreateTransferHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly RateDate = new(2026, 11, 20);
    private static readonly DateOnly StaleRateDate = new(2026, 11, 14);
    private const int MaxRateAgeInDays = 4;
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITransferRepository _transferRepository = Substitute.For<ITransferRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly ILedgerEntryRepository _ledgerEntryRepository = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransferQueries _transferQueries = Substitute.For<ITransferQueries>();
    private readonly IExchangeRateProvider _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CreateTransferHandler _handler;

    private readonly Wallet _source = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);
    private readonly Wallet _euroDestination = Wallet.Create(Guid.CreateVersion7(), "bob", Currency.EUR, Now);
    private readonly Wallet _dollarDestination = Wallet.Create(Guid.CreateVersion7(), "carol", Currency.USD, Now);

    public CreateTransferHandlerTests()
    {
        _source.TopUp(Money.Create(100m, Currency.EUR), Guid.NewGuid(), Now);

        _timeProvider.GetUtcNow().Returns(Now);
        foreach (Wallet wallet in new[] { _source, _euroDestination, _dollarDestination })
        {
            _walletRepository.GetByIdAsync(wallet.Id, Arg.Any<CancellationToken>()).Returns(wallet);
        }

        // Echoes back the id it's asked for, so a test can check which transfer the handler read back.
        _transferQueries
            .GetDetailsAsync(Arg.Any<Guid>(), OwnerId, Arg.Any<CancellationToken>())
            .Returns(call => new TransferDetails(
                call.ArgAt<Guid>(0), _source.Id, Guid.Empty, 0m, Currency.EUR, 0m, Currency.EUR, null, null, Now));

        _handler = new CreateTransferHandler(
            _transferRepository,
            _walletRepository,
            _ledgerEntryRepository,
            _unitOfWork,
            _transferQueries,
            _exchangeRateProvider,
            new ExchangeRateAgePolicy(_timeProvider, MaxRateAgeInDays),
            _timeProvider,
            NullLogger<CreateTransferHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SameCurrency_MovesMoneySavesEverythingAndNeverAsksForARate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _euroDestination.Id, 30m, Currency.EUR, Guid.NewGuid(), null);

        Transfer? added = null;
        _transferRepository.Add(Arg.Do<Transfer>(transfer => added = transfer));

        TransferDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(70m, _source.Balance);
        Assert.Equal(30m, _euroDestination.Balance);
        Assert.NotNull(added);
        _ledgerEntryRepository.Received(1).Add(Arg.Is<LedgerEntry>(entry => entry.Type == LedgerEntryType.TransferOut));
        _ledgerEntryRepository.Received(1).Add(Arg.Is<LedgerEntry>(entry => entry.Type == LedgerEntryType.TransferIn));
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        await _exchangeRateProvider.DidNotReceive()
            .GetLatestRateAsync(Arg.Any<Currency>(), Arg.Any<Currency>(), Arg.Any<CancellationToken>());
        Assert.Equal(added.Id, result.Id);
    }

    [Fact]
    public async Task HandleAsync_DifferentCurrencies_ConvertsWithTheProvidersRate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _exchangeRateProvider
            .GetLatestRateAsync(Currency.EUR, Currency.USD, cancellationToken)
            .Returns(ExchangeRate.Create(Currency.EUR, Currency.USD, 1.1m, RateDate));
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 10m, Currency.EUR, Guid.NewGuid(), null);

        await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(90m, _source.Balance);
        Assert.Equal(11m, _dollarDestination.Balance);
        _transferRepository.Received(1).Add(Arg.Is<Transfer>(transfer => transfer.Rate == 1.1m && transfer.RateDate == RateDate));
    }

    [Fact]
    public async Task HandleAsync_RateUnavailable_ThrowsAndSavesNothing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _exchangeRateProvider
            .GetLatestRateAsync(Currency.EUR, Currency.USD, cancellationToken)
            .Returns<ExchangeRate>(_ => throw new ExchangeRateUnavailableException(Currency.EUR, Currency.USD));
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 10m, Currency.EUR, Guid.NewGuid(), null);

        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(() => _handler.HandleAsync(command, cancellationToken));

        Assert.Equal(100m, _source.Balance);
        _transferRepository.DidNotReceive().Add(Arg.Any<Transfer>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithKeyOfSameTransfer_ReturnsTheOriginalWithoutSendingAgain()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        Transfer existing = Transfer.Execute(
            _source, _euroDestination, Money.Create(30m, Currency.EUR), null, idempotencyKey, Now).Transfer;
        _transferRepository.GetByIdempotencyKeyAsync(_source.Id, idempotencyKey, cancellationToken).Returns(existing);
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _euroDestination.Id, 30m, Currency.EUR, idempotencyKey, null);

        TransferDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(70m, _source.Balance);
        _transferRepository.DidNotReceive().Add(Arg.Any<Transfer>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithKeyOfTransferToAnotherWallet_ThrowsIdempotencyKeyReused()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid idempotencyKey = Guid.NewGuid();
        Transfer existing = Transfer.Execute(
            _source, _euroDestination, Money.Create(30m, Currency.EUR), null, idempotencyKey, Now).Transfer;
        _transferRepository.GetByIdempotencyKeyAsync(_source.Id, idempotencyKey, cancellationToken).Returns(existing);
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 30m, Currency.EUR, idempotencyKey, null);

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => _handler.HandleAsync(command, cancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FromAnotherUsersWallet_ThrowsNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _euroDestination.Id, _source.Id, 10m, Currency.EUR, Guid.NewGuid(), null);

        await Assert.ThrowsAsync<WalletNotFoundException>(() => _handler.HandleAsync(command, cancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ToMissingWallet_ThrowsNotFoundAndKeepsBalance()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, Guid.CreateVersion7(), 10m, Currency.EUR, Guid.NewGuid(), null);

        await Assert.ThrowsAsync<WalletNotFoundException>(() => _handler.HandleAsync(command, cancellationToken));

        Assert.Equal(100m, _source.Balance);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StaleRateNotAccepted_ThrowsWithTheRateAndSavesNothing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ExchangeRate staleRate = ExchangeRate.Create(Currency.EUR, Currency.USD, 1.05m, StaleRateDate);
        _exchangeRateProvider.GetLatestRateAsync(Currency.EUR, Currency.USD, cancellationToken).Returns(staleRate);
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 10m, Currency.EUR, Guid.NewGuid(), null);

        StaleExchangeRateException exception = await Assert.ThrowsAsync<StaleExchangeRateException>(
            () => _handler.HandleAsync(command, cancellationToken));

        Assert.Same(staleRate, exception.Rate);
        Assert.Equal(100m, _source.Balance);
        _transferRepository.DidNotReceive().Add(Arg.Any<Transfer>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StaleRateAcceptedForAnOlderDate_StillThrows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _exchangeRateProvider
            .GetLatestRateAsync(Currency.EUR, Currency.USD, cancellationToken)
            .Returns(ExchangeRate.Create(Currency.EUR, Currency.USD, 1.05m, StaleRateDate));
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 10m, Currency.EUR, Guid.NewGuid(), StaleRateDate.AddDays(-1));

        await Assert.ThrowsAsync<StaleExchangeRateException>(() => _handler.HandleAsync(command, cancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StaleRateAcceptedWithItsDate_SendsAtThatRate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _exchangeRateProvider
            .GetLatestRateAsync(Currency.EUR, Currency.USD, cancellationToken)
            .Returns(ExchangeRate.Create(Currency.EUR, Currency.USD, 1.05m, StaleRateDate));
        CreateTransferCommand command = new CreateTransferCommand(
            OwnerId, _source.Id, _dollarDestination.Id, 10m, Currency.EUR, Guid.NewGuid(), StaleRateDate);

        await _handler.HandleAsync(command, cancellationToken);

        Assert.Equal(10.50m, _dollarDestination.Balance);
        _transferRepository.Received(1).Add(Arg.Is<Transfer>(transfer => transfer.RateDate == StaleRateDate));
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}