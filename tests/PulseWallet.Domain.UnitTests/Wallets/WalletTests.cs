using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.UnitTests.Wallets;

public sealed class WalletTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidFriendlyName_ReturnsEmptyWalletWithTrimmedName()
    {
        Wallet wallet = Wallet.Create(OwnerId, "  Holiday fund 2027! ", Currency.EUR, Now);

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal("Holiday fund 2027!", wallet.FriendlyName);
        Assert.Equal(Currency.EUR, wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
        Assert.Equal(Now, wallet.CreatedAt);
        Assert.Equal(OwnerId, wallet.OwnerId);
    }

    [Fact]
    public void Create_WithoutOwner_Throws()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(Guid.Empty, "Alice", Currency.EUR, Now));

        Assert.Contains("owner", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsOwnedBy_ReturnsTrueOnlyForTheOwner()
    {
        Wallet wallet = CreateWallet();

        Assert.True(wallet.IsOwnedBy(OwnerId));
        Assert.False(wallet.IsOwnedBy(Guid.CreateVersion7()));
    }

    [Theory]
    [InlineData("Rent\nmoney")]
    [InlineData("Rent\tmoney")]
    public void Create_WithControlCharacterInFriendlyName_ThrowsDomainException(string friendlyName)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, friendlyName, Currency.EUR, Now));
        Assert.Contains("control characters", exception.Message);
    }

    [Fact]
    public void Create_WithTooLongFriendlyName_ThrowsDomainException()
    {
        string friendlyName = new string('a', Wallet.FriendlyNameMaxLength + 1);

        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, friendlyName, Currency.EUR, Now));
        Assert.Contains("at most", exception.Message);
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, "Alice", (Currency)123, Now));
        Assert.Contains("is not supported", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingFriendlyName_ThrowsDomainException(string? friendlyName)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, friendlyName!, Currency.EUR, Now));
        Assert.Contains("needs a friendly name", exception.Message);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("This name is exactly forty characters!!!")]
    public void Create_WithFriendlyNameAtLengthLimit_Succeeds(string friendlyName)
    {
        Wallet wallet = Wallet.Create(OwnerId, friendlyName, Currency.EUR, Now);

        Assert.Equal(friendlyName, wallet.FriendlyName);
    }

    public static readonly TheoryData<decimal> ValidTopUpAmounts =
    [
        50m,
        50.10m,
        0.01m
    ];

    [Theory]
    [MemberData(nameof(ValidTopUpAmounts))]
    public void TopUp_WithValidAmount_IncreasesBalanceAndReturnsTopUpEntry(decimal topUpAmount)
    {
        Wallet wallet = CreateWallet();
        Money amount = Money.Create(topUpAmount, Currency.EUR);
        Guid idempotencyKey = Guid.NewGuid();

        LedgerEntry entry = wallet.TopUp(amount, idempotencyKey, Now);

        Assert.Equal(topUpAmount, wallet.Balance);
        Assert.Equal(LedgerEntryType.TopUp, entry.Type);
        Assert.Equal(wallet.Id, entry.WalletId);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(idempotencyKey, entry.IdempotencyKey);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Null(entry.TransferId);
    }

    public static readonly TheoryData<decimal> ValidWithdrawalAmounts =
    [
        50m,
        50.10m,
        0.01m,
        1000m
    ];

    [Theory]
    [MemberData(nameof(ValidWithdrawalAmounts))]
    public void Withdraw_WithValidAmount_DecreasesBalanceAndReturnsWithdrawalEntry(decimal withdrawalAmount)
    {
        Wallet wallet = CreateWalletWithBalance(1000m);
        Money amount = Money.Create(withdrawalAmount, Currency.EUR);
        Guid idempotencyKey = Guid.NewGuid();

        LedgerEntry entry = wallet.Withdraw(amount, idempotencyKey, Now);

        Assert.Equal(1000m - withdrawalAmount, wallet.Balance);
        Assert.Equal(LedgerEntryType.Withdrawal, entry.Type);
        Assert.Equal(wallet.Id, entry.WalletId);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(idempotencyKey, entry.IdempotencyKey);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Null(entry.TransferId);
    }

    [Fact]
    public void TopUp_WithDifferentCurrency_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.TopUp(Money.Create(50m, Currency.USD), Guid.NewGuid(), Now));

        Assert.Contains("Wallet currency is EUR, but the amount is in USD", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void TopUp_WithZeroAmount_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.TopUp(Money.Create(0m, Currency.EUR), Guid.NewGuid(), Now));

        Assert.Contains("Amount must be greater than zero", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WholeBalance_LeavesZeroBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        wallet.Withdraw(Money.Create(100m, Currency.EUR), Guid.NewGuid(), Now);

        Assert.Equal(0m, wallet.Balance);
    }

    public static readonly TheoryData<decimal> AmountsAboveBalanceOf100 =
    [
        100.01m,
        500m
    ];

    [Theory]
    [MemberData(nameof(AmountsAboveBalanceOf100))]
    public void Withdraw_MoreThanBalance_ThrowsInsufficientFundsExceptionAndKeepsBalance(decimal requested)
    {
        Wallet wallet = CreateWalletWithBalance(100m);
        Money amount = Money.Create(requested, Currency.EUR);

        Assert.Throws<InsufficientFundsException>(() => wallet.Withdraw(amount, Guid.NewGuid(), Now));

        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WithDifferentCurrency_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.Withdraw(Money.Create(50m, Currency.USD), Guid.NewGuid(), Now));

        Assert.Contains("Wallet currency is EUR, but the amount is in USD", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WithZeroAmount_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.Withdraw(Money.Create(0m, Currency.EUR), Guid.NewGuid(), Now));

        Assert.Contains("Amount must be greater than zero", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    private static Wallet CreateWallet(Currency currency = Currency.EUR)
    {
        return Wallet.Create(OwnerId, "Alice", currency, Now);
    }

    private static Wallet CreateWalletWithBalance(decimal balance, Currency currency = Currency.EUR)
    {
        Wallet wallet = CreateWallet(currency);
        wallet.TopUp(Money.Create(balance, currency), Guid.NewGuid(), Now);
        return wallet;
    }
}