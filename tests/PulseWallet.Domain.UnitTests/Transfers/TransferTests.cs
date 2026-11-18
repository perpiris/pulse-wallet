using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.UnitTests.Transfers;

public sealed class TransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly RateDate = new(2026, 11, 11);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    [Fact]
    public void Execute_WithSufficientFunds_MovesMoneyAndReturnsTransferWithBothEntries()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m);
        Money amount = Money.Create(30m, Currency.EUR);
        Guid idempotencyKey = Guid.NewGuid();

        TransferExecution execution = Transfer.Execute(source, destination, amount, null, idempotencyKey, Now);

        Assert.Equal(70m, source.Balance);
        Assert.Equal(50m, destination.Balance);

        Transfer transfer = execution.Transfer;
        Assert.NotEqual(Guid.Empty, transfer.Id);
        Assert.Equal(source.Id, transfer.SourceWalletId);
        Assert.Equal(destination.Id, transfer.DestinationWalletId);
        Assert.Equal(amount, transfer.Amount);
        Assert.Equal(amount, transfer.ReceivedAmount);
        Assert.Null(transfer.Rate);
        Assert.Null(transfer.RateDate);
        Assert.Equal(idempotencyKey, transfer.IdempotencyKey);
        Assert.Equal(Now, transfer.CreatedAt);

        Assert.Equal(LedgerEntryType.TransferOut, execution.Debit.Type);
        Assert.Equal(source.Id, execution.Debit.WalletId);
        Assert.Equal(LedgerEntryType.TransferIn, execution.Credit.Type);
        Assert.Equal(destination.Id, execution.Credit.WalletId);

        foreach (LedgerEntry entry in new[] { execution.Debit, execution.Credit })
        {
            Assert.Equal(transfer.Id, entry.TransferId);
            Assert.Equal(amount, entry.Amount);
            Assert.Null(entry.IdempotencyKey);
            Assert.Equal(Now, entry.CreatedAt);
        }
    }

    [Fact]
    public void Execute_WholeBalance_LeavesSourceAtZero()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 0m);

        Transfer.Execute(source, destination, Money.Create(100m, Currency.EUR), null, Guid.NewGuid(), Now);

        Assert.Equal(0m, source.Balance);
        Assert.Equal(100m, destination.Balance);
    }

    [Fact]
    public void Execute_ToSameWallet_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance("alice", 100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(wallet, wallet, Money.Create(30m, Currency.EUR), null, Guid.NewGuid(), Now));

        Assert.Contains("cannot transfer to itself", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Execute_BetweenDifferentCurrencies_ConvertsRoundingDownAndStoresTheRate()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m, Currency.USD);
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, 1.09687m, RateDate);
        Money amount = Money.Create(10m, Currency.EUR);

        TransferExecution execution = Transfer.Execute(source, destination, amount, rate, Guid.NewGuid(), Now);

        // 10 × 1.09687 = 10.9687 → 10.96, not 10.97.
        Money received = Money.Create(10.96m, Currency.USD);
        Assert.Equal(90m, source.Balance);
        Assert.Equal(30.96m, destination.Balance);
        Assert.Equal(amount, execution.Transfer.Amount);
        Assert.Equal(received, execution.Transfer.ReceivedAmount);
        Assert.Equal(1.09687m, execution.Transfer.Rate);
        Assert.Equal(RateDate, execution.Transfer.RateDate);
        Assert.Equal(amount, execution.Debit.Amount);
        Assert.Equal(received, execution.Credit.Amount);
    }

    [Fact]
    public void Execute_BetweenDifferentCurrenciesWithoutRate_ThrowsDomainExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m, Currency.USD);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(source, destination, Money.Create(30m, Currency.EUR), null, Guid.NewGuid(), Now));

        Assert.Contains("needs an exchange rate", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Execute_WithRateForAnotherPair_ThrowsDomainExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m, Currency.USD);
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.DKK, 7.4744m, RateDate);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(source, destination, Money.Create(30m, Currency.EUR), rate, Guid.NewGuid(), Now));

        Assert.Contains("rate is for EUR/DKK", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Execute_AmountThatConvertsToZero_ThrowsDomainExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m, Currency.USD);
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, 0.5m, RateDate);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(source, destination, Money.Create(0.01m, Currency.EUR), rate, Guid.NewGuid(), Now));

        Assert.Contains("too small to convert", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Execute_WithAmountInOtherCurrency_ThrowsDomainExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(source, destination, Money.Create(30m, Currency.USD), null, Guid.NewGuid(), Now));

        Assert.Contains("Wallet currency is EUR, but the amount is in USD", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Execute_MoreThanBalance_ThrowsInsufficientFundsExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m);

        Assert.Throws<InsufficientFundsException>(
            () => Transfer.Execute(source, destination, Money.Create(100.01m, Currency.EUR), null, Guid.NewGuid(), Now));

        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Execute_WithZeroAmount_ThrowsDomainExceptionAndKeepsBothBalances()
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        Wallet destination = CreateWalletWithBalance("bob", 20m);

        DomainException exception = Assert.Throws<DomainException>(
            () => Transfer.Execute(source, destination, Money.Create(0m, Currency.EUR), null, Guid.NewGuid(), Now));

        Assert.Contains("Amount must be greater than zero", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(20m, destination.Balance);
    }

    [Fact]
    public void Matches_WithSameDestinationAndAmount_IsTrue()
    {
        Wallet destination = CreateWalletWithBalance("bob", 0m);
        Transfer transfer = ExecuteTransfer(destination, 30m);

        Assert.True(transfer.Matches(destination.Id, Money.Create(30m, Currency.EUR)));
    }

    [Fact]
    public void Matches_WithDifferentDestination_IsFalse()
    {
        Wallet destination = CreateWalletWithBalance("bob", 0m);
        Transfer transfer = ExecuteTransfer(destination, 30m);

        Assert.False(transfer.Matches(Guid.NewGuid(), Money.Create(30m, Currency.EUR)));
    }

    [Fact]
    public void Matches_WithDifferentAmount_IsFalse()
    {
        Wallet destination = CreateWalletWithBalance("bob", 0m);
        Transfer transfer = ExecuteTransfer(destination, 30m);

        Assert.False(transfer.Matches(destination.Id, Money.Create(31m, Currency.EUR)));
    }

    private static Transfer ExecuteTransfer(Wallet destination, decimal amount)
    {
        Wallet source = CreateWalletWithBalance("alice", 100m);
        return Transfer.Execute(source, destination, Money.Create(amount, Currency.EUR), null, Guid.NewGuid(), Now).Transfer;
    }

    private static Wallet CreateWalletWithBalance(string friendlyName, decimal balance, Currency currency = Currency.EUR)
    {
        Wallet wallet = Wallet.Create(OwnerId, friendlyName, currency, Now);
        if (balance > 0)
        {
            wallet.TopUp(Money.Create(balance, currency), Guid.NewGuid(), Now);
        }

        return wallet;
    }
}